using Microsoft.AspNetCore.SignalR;

namespace AssessmentService.Api.Hubs
{
    public class AssessmentHub : Hub
    {
        private const string AdminMonitoringChannel = "admin-monitoring";

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinAdminMonitoringChannel()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminMonitoringChannel);
            await Clients.Group(AdminMonitoringChannel).SendAsync("AdminJoined", $"Admin connected at {DateTime.UtcNow}");
        }

        public async Task JoinAssessment(string assessmentId, string candidateName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, assessmentId);
            await Clients.Group(assessmentId).SendAsync("CandidateJoined", candidateName);
        }

        public async Task JoinAssessmentChannel(string candidateAssessmentId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, candidateAssessmentId);
            await Clients.Group(AdminMonitoringChannel).SendAsync("CandidateJoined",
                new { candidateAssessmentId, connectedAt = DateTime.UtcNow });
        }

        public async Task UpdateProgress(string assessmentId, int progress)
        {
            var payload = new
            {
                candidateAssessmentId = assessmentId,
                candidateName = "Candidate",
                status = progress >= 100 ? "Submitted" : "InProgress",
                completionPercent = progress,
                suspiciousEvents = 0,
                remainingSeconds = 0
            };

            await Clients.Group(assessmentId).SendAsync("ProgressUpdated", payload);
            await Clients.Group(AdminMonitoringChannel).SendAsync("ProgressUpdated", payload);
        }

        public async Task AssessmentCompleted(string assessmentId, string candidateName, int score)
        {
            await Clients.Group(assessmentId).SendAsync("AssessmentCompleted", 
                new { candidateName, score, completedAt = DateTime.UtcNow });
            // Also notify admins
            await Clients.Group(AdminMonitoringChannel).SendAsync("AssessmentCompleted",
                new { assessmentId, candidateName, score, completedAt = DateTime.UtcNow });
        }

        public async Task ReportSuspiciousActivity(string assessmentId, string candidateName, string violationType)
        {
            await Clients.Group(assessmentId).SendAsync("SuspiciousActivityDetected",
                new { candidateName, violationType, reportedAt = DateTime.UtcNow });
            // Also notify admins
            await Clients.Group(AdminMonitoringChannel).SendAsync("SuspiciousActivityDetected",
                new { assessmentId, candidateName, violationType, reportedAt = DateTime.UtcNow });
        }
    }
}
