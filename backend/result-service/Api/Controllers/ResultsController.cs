using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ResultService.Application.DTOs;
using ResultService.Application.Services;
using ResultService.Domain.Entities;

namespace ResultService.Api.Controllers
{
    [ApiController]
    [Route("api/results")]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly IResultService _resultService;

        public ResultsController(IResultService resultService)
        {
            _resultService = resultService;
        }

        /// <summary>
        /// Get all results
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var results = await _resultService.GetAllAsync();
            var mapper = MapToDto();
            return Ok(results.Select(mapper).ToList());
        }

        [HttpGet("analytics/overview")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAnalyticsOverview()
        {
            var results = await _resultService.GetAllAsync();
            var total = results.Count;
            return Ok(new
            {
                totalCandidates = results.Select(r => r.CandidateId).Distinct().Count(),
                averageScore = total > 0 ? Math.Round(results.Average(r => r.Percentage), 2) : 0,
                suspiciousCases = 0,
                completionRate = total > 0 ? 100 : 0
            });
        }

        /// <summary>
        /// Get result by ID
        /// </summary>
        [HttpGet("{resultId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(Guid resultId)
        {
            var result = await _resultService.GetByIdAsync(resultId);
            if (result == null)
                return NotFound(new { message = "Result not found" });

            var mapper = MapToDto();
            return Ok(mapper(result));
        }

        /// <summary>
        /// Get results for a candidate
        /// </summary>
        [HttpGet("candidates/{candidateId}")]
        [Authorize(Roles = "Admin,Candidate")]
        public async Task<IActionResult> GetCandidateResults(Guid candidateId)
        {
            var results = await _resultService.GetByCandidateIdAsync(candidateId);
            var performance = new CandidatePerformanceDto
            {
                CandidateId = candidateId,
                Results = results.Select(r => new ResultSummaryDto
                {
                    ResultId = r.Id,
                    CandidateId = r.CandidateId,
                    AssessmentId = r.AssessmentId,
                    Score = r.Score,
                    MaxScore = r.MaxScore,
                    Percentage = r.Percentage,
                    IsPassed = r.IsPassed,
                    Remarks = r.Remarks ?? string.Empty,
                    PublishedAt = r.PublishedAt
                }).ToList(),
                AveragePercentage = results.Any() ? Math.Round(results.Average(r => r.Percentage), 2) : 0,
                TotalAssessmentsTaken = results.Count,
                TotalPassed = results.Count(r => r.IsPassed),
                TotalFailed = results.Count(r => !r.IsPassed)
            };
            return Ok(performance);
        }

        /// <summary>
        /// Get results for an assessment
        /// </summary>
        [HttpGet("assessments/{assessmentId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAssessmentResults(Guid assessmentId)
        {
            var results = await _resultService.GetByAssessmentIdAsync(assessmentId);
            var analytics = new AssessmentAnalyticsDto
            {
                AssessmentId = assessmentId,
                TotalCandidates = results.Count,
                PassedCount = results.Count(r => r.IsPassed),
                FailedCount = results.Count(r => !r.IsPassed),
                AverageScore = results.Any() ? Math.Round((decimal)results.Average(r => r.Score), 2) : 0,
                AveragePercentage = results.Any() ? Math.Round(results.Average(r => r.Percentage), 2) : 0,
                HighestScore = results.Any() ? results.Max(r => r.Score) : 0,
                LowestScore = results.Any() ? results.Min(r => r.Score) : 0
            };
            return Ok(analytics);
        }

        /// <summary>
        /// Get result for a specific candidate and assessment
        /// </summary>
        [HttpGet("assessments/{assessmentId}/candidates/{candidateId}")]
        [Authorize(Roles = "Admin,Candidate")]
        public async Task<IActionResult> GetCandidateAssessmentResult(Guid assessmentId, Guid candidateId)
        {
            var result = await _resultService.GetByCandidateAndAssessmentAsync(candidateId, assessmentId);
            if (result == null)
                return NotFound(new { message = "Result not found" });

            var mapper = MapToDto();
            return Ok(mapper(result));
        }

        /// <summary>
        /// Calculate and publish result for a candidate in an assessment
        /// </summary>
        [HttpPost("assessments/{assessmentId}/candidates/{candidateId}/calculate")]
        [Authorize(Roles = "Admin,Candidate")]
        public async Task<IActionResult> CalculateAndPublishResult(Guid assessmentId, Guid candidateId)
        {
            var result = await _resultService.CalculateAndPublishAsync(candidateId, assessmentId);
            var mapper = MapToDto();
            return Ok(mapper(result));
        }

        /// <summary>
        /// Publish a result
        /// </summary>
        [HttpPost("{resultId}/publish")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PublishResult(Guid resultId)
        {
            var result = await _resultService.PublishResultAsync(resultId);
            var mapper = MapToDto();
            return Ok(mapper(result));
        }

        /// <summary>
        /// Get passed candidates for an assessment
        /// </summary>
        [HttpGet("assessments/{assessmentId}/passed")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPassedCandidates(Guid assessmentId)
        {
            var results = await _resultService.GetPassedCandidatesAsync(assessmentId);
            var mapper = MapToDto();
            return Ok(results.Select(mapper).ToList());
        }

        /// <summary>
        /// Get failed candidates for an assessment
        /// </summary>
        [HttpGet("assessments/{assessmentId}/failed")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetFailedCandidates(Guid assessmentId)
        {
            var results = await _resultService.GetFailedCandidatesAsync(assessmentId);
            var mapper = MapToDto();
            return Ok(results.Select(mapper).ToList());
        }

        // Helper method to map Result entity to DTO
        private Func<Result, ResultDto> MapToDto()
        {
            return result => new ResultDto
            {
                Id = result.Id,
                CandidateId = result.CandidateId,
                AssessmentId = result.AssessmentId,
                Score = result.Score,
                MaxScore = result.MaxScore,
                Percentage = result.Percentage,
                Status = result.Status,
                TotalQuestions = result.TotalQuestions,
                CorrectAnswers = result.CorrectAnswers,
                WrongAnswers = result.WrongAnswers,
                SkippedQuestions = result.SkippedQuestions,
                StartedAt = result.StartedAt,
                CompletedAt = result.CompletedAt,
                EvaluatedAt = result.EvaluatedAt,
                CalculatedAt = result.CalculatedAt,
                PublishedAt = result.PublishedAt,
                Remarks = result.Remarks,
                IsPassed = result.IsPassed,
                PassingPercentage = result.PassingPercentage
            };
        }
    }
}

