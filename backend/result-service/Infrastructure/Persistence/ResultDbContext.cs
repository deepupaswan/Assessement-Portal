using Microsoft.EntityFrameworkCore;
using ResultService.Domain.Entities;

namespace ResultService.Infrastructure.Persistence
{
    public class ResultDbContext : DbContext
    {
        public ResultDbContext(DbContextOptions<ResultDbContext> options) : base(options) { }

        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Result>()
                .HasIndex(r => r.CandidateId);

            modelBuilder.Entity<Result>()
                .HasIndex(r => r.AssessmentId);

            modelBuilder.Entity<Result>()
                .HasIndex(r => new { r.CandidateId, r.AssessmentId });
        }
    }
}