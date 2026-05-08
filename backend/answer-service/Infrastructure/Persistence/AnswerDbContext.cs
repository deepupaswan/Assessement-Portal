using Microsoft.EntityFrameworkCore;
using AnswerService.Domain.Entities;

namespace AnswerService.Infrastructure.Persistence
{
    public class AnswerDbContext : DbContext
    {
        public AnswerDbContext(DbContextOptions<AnswerDbContext> options) : base(options) { }

        public DbSet<Answer> Answers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Answer>()
                .HasIndex(a => a.AssessmentId);

            modelBuilder.Entity<Answer>()
                .HasIndex(a => a.CandidateId);

            modelBuilder.Entity<Answer>()
                .HasIndex(a => a.QuestionId);

            modelBuilder.Entity<Answer>()
                .HasIndex(a => new { a.CandidateId, a.AssessmentId });
        }
    }
}