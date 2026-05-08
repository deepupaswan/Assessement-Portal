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
    private static readonly HashSet<string> AllowedSortBy = new(StringComparer.OrdinalIgnoreCase)
    {
        "score",
        "createdAt",
        "name",
        "email"
    };

    private static readonly HashSet<string> AllowedSortOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        "asc",
        "desc"
    };

    private readonly ICandidateService _candidateService;
    private readonly ICandidateSearchService _candidateSearchService;
    private readonly ICandidateAssessmentService _assessmentService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CandidatesController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly HttpClient _httpClient;
    private readonly string _assessmentServiceBaseUrl;
    private readonly string _resultServiceBaseUrl;

    public CandidatesController(
        ICandidateService candidateService,
        ICandidateSearchService candidateSearchService,
        ICandidateAssessmentService assessmentService,
        IPublishEndpoint publishEndpoint,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<CandidatesController> logger)
    {
        _candidateService = candidateService;
        _candidateSearchService = candidateSearchService;
        _assessmentService = assessmentService;
        _publishEndpoint = publishEndpoint;
        _environment = environment;
        _httpClient = httpClientFactory.CreateClient("InternalServices");
        _assessmentServiceBaseUrl = (configuration["ServiceUrls:AssessmentService"] ?? "http://localhost:5098").TrimEnd('/');
        _resultServiceBaseUrl = (configuration["ServiceUrls:ResultService"] ?? "http://localhost:5160").TrimEnd('/');
        _logger = logger;
    }

    /// <summary>
    /// Searches candidates using Elasticsearch with pagination, filtering, and sorting.
    /// </summary>
    /// <param name="q">Free-text query for candidate name or email.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="size">Page size (1-100).</param>
    /// <param name="email">Optional exact email filter (case-insensitive).</param>
    /// <param name="createdFromUtc">Optional lower bound for candidate created timestamp (UTC).</param>
    /// <param name="createdToUtc">Optional upper bound for candidate created timestamp (UTC).</param>
    /// <param name="sortBy">Sort field. Allowed values: score, createdAt, name, email.</param>
    /// <param name="sortOrder">Sort direction. Allowed values: asc, desc.</param>
    [HttpGet("search")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SearchCandidates(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? email = null,
        [FromQuery] DateTime? createdFromUtc = null,
        [FromQuery] DateTime? createdToUtc = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Search query is required" });

        if (createdFromUtc.HasValue && createdToUtc.HasValue && createdFromUtc > createdToUtc)
            return BadRequest(new { message = "createdFromUtc must be earlier than or equal to createdToUtc" });

        if (!string.IsNullOrWhiteSpace(sortBy) && !AllowedSortBy.Contains(sortBy))
            return BadRequest(new { message = "sortBy must be one of: score, createdAt, name, email" });

        if (!string.IsNullOrWhiteSpace(sortOrder) && !AllowedSortOrder.Contains(sortOrder))
            return BadRequest(new { message = "sortOrder must be one of: asc, desc" });

        try
        {
            var searchResult = await _candidateSearchService.SearchCandidatesAsync(
                q,
                page,
                size,
                email,
                createdFromUtc,
                createdToUtc,
                sortBy,
                sortOrder);
            return Ok(searchResult);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Elasticsearch search failed for query {Query}", q);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Search is temporarily unavailable" });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Elasticsearch search timed out for query {Query}", q);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Search timed out" });
        }
    }

    [HttpPost("search/reset-reindex")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetAndReindexCandidates()
    {
        if (_environment.IsProduction())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Reset and reindex is disabled in production" });

        try
        {
            await _candidateSearchService.ResetIndexAsync();
            var candidates = await _candidateService.GetAllCandidatesAsync();
            var result = await _candidateSearchService.ReindexCandidatesAsync(candidates);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Elasticsearch reset and reindex operation failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Reset and reindex is temporarily unavailable" });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Elasticsearch reset and reindex operation timed out");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Reset and reindex timed out" });
        }
    }

    [HttpPost("search/reindex")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReindexCandidates()
    {
        try
        {
            var candidates = await _candidateService.GetAllCandidatesAsync();
            var result = await _candidateSearchService.ReindexCandidatesAsync(candidates);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Elasticsearch reindex operation failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Reindex is temporarily unavailable" });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Elasticsearch reindex operation timed out");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Reindex timed out" });
        }
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

    [HttpGet("assignments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAssignments()
    {
        var assignments = await _assessmentService.GetAllAssignmentsAsync();
        var response = new List<CandidateAssignmentDto>();

        foreach (var assignment in assignments)
            response.Add(await MapAssignmentAsync(assignment));

        return Ok(response);
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

        var assignment = await _assessmentService.AssignAssessmentAsync(
            request.CandidateId,
            request.AssessmentId,
            request.ScheduledAtUtc);
        await PublishAssignmentCreatedEventAsync(assignment);
        var response = await MapAssignmentAsync(assignment);
        return Ok(response);
    }

    [HttpPut("assignments/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAssignment(Guid id, [FromBody] AssignmentRequest request)
    {
        if (request.CandidateId == Guid.Empty || request.AssessmentId == Guid.Empty)
            return BadRequest(new { message = "Candidate ID and Assessment ID are required" });

        var candidate = await _candidateService.GetCandidateByIdAsync(request.CandidateId);
        if (candidate == null)
            return NotFound(new { message = "Candidate not found" });

        try
        {
            var assignment = await _assessmentService.UpdateAssignmentAsync(
                id,
                request.CandidateId,
                request.AssessmentId,
                request.ScheduledAtUtc);

            if (assignment == null)
                return NotFound(new { message = "Assignment not found" });

            var response = await MapAssignmentAsync(assignment);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("assignments/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAssignment(Guid id)
    {
        try
        {
            var deleted = await _assessmentService.DeleteAssignmentAsync(id);
            if (!deleted)
                return NotFound(new { message = "Assignment not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
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

        if (assignment.ScheduledAtUtc.HasValue && assignment.ScheduledAtUtc.Value > DateTime.UtcNow)
            return Conflict(new { message = "This assessment is scheduled for later." });

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

        if (assignment.ScheduledAtUtc.HasValue && assignment.ScheduledAtUtc.Value > DateTime.UtcNow)
            return Conflict(new { message = "This assessment is scheduled for later." });

        var started = await _assessmentService.StartAssessmentAsync(candidateAssessmentId);
        if (!started)
            return NotFound(new { message = "Assignment not found" });

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
                    Status = GetAssignmentStatus(assignment),
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
            CandidateName = assignment.Candidate?.Name ?? string.Empty,
            AssessmentId = assignment.AssessmentId,
            AssessmentTitle = assessment?.Title ?? "Assessment",
            Status = GetAssignmentStatus(assignment),
            AssignedAtUtc = assignment.AssignedAt,
            ScheduledAtUtc = assignment.ScheduledAtUtc,
            StartTimeUtc = assignment.StartedAtUtc ?? assignment.AssignedAt,
            StartedAtUtc = assignment.StartedAtUtc,
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

        var totalSeconds = Math.Max(durationMinutes, 1) * 60;

        if (!assignment.StartedAtUtc.HasValue)
            return totalSeconds;

        var elapsed = DateTime.UtcNow - assignment.StartedAtUtc.Value;
        var remaining = TimeSpan.FromMinutes(Math.Max(durationMinutes, 1)) - elapsed;
        return Math.Max((int)remaining.TotalSeconds, 0);
    }

    private static string GetAssignmentStatus(CandidateService.Domain.Entities.CandidateAssessment assignment)
    {
        if (assignment.CompletedAt.HasValue)
            return "Submitted";

        if (assignment.StartedAtUtc.HasValue)
            return "InProgress";

        if (assignment.ScheduledAtUtc.HasValue && assignment.ScheduledAtUtc.Value > DateTime.UtcNow)
            return "Scheduled";

        return "Assigned";
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
