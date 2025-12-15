using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GrapheneTrace.Models;

namespace GrapheneTrace.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ClinicianPatientAssignment> ClinicianPatientAssignments { get; set; }
        public DbSet<UserDomain> UserDomains { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ClinicianPatientAssignment>(e =>
            {
                e.HasIndex(x => new { x.ClinicianUserId, x.PatientUserId }).IsUnique();
            });

            builder.Entity<UserDomain>(e =>
            {
                e.HasKey(x => x.UserId);
                e.Property(x => x.Domain).HasMaxLength(50);
            });

            builder.Entity<SystemSettings>(e =>
            {
                e.HasKey(x => x.Id);
            });
        }
    }
}
