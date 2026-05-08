using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ResultService.Application.Events;
using ResultService.Application.Repositories;
using ResultService.Application.Services;
using ResultService.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace ResultService.Infrastructure.Services;

public class ResultAppService : IResultService
{
    private readonly IResultRepository _resultRepository;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ResultAppService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _answerServiceBaseUrl;
    private readonly string _assessmentServiceBaseUrl;
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public ResultAppService(
        IResultRepository resultRepository,
        IPublishEndpoint publishEndpoint,
        ILogger<ResultAppService> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _resultRepository = resultRepository;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _httpClient = httpClient;
        _answerServiceBaseUrl = (configuration["ServiceUrls:AnswerService"] ?? "http://localhost:5118").TrimEnd('/');
        _assessmentServiceBaseUrl = (configuration["ServiceUrls:AssessmentService"] ?? "http://localhost:5098").TrimEnd('/');
        _jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        _jwtIssuer = configuration["Jwt:Issuer"] ?? "IdentityService";
        _jwtAudience = configuration["Jwt:Audience"] ?? "AssessmentPortal";
    }

    public async Task<IReadOnlyList<Result>> GetAllAsync()
        => await _resultRepository.GetAllAsync();

    public async Task<Result?> GetByIdAsync(Guid id)
        => await _resultRepository.GetByIdAsync(id);

    public async Task<IReadOnlyList<Result>> GetByCandidateIdAsync(Guid candidateId)
        => await _resultRepository.GetByCandidateIdAsync(candidateId);

    public async Task<IReadOnlyList<Result>> GetByAssessmentIdAsync(Guid assessmentId)
        => await _resultRepository.GetByAssessmentIdAsync(assessmentId);

    public async Task<Result?> GetByCandidateAndAssessmentAsync(Guid candidateId, Guid assessmentId)
        => await _resultRepository.GetByCandidateAndAssessmentAsync(candidateId, assessmentId);

    public async Task<Result> CreateAsync(Result result)
    {
        result.Id = Guid.NewGuid();
        result.CalculatedAt = DateTime.UtcNow;

        await _resultRepository.AddAsync(result);
        await _resultRepository.SaveChangesAsync();
        await PublishCreatedEvent(result);

        return result;
    }

    public async Task<Result> UpdateAsync(Result result)
    {
        _resultRepository.Update(result);
        await _resultRepository.SaveChangesAsync();
        return result;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var deleted = await _resultRepository.DeleteAsync(id);
        if (!deleted)
            return false;

        await _resultRepository.SaveChangesAsync();
        return true;
    }

    public async Task<Result> CalculateAndPublishAsync(Guid candidateId, Guid assessmentId)
    {
        try
        {
            var existing = await _resultRepository.GetByCandidateAndAssessmentAsync(candidateId, assessmentId, asNoTracking: false);

            if (existing != null && existing.Status == "Published")
                return existing;

            var answers = await GetCandidateAnswersAsync(candidateId, assessmentId);
            if (answers == null || !answers.Any())
            {
                _logger.LogWarning(
                    "No answers found for candidate {CandidateId} in assessment {AssessmentId}",
                    candidateId,
                    assessmentId);
                throw new InvalidOperationException("No answers submitted for this assessment");
            }

            var questions = await GetAssessmentQuestionsAsync(assessmentId);
            if (!questions.Any())
                throw new InvalidOperationException("No questions found for this assessment");

            var answerByQuestion = answers
                .GroupBy(a => a.QuestionId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.SubmittedAt).First());

            var correctAnswers = 0;
            var wrongAnswers = 0;
            var skippedQuestions = 0;
            var totalScore = 0;
            var maxScore = questions.Sum(q => Math.Max(q.MaxScore, 1));

            foreach (var question in questions)
            {
                if (!answerByQuestion.TryGetValue(question.Id, out var answer) || answer.IsBlank)
                {
                    skippedQuestions += 1;
                    continue;
                }

                var grade = GradeQuestion(question, answer);
                if (grade.IsCorrect)
                {
                    correctAnswers += 1;
                    totalScore += grade.Points;
                }
                else
                {
                    wrongAnswers += 1;
                }
            }

            var percentage = maxScore > 0 ? Math.Round((decimal)totalScore / maxScore * 100, 2) : 0m;
            var passingPercentage = 50m;
            var isPassed = percentage >= passingPercentage;

            var result = existing ?? new Result
            {
                Id = Guid.NewGuid(),
                CandidateId = candidateId,
                AssessmentId = assessmentId
            };

            result.Score = totalScore;
            result.MaxScore = maxScore;
            result.Percentage = percentage;
            result.TotalQuestions = questions.Count;
            result.CorrectAnswers = correctAnswers;
            result.WrongAnswers = wrongAnswers;
            result.SkippedQuestions = skippedQuestions;
            result.StartedAt = result.StartedAt == default ? answers.Min(a => a.SubmittedAt) : result.StartedAt;
            result.CompletedAt = answers.Max(a => a.SubmittedAt);
            result.EvaluatedAt = DateTime.UtcNow;
            result.IsPassed = isPassed;
            result.PassingPercentage = passingPercentage;
            result.Status = "Graded";
            result.CalculatedAt = DateTime.UtcNow;
            result.Remarks = isPassed ? "Assessment passed successfully" : "Assessment not passed";

            if (existing == null)
                await _resultRepository.AddAsync(result);
            else
                _resultRepository.Update(result);

            await _resultRepository.SaveChangesAsync();
            await PublishCreatedEvent(result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error calculating result for candidate {CandidateId} in assessment {AssessmentId}",
                candidateId,
                assessmentId);
            throw;
        }
    }

    public async Task<Result> PublishResultAsync(Guid resultId)
    {
        var result = await _resultRepository.GetByIdAsync(resultId)
            ?? throw new InvalidOperationException($"Result with ID {resultId} not found");

        result.Status = "Published";
        result.PublishedAt = DateTime.UtcNow;

        _resultRepository.Update(result);
        await _resultRepository.SaveChangesAsync();

        return result;
    }

    public async Task<IReadOnlyList<Result>> GetPassedCandidatesAsync(Guid assessmentId)
        => (await _resultRepository.GetByAssessmentIdAsync(assessmentId))
            .Where(r => r.IsPassed)
            .ToList();

    public async Task<IReadOnlyList<Result>> GetFailedCandidatesAsync(Guid assessmentId)
        => (await _resultRepository.GetByAssessmentIdAsync(assessmentId))
            .Where(r => !r.IsPassed)
            .ToList();

    private async Task PublishCreatedEvent(Result result)
    {
        await _publishEndpoint.Publish(new ResultCreatedEvent
        {
            ResultId = result.Id,
            AssessmentId = result.AssessmentId,
            CandidateId = result.CandidateId,
            Score = result.Score,
            CalculatedAt = result.CalculatedAt ?? DateTime.UtcNow
        });
    }

    private async Task<List<AnswerDto>?> GetCandidateAnswersAsync(Guid candidateId, Guid assessmentId)
    {
        try
        {
            using var request = CreateAuthorizedRequest(
                HttpMethod.Get,
                $"{_answerServiceBaseUrl}/api/assessments/{assessmentId}/answers/candidates/{candidateId}");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to retrieve answers from Answer Service. Status: {StatusCode}",
                    response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadFromJsonAsync<CandidateAnswersResponse>();
            return content?.Answers ?? new List<AnswerDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Answer Service for candidate {CandidateId}", candidateId);
            return null;
        }
    }

    private async Task<List<QuestionDto>> GetAssessmentQuestionsAsync(Guid assessmentId)
    {
        try
        {
            using var request = CreateAuthorizedRequest(
                HttpMethod.Get,
                $"{_assessmentServiceBaseUrl}/api/assessments/{assessmentId}/questions");
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to retrieve questions from Assessment Service. Status: {StatusCode}",
                    response.StatusCode);
                return new List<QuestionDto>();
            }

            var questions = await response.Content.ReadFromJsonAsync<List<QuestionDto>>();

            return questions ?? new List<QuestionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Assessment Service for assessment {AssessmentId}", assessmentId);
            return new List<QuestionDto>();
        }
    }

    private static (bool IsCorrect, int Points) GradeQuestion(QuestionDto question, AnswerDto answer)
    {
        var maxScore = Math.Max(question.MaxScore, 1);
        if (question.Type.Equals("MCQ", StringComparison.OrdinalIgnoreCase))
        {
            var selected = question.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId);
            var isCorrect = selected?.IsCorrect == true;
            return (isCorrect, isCorrect ? maxScore : 0);
        }

        if (!string.IsNullOrWhiteSpace(question.CorrectAnswer))
        {
            var isCorrect = string.Equals(
                question.CorrectAnswer.Trim(),
                answer.AnswerText.Trim(),
                StringComparison.OrdinalIgnoreCase);
            return (isCorrect, isCorrect ? maxScore : 0);
        }

        var manuallyCorrect = answer.IsCorrect == true;
        return (manuallyCorrect, manuallyCorrect ? Math.Min(answer.PointsObtained ?? maxScore, maxScore) : 0);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateServiceAccessToken());
        return request;
    }

    private string CreateServiceAccessToken()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "result-service"),
            new Claim(ClaimTypes.Name, "Result Service"),
            new Claim(ClaimTypes.Email, "result-service@internal.local"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class CandidateAnswersResponse
    {
        public List<AnswerDto> Answers { get; set; } = new();
    }

    private sealed class AnswerDto
    {
        public Guid QuestionId { get; set; }
        public Guid? SelectedOptionId { get; set; }
        public string AnswerText { get; set; } = string.Empty;
        public bool? IsCorrect { get; set; }
        public int? PointsObtained { get; set; }
        public DateTime SubmittedAt { get; set; }

        public bool IsBlank => SelectedOptionId == null && string.IsNullOrWhiteSpace(AnswerText);
    }

    private sealed class QuestionDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = "MCQ";
        public int MaxScore { get; set; } = 1;
        public string? CorrectAnswer { get; set; }
        public List<QuestionOptionDto> Options { get; set; } = new();
    }

    private sealed class QuestionOptionDto
    {
        public Guid Id { get; set; }
        public bool IsCorrect { get; set; }
    }
}
