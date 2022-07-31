using Microsoft.EntityFrameworkCore;
using Wilgysef.Stalk.Core.Models;
using Wilgysef.Stalk.Core.Models.Jobs;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.EntityFrameworkCore;

public class StalkDbContext : DbContext, IStalkDbContext
{
    public DbSet<Job> Jobs { get; set; } = null!;
    public DbSet<JobTask> JobTasks { get; set; } = null!;

    public StalkDbContext(DbContextOptions<StalkDbContext> options) : base(options) { }
}