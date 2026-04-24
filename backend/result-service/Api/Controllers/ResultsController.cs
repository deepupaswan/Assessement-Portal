using Microsoft.AspNetCore.Mvc;
using ResultService.Application.DTOs;
using ResultService.Application.Services;
using ResultService.Domain.Entities;

namespace ResultService.Api.Controllers
{
    [ApiController]
    [Route("api/results")]
    public class ResultsController : ControllerBase
    {
        private readonly IResultService _resultService;
        private readonly ILogger<ResultsController> _logger;

        public ResultsController(IResultService resultService, ILogger<ResultsController> logger)
        {
            _resultService = resultService;
            _logger = logger;
        }

        /// <summary>
        /// Get all results
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var results = await _resultService.GetAllAsync();
                var mapper = MapToDto();
                return Ok(results.Select(mapper).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all results");
                return StatusCode(500, new { message = "Failed to get results" });
            }
        }

        [HttpGet("analytics/overview")]
        public async Task<IActionResult> GetAnalyticsOverview()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics overview");
                return StatusCode(500, new { message = "Failed to get analytics overview" });
            }
        }

        /// <summary>
        /// Get result by ID
        /// </summary>
        [HttpGet("{resultId}")]
        public async Task<IActionResult> GetById(Guid resultId)
        {
            try
            {
                var result = await _resultService.GetByIdAsync(resultId);
                if (result == null)
                    return NotFound(new { message = "Result not found" });

                var mapper = MapToDto();
                return Ok(mapper(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting result {ResultId}", resultId);
                return StatusCode(500, new { message = "Failed to get result" });
            }
        }

        /// <summary>
        /// Get results for a candidate
        /// </summary>
        [HttpGet("candidates/{candidateId}")]
        public async Task<IActionResult> GetCandidateResults(Guid candidateId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting results for candidate {CandidateId}", candidateId);
                return StatusCode(500, new { message = "Failed to get candidate results" });
            }
        }

        /// <summary>
        /// Get results for an assessment
        /// </summary>
        [HttpGet("assessments/{assessmentId}")]
        public async Task<IActionResult> GetAssessmentResults(Guid assessmentId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting results for assessment {AssessmentId}", assessmentId);
                return StatusCode(500, new { message = "Failed to get assessment results" });
            }
        }

        /// <summary>
        /// Get result for a specific candidate and assessment
        /// </summary>
        [HttpGet("assessments/{assessmentId}/candidates/{candidateId}")]
        public async Task<IActionResult> GetCandidateAssessmentResult(Guid assessmentId, Guid candidateId)
        {
            try
            {
                var result = await _resultService.GetByCandidateAndAssessmentAsync(candidateId, assessmentId);
                if (result == null)
                    return NotFound(new { message = "Result not found" });

                var mapper = MapToDto();
                return Ok(mapper(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting result for candidate {CandidateId} in assessment {AssessmentId}",
                    candidateId, assessmentId);
                return StatusCode(500, new { message = "Failed to get result" });
            }
        }

        /// <summary>
        /// Calculate and publish result for a candidate in an assessment
        /// </summary>
        [HttpPost("assessments/{assessmentId}/candidates/{candidateId}/calculate")]
        public async Task<IActionResult> CalculateAndPublishResult(Guid assessmentId, Guid candidateId)
        {
            try
            {
                var result = await _resultService.CalculateAndPublishAsync(candidateId, assessmentId);
                var mapper = MapToDto();
                return Ok(mapper(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating result for candidate {CandidateId} in assessment {AssessmentId}",
                    candidateId, assessmentId);
                return StatusCode(500, new { message = "Failed to calculate result: " + ex.Message });
            }
        }

        /// <summary>
        /// Publish a result
        /// </summary>
        [HttpPost("{resultId}/publish")]
        public async Task<IActionResult> PublishResult(Guid resultId)
        {
            try
            {
                var result = await _resultService.PublishResultAsync(resultId);
                var mapper = MapToDto();
                return Ok(mapper(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing result {ResultId}", resultId);
                return StatusCode(500, new { message = "Failed to publish result" });
            }
        }

        /// <summary>
        /// Get passed candidates for an assessment
        /// </summary>
        [HttpGet("assessments/{assessmentId}/passed")]
        public async Task<IActionResult> GetPassedCandidates(Guid assessmentId)
        {
            try
            {
                var results = await _resultService.GetPassedCandidatesAsync(assessmentId);
                var mapper = MapToDto();
                return Ok(results.Select(mapper).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting passed candidates for assessment {AssessmentId}", assessmentId);
                return StatusCode(500, new { message = "Failed to get passed candidates" });
            }
        }

        /// <summary>
        /// Get failed candidates for an assessment
        /// </summary>
        [HttpGet("assessments/{assessmentId}/failed")]
        public async Task<IActionResult> GetFailedCandidates(Guid assessmentId)
        {
            try
            {
                var results = await _resultService.GetFailedCandidatesAsync(assessmentId);
                var mapper = MapToDto();
                return Ok(results.Select(mapper).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting failed candidates for assessment {AssessmentId}", assessmentId);
                return StatusCode(500, new { message = "Failed to get failed candidates" });
            }
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

