using Microsoft.EntityFrameworkCore;
using ExamGenerator.Models;

namespace ExamGenerator.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ExamData> ExamData { get; set; }
}