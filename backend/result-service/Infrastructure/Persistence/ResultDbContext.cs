using Microsoft.EntityFrameworkCore;
using ResultService.Domain.Entities;

namespace ResultService.Infrastructure.Persistence
{
    public class ResultDbContext : DbContext
    {
        public ResultDbContext(DbContextOptions<ResultDbContext> options) : base(options) { }

        public DbSet<Result> Results { get; set; }
    }
}