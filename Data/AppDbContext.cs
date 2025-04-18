﻿/*
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
        public DbSet<Congregation> Congregations { get; set; }
        public DbSet<Placement> Placements { get; set; }
        public DbSet<MeetingSchedule> MeetingSchedules { get; set; }
        public DbSet<Publisher> Publishers { get; set; }
        public DbSet<ServiceGroup> ServiceGroups { get; set; }
        public DbSet<OCLMPart> OCLMParts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
