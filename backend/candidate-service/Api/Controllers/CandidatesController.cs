using CandidateService.Application.Events;
using CandidateService.Application.Services;
using CandidateService.Api.Models;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CandidateService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CandidatesController : ControllerBase
{
    private readonly ICandidateService _candidateService;
    private readonly ICandidateAssessmentService _assessmentService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CandidatesController> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _assessmentServiceBaseUrl;
    private readonly string _resultServiceBaseUrl;

    public CandidatesController(
        ICandidateService candidateService,
        ICandidateAssessmentService assessmentService,
        IPublishEndpoint publishEndpoint,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CandidatesController> logger)
    {
        _candidateService = candidateService;
        _assessmentService = assessmentService;
        _publishEndpoint = publishEndpoint;
        _httpClient = httpClientFactory.CreateClient("InternalServices");
        _assessmentServiceBaseUrl = (configuration["ServiceUrls:AssessmentService"] ?? "http://localhost:5098").TrimEnd('/');
        _resultServiceBaseUrl = (configuration["ServiceUrls:ResultService"] ?? "http://localhost:5160").TrimEnd('/');
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllCandidates()
    {
        var candidates = await _candidateService.GetAllCandidatesAsync();
        return Ok(candidates);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetCandidate(Guid id)
    {
        var candidate = await _candidateService.GetCandidateByIdAsync(id);
        if (candidate == null)
            return NotFound();

        return Ok(candidate);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCandidate([FromBody] CreateCandidateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Name and Email are required");

        var existingCandidate = await _candidateService.GetCandidateByEmailAsync(request.Email);
        if (existingCandidate != null)
            return Conflict(new { message = "Candidate with this email already exists" });

        var candidate = await _candidateService.CreateCandidateAsync(request.Name, request.Email);
        return CreatedAtAction(nameof(GetCandidate), new { id = candidate.Id }, candidate);
    }

    [HttpPost("{id}/assessments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignAssessment(Guid id, [FromBody] AssignAssessmentRequest request)
    {
        var assignment = await _assessmentService.AssignAssessmentAsync(id, request.AssessmentId);
        await PublishAssignmentCreatedEventAsync(assignment);
        return Ok(assignment);
    }

    [HttpPost("assignments")]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetCandidateAssessments(Guid id)
    {
        var assessments = await _assessmentService.GetCandidateAssessmentsAsync(id);
        return Ok(assessments);
    }

    // New endpoint: Get assignments for current user (all assignments for demo)
    [HttpGet("assignments/me")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> GetMyAssignments()
    {
        var currentCandidate = await GetCurrentCandidateAsync();

        if (currentCandidate == null)
            return Ok(Array.Empty<CandidateAssignmentDto>());

        var assignments = new List<CandidateAssignmentDto>();
        var candidateAssignments = await _assessmentService.GetCandidateAssessmentsAsync(currentCandidate.Id);

        foreach (var assignment in candidateAssignments)
            assignments.Add(await MapAssignmentAsync(assignment));

        return Ok(assignments);
    }

    [HttpGet("assessments/{candidateAssessmentId}/session")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> GetAssessmentSession(Guid candidateAssessmentId)
    {
        var currentCandidate = await GetCurrentCandidateAsync();
        if (currentCandidate == null)
            return Unauthorized(new { message = "Candidate identity is required" });

        var assignment = await _assessmentService.GetAssignmentAsync(candidateAssessmentId);
        if (assignment == null)
            return NotFound(new { message = "Assignment not found" });

        if (assignment.CandidateId != currentCandidate.Id)
            return Forbid();

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
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> StartAssessment(Guid candidateAssessmentId)
    {
        var currentCandidate = await GetCurrentCandidateAsync();
        if (currentCandidate == null)
            return Unauthorized(new { message = "Candidate identity is required" });

        var assignment = await _assessmentService.GetAssignmentAsync(candidateAssessmentId);
        if (assignment == null)
            return NotFound(new { message = "Assignment not found" });

        if (assignment.CandidateId != currentCandidate.Id)
            return Forbid();

        return NoContent();
    }

    [HttpPost("assessments/{candidateAssessmentId}/submit")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> SubmitAssessment(Guid candidateAssessmentId)
    {
        var currentCandidate = await GetCurrentCandidateAsync();
        if (currentCandidate == null)
            return Unauthorized(new { message = "Candidate identity is required" });

        var assignment = await _assessmentService.GetAssignmentAsync(candidateAssessmentId);
        if (assignment == null)
            return NotFound(new { message = "Assignment not found" });

        if (assignment.CandidateId != currentCandidate.Id)
            return Forbid();

        var success = await _assessmentService.CompleteAssessmentAsync(candidateAssessmentId);
        if (!success)
            return NotFound(new { message = "Assignment not found" });

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"{_resultServiceBaseUrl}/api/results/assessments/{assignment.AssessmentId}/candidates/{assignment.CandidateId}/calculate");
        using var response = await _httpClient.SendAsync(request);

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

    [HttpGet("live-progress")]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> ReportSuspiciousActivity([FromBody] SuspiciousActivityRequest request)
    {
        var currentCandidate = await GetCurrentCandidateAsync();
        if (currentCandidate == null)
            return Unauthorized(new { message = "Candidate identity is required" });

        var assignment = await _assessmentService.GetAssignmentAsync(request.CandidateAssessmentId);
        if (assignment == null)
            return NotFound(new { message = "Assignment not found" });

        if (assignment.CandidateId != currentCandidate.Id)
            return Forbid();

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
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"{_assessmentServiceBaseUrl}/api/assessments/{assessmentId}");
        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Assessment service returned {StatusCode} for assessment {AssessmentId}",
                response.StatusCode,
                assessmentId);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AssessmentDto>();
    }

    private async Task<List<QuestionDto>> GetQuestionsAsync(Guid assessmentId)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"{_assessmentServiceBaseUrl}/api/assessments/{assessmentId}/questions");
        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Assessment service returned {StatusCode} for questions of assessment {AssessmentId}",
                response.StatusCode,
                assessmentId);
            return new List<QuestionDto>();
        }

        return await response.Content.ReadFromJsonAsync<List<QuestionDto>>() ?? new List<QuestionDto>();
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
        return User.FindFirstValue(ClaimTypes.Email);
    }

    private async Task<CandidateService.Domain.Entities.Candidate?> GetCurrentCandidateAsync()
    {
        var currentEmail = GetCurrentUserEmail();
        if (string.IsNullOrWhiteSpace(currentEmail))
            return null;

        return await _candidateService.GetCandidateByEmailAsync(currentEmail);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        var token = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            return request;
        }

        if (AuthenticationHeaderValue.TryParse(token, out var parsed) &&
            "Bearer".Equals(parsed.Scheme, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(parsed.Parameter))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", parsed.Parameter);
        }

        return request;
    }
}
