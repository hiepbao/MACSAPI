using MACSAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml;

namespace MACSAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<HistoryCar> HistoryCar { get; set; }
        public DbSet<FileModel> FileModel { get; set; }
        public DbSet<TaskAssignmentGroup> TaskAssignmentGroups { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
    }


}