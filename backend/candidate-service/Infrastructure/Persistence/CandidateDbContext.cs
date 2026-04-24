using CandidateService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CandidateService.Infrastructure.Persistence
{
    public class CandidateDbContext : DbContext
    {
        public CandidateDbContext(DbContextOptions<CandidateDbContext> options) : base(options) { }

        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<CandidateAssessment> CandidateAssessments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CandidateAssessment>()
                .HasOne(ca => ca.Candidate)
                .WithMany()
                .HasForeignKey(ca => ca.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}