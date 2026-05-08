using AssessmentService.Application.DTOs;
using AssessmentService.Application.Services;
using AssessmentService.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssessmentService.Api.Controllers;

/// <summary>
/// Assessment management controller with pagination support.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssessmentsController : ControllerBase
{
    private readonly IAssessmentService _assessmentService;

    public AssessmentsController(IAssessmentService assessmentService)
    {
        _assessmentService = assessmentService;
    }

    /// <summary>
    /// Gets paginated list of assessments (Admin only).
    /// </summary>
    /// <param name="pageNumber">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20, max 100)</param>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAssessments([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _assessmentService.GetAllAssessmentsAsync(pageNumber, pageSize);

        // Add pagination metadata as response headers
        Response.Headers.Add("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Add("X-Page-Number", result.PageNumber.ToString());
        Response.Headers.Add("X-Page-Size", result.PageSize.ToString());
        Response.Headers.Add("X-Total-Pages", result.TotalPages.ToString());
        Response.Headers.Add("X-Has-Next-Page", result.HasNextPage.ToString().ToLower());
        Response.Headers.Add("X-Has-Previous-Page", result.HasPreviousPage.ToString().ToLower());

        return Ok(result.Items);
    }

    /// <summary>
    /// Gets assessment summary by ID (for list/preview, no questions).
    /// </summary>
    [HttpGet("{id}/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAssessmentSummary(Guid id)
    {
        // For summary, use the full assessment object as temporary mapping
        var assessment = await _assessmentService.GetAssessmentByIdAsync(id);
        if (assessment == null)
            return NotFound(new { message = "Assessment not found" });

        return Ok(new
        {
            assessment.Id,
            assessment.Title,
            assessment.Description,
            assessment.DurationMinutes,
            assessment.IsPublished,
            QuestionCount = assessment.Questions.Count,
            assessment.RandomizeQuestions,
            assessment.CreatedAt
        });
    }

    /// <summary>
    /// Gets full assessment details with questions and options (for exam taking).
    /// </summary>
    [HttpGet("{id}/details")]
    [Authorize(Roles = "Admin,Candidate")]
    public async Task<IActionResult> GetAssessmentDetails(Guid id)
    {
        var assessment = await _assessmentService.GetAssessmentDetailsAsync(id);
        if (assessment == null)
            return NotFound(new { message = "Assessment not found" });

        return Ok(assessment);
    }

    /// <summary>
    /// Gets basic assessment by ID (legacy endpoint for compatibility).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Candidate")]
    public async Task<IActionResult> GetAssessment(Guid id)
    {
        var assessment = await _assessmentService.GetAssessmentByIdAsync(id);
        if (assessment == null)
            return NotFound(new { message = "Assessment not found" });

        return Ok(new
        {
            assessment.Id,
            assessment.Title,
            assessment.Description,
            assessment.DurationMinutes,
            assessment.IsPublished,
            assessment.RandomizeQuestions,
            assessment.CreatedAt,
            assessment.UpdatedAt,
            QuestionCount = assessment.Questions.Count
        });
    }

    /// <summary>
    /// Creates a new assessment (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAssessment([FromBody] CreateAssessmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title is required" });

        if (request.Title.Length > 255)
            return BadRequest(new { message = "Title cannot exceed 255 characters" });

        if (request.DurationMinutes <= 0)
            return BadRequest(new { message = "Duration must be greater than 0" });

        var assessment = await _assessmentService.CreateAssessmentAsync(
            request.Title,
            request.Description,
            request.DurationMinutes,
            request.RandomizeQuestions);

        return CreatedAtAction(nameof(GetAssessmentDetails),
            new { id = assessment.Id }, assessment);
    }

    /// <summary>
    /// Updates an assessment (Admin only).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAssessment(Guid id, [FromBody] UpdateAssessmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Title is required" });

        if (request.Title.Length > 255)
            return BadRequest(new { message = "Title cannot exceed 255 characters" });

        var success = await _assessmentService.UpdateAssessmentAsync(
            id,
            request.Title,
            request.Description,
            request.DurationMinutes,
            request.RandomizeQuestions);

        if (!success)
            return NotFound(new { message = "Assessment not found" });

        return NoContent();
    }

    /// <summary>
    /// Deletes an assessment and related data (Admin only).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAssessment(Guid id)
    {
        var success = await _assessmentService.DeleteAssessmentAsync(id);
        if (!success)
            return NotFound(new { message = "Assessment not found" });

        return NoContent();
    }
}
