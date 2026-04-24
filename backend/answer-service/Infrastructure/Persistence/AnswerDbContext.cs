using Microsoft.EntityFrameworkCore;
using AnswerService.Domain.Entities;

namespace AnswerService.Infrastructure.Persistence
{
    public class AnswerDbContext : DbContext
    {
        public AnswerDbContext(DbContextOptions<AnswerDbContext> options) : base(options) { }

        public DbSet<Answer> Answers { get; set; }
    }
}