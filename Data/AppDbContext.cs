/*
    Khronos 4 by Raymond Hernandez
    January 27, 2025
*/

using Khronos4.Models;
using Microsoft.EntityFrameworkCore;

namespace Khronos4.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<BlogRevision> BlogRevisions { get; set; }
        public DbSet<Householder> Householders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
