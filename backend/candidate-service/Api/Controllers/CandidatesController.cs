using CandidateService.Application.Events;
using CandidateService.Application.Services;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net.Http.Json;

namespace CandidateService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CandidatesController : ControllerBase
{
    private readonly ICandidateService _candidateService;
    private readonly ICandidateAssessmentService _assessmentService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CandidatesController> _logger;
    private readonly HttpClient _httpClient = new();
    private readonly string _assessmentServiceBaseUrl;
    private readonly string _resultServiceBaseUrl;

    public CandidatesController(
        ICandidateService candidateService,
        ICandidateAssessmentService assessmentService,
        IPublishEndpoint publishEndpoint,
        IConfiguration configuration,
        ILogger<CandidatesController> logger)
    {
        _candidateService = candidateService;
        _assessmentService = assessmentService;
        _publishEndpoint = publishEndpoint;
        _assessmentServiceBaseUrl = (configuration["ServiceUrls:AssessmentService"] ?? "http://localhost:5098").TrimEnd('/');
        _resultServiceBaseUrl = (configuration["ServiceUrls:ResultService"] ?? "http://localhost:5160").TrimEnd('/');
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCandidates()
    {
        var candidates = await _candidateService.GetAllCandidatesAsync();
        return Ok(candidates);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCandidate(Guid id)
    {
        var candidate = await _candidateService.GetCandidateByIdAsync(id);
        if (candidate == null)
            return NotFound();

        return Ok(candidate);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCandidate([FromBody] CreateCandidateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Name and Email are required");

        var candidate = await _candidateService.CreateCandidateAsync(request.Name, request.Email);
        return CreatedAtAction(nameof(GetCandidate), new { id = candidate.Id }, candidate);
    }

    [HttpPost("{id}/assessments")]
    public async Task<IActionResult> AssignAssessment(Guid id, [FromBody] AssignAssessmentRequest request)
    {
        var assignment = await _assessmentService.AssignAssessmentAsync(id, request.AssessmentId);
        await PublishAssignmentCreatedEventAsync(assignment);
        return Ok(assignment);
    }

    [HttpPost("assignments")]
    public async Task<IActionResult> AssignAssessmentToCandidate([FromBody] AssignmentRequest request)
    {
        if (request.CandidateId == Guid.Empty || request.AssessmentId == Guid.Empty)
            return BadRequest(new { message = "Candidate ID and Assessment ID are required" });

        var candidate = await _candidateService.GetCandidateByIdAsync(request.CandidateId);
        if (candidate == null)
            return NotFound(new { message = "Candidate not found" });

        var assignment = await _assessmentService.AssignAssessmentAsync(request.CandidateId, request.AssessmentId);
        await PublishAssignmentCreatedEventAsync(assignment);
        var response = await MapAssignmentAsync(assignment);
        return Ok(response);
    }

    private async Task PublishAssignmentCreatedEventAsync(CandidateService.Domain.Entities.CandidateAssessment assignment)
    {
        await _publishEndpoint.Publish(new CandidateAssessmentAssignedEvent
        {
            CandidateAssessmentId = assignment.Id,
            CandidateId = assignment.CandidateId,
            AssessmentId = assignment.AssessmentId,
            AssignedAt = assignment.AssignedAt
        });
    }

    [HttpGet("{id}/assessments")]
    public async Task<IActionResult> GetCandidateAssessments(Guid id)
    {
        var assessments = await _assessmentService.GetCandidateAssessmentsAsync(id);
        return Ok(assessments);
    }

    // New endpoint: Get assignments for current user (all assignments for demo)
    [HttpGet("assignments/me")]
    public async Task<IActionResult> GetMyAssignments()
    {
        try
        {
            var currentEmail = GetCurrentUserEmail();
            if (string.IsNullOrWhiteSpace(currentEmail))
                return Unauthorized(new { message = "Candidate identity is required" });

            var allCandidates = await _candidateService.GetAllCandidatesAsync();
            var currentCandidate = allCandidates.FirstOrDefault(c =>
                string.Equals(c.Email, currentEmail, StringComparison.OrdinalIgnoreCase));

            if (currentCandidate == null)
                return Ok(Array.Empty<CandidateAssignmentDto>());

            var assignments = new List<CandidateAssignmentDto>();
            var candidateAssignments = await _assessmentService.GetCandidateAssessmentsAsync(currentCandidate.Id);

            foreach (var assignment in candidateAssignments)
                assignments.Add(await MapAssignmentAsync(assignment));

            return Ok(assignments);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("assessments/{candidateAssessmentId}/session")]
    public async Task<IActionResult> GetAssessmentSession(Guid candidateAssessmentId)
    {
        var assignment = await _assessmentService.GetAssignmentAsync(candidateAssessmentId);
        if (assignment == null)
            return NotFound(new { message = "Assignment not found" });

        var assessment = await GetAssessmentAsync(assignment.AssessmentId);
        if (assessment == null)
            return NotFound(new { message = "Assessment not found" });

        var questions = await GetQuestionsAsync(assignment.AssessmentId);
        var session = new CandidateAssessmentSessionDto
        {
            CandidateAssessmentId = assignment.Id,
            CandidateId = assignment.CandidateId,
            AssessmentId = assignment.AssessmentId,
            AssessmentTitle = assessment.Title,
            DurationMinutes = assessment.DurationMinutes,
            RemainingSeconds = GetRemainingSeconds(assignment, assessment.DurationMinutes),
            AllowedViolations = 3,
            Questions = questions.Select(q => new CandidateQuestionDto
            {
                Id = q.Id,
                Prompt = q.Text,
                QuestionType = q.Type,
                Marks = Math.Max(q.MaxScore, 1),
                Options = q.Options
                    .OrderBy(o => o.Order)
                    .Select(o => new CandidateQuestionOptionDto
                    {
                        Id = o.Id,
                        Text = o.Text
                    })
                    .ToList()
            }).ToList()
        };

        return Ok(session);
    }

    [HttpPost("assessments/{candidateAssessmentId}/start")]
    public async Task<IActionResult> StartAssessment(Guid candidateAssessmentId)
    {
        var assignment = await _assessmentService.GetAssignmentAsync(candidateAssessmentId);
        if (assignment == null)
            return NotFound(new { message = "Assignment not found" });

        return NoContent();
    }

    [HttpPost("assessments/{candidateAssessmentId}/submit")]
    public async Task<IActionResult> SubmitAssessment(Guid candidateAssessmentId)
    {
        var assignment = await _assessmentService.GetAssignmentAsync(candidateAssessmentId);
        if (assignment == null)
            return NotFound(new { message = "Assignment not found" });

        var success = await _assessmentService.CompleteAssessmentAsync(candidateAssessmentId);
        if (!success)
            return NotFound(new { message = "Assignment not found" });

        try
        {
            var response = await _httpClient.PostAsync(
                $"{_resultServiceBaseUrl}/api/results/assessments/{assignment.AssessmentId}/candidates/{assignment.CandidateId}/calculate",
                null);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Result calculation failed for assignment {CandidateAssessmentId}. Status {StatusCode}: {Body}",
                    candidateAssessmentId,
                    response.StatusCode,
                    body);

                return StatusCode(502, new { message = "Assessment submitted, but score calculation failed" });
            }

            var result = await response.Content.ReadFromJsonAsync<object>();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to calculate score for assignment {CandidateAssessmentId}", candidateAssessmentId);
            return StatusCode(502, new { message = "Assessment submitted, but score calculation failed" });
        }
    }

    [HttpGet("live-progress")]
    public async Task<IActionResult> GetLiveProgress()
    {
        var candidates = await _candidateService.GetAllCandidatesAsync();
        var progress = new List<AssessmentProgressDto>();

        foreach (var candidate in candidates)
        {
            var assignments = await _assessmentService.GetCandidateAssessmentsAsync(candidate.Id);
            foreach (var assignment in assignments)
            {
                var assessment = await GetAssessmentAsync(assignment.AssessmentId);
                var duration = assessment?.DurationMinutes ?? 60;
                progress.Add(new AssessmentProgressDto
                {
                    CandidateAssessmentId = assignment.Id,
                    CandidateName = candidate.Name,
                    Status = assignment.CompletedAt.HasValue ? "Submitted" : "Assigned",
                    CompletionPercent = assignment.CompletedAt.HasValue ? 100 : 0,
                    SuspiciousEvents = 0,
                    RemainingSeconds = GetRemainingSeconds(assignment, duration)
                });
            }
        }

        return Ok(progress);
    }

    [HttpPost("suspicious-activity")]
    public IActionResult ReportSuspiciousActivity([FromBody] SuspiciousActivityRequest request)
    {
        _logger.LogWarning(
            "Suspicious activity for assignment {CandidateAssessmentId}: {ViolationType} {Metadata}",
            request.CandidateAssessmentId,
            request.ViolationType,
            request.Metadata);

        return NoContent();
    }

    private async Task<CandidateAssignmentDto> MapAssignmentAsync(CandidateService.Domain.Entities.CandidateAssessment assignment)
    {
        var assessment = await GetAssessmentAsync(assignment.AssessmentId);
        var durationMinutes = assessment?.DurationMinutes ?? 60;

        return new CandidateAssignmentDto
        {
            CandidateAssessmentId = assignment.Id,
            CandidateId = assignment.CandidateId,
            AssessmentId = assignment.AssessmentId,
            AssessmentTitle = assessment?.Title ?? "Assessment",
            Status = assignment.CompletedAt.HasValue ? "Submitted" : "Assigned",
            StartTimeUtc = assignment.AssignedAt,
            SubmittedAtUtc = assignment.CompletedAt,
            RemainingSeconds = GetRemainingSeconds(assignment, durationMinutes)
        };
    }

    private async Task<AssessmentDto?> GetAssessmentAsync(Guid assessmentId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AssessmentDto>(
                $"{_assessmentServiceBaseUrl}/api/assessments/{assessmentId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load assessment {AssessmentId}", assessmentId);
            return null;
        }
    }

    private async Task<List<QuestionDto>> GetQuestionsAsync(Guid assessmentId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<QuestionDto>>(
                $"{_assessmentServiceBaseUrl}/api/assessments/{assessmentId}/questions")
                ?? new List<QuestionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load questions for assessment {AssessmentId}", assessmentId);
            return new List<QuestionDto>();
        }
    }

    private static int GetRemainingSeconds(CandidateService.Domain.Entities.CandidateAssessment assignment, int durationMinutes)
    {
        if (assignment.CompletedAt.HasValue)
            return 0;

        var elapsed = DateTime.UtcNow - assignment.AssignedAt;
        var remaining = TimeSpan.FromMinutes(Math.Max(durationMinutes, 1)) - elapsed;
        return Math.Max((int)remaining.TotalSeconds, 0);
    }

    private string? GetCurrentUserEmail()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authorization["Bearer ".Length..].Trim();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        return jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == JwtRegisteredClaimNames.Email)?.Value;
    }
}

public class CreateCandidateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AssignAssessmentRequest
{
    public Guid AssessmentId { get; set; }
}

public class AssignmentRequest
{
    public Guid CandidateId { get; set; }
    public Guid AssessmentId { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
}

public class CandidateAssignmentDto
{
    public Guid CandidateAssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public string Status { get; set; } = "Assigned";
    public DateTime? StartTimeUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public int RemainingSeconds { get; set; }
}

public class CandidateAssessmentSessionDto
{
    public Guid CandidateAssessmentId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public int RemainingSeconds { get; set; }
    public int AllowedViolations { get; set; }
    public List<CandidateQuestionDto> Questions { get; set; } = new();
}

public class CandidateQuestionDto
{
    public Guid Id { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "MCQ";
    public int Marks { get; set; }
    public List<CandidateQuestionOptionDto> Options { get; set; } = new();
}

public class CandidateQuestionOptionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class AssessmentProgressDto
{
    public Guid CandidateAssessmentId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string Status { get; set; } = "Assigned";
    public int CompletionPercent { get; set; }
    public int SuspiciousEvents { get; set; }
    public int RemainingSeconds { get; set; }
}

public class SuspiciousActivityRequest
{
    public Guid CandidateAssessmentId { get; set; }
    public string ViolationType { get; set; } = string.Empty;
    public string? Metadata { get; set; }
}

public class AssessmentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 60;
}

public class QuestionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = "MCQ";
    public int MaxScore { get; set; } = 1;
    public int Order { get; set; }
    public List<QuestionOptionDto> Options { get; set; } = new();
}

public class QuestionOptionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
}
