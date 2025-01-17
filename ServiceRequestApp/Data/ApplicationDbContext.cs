using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServiceRequestApp.Models;

namespace ServiceRequestApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<AcceptedRequest> AcceptedRequests { get; set; }
        //Not implemented yet
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ServiceRequest>()
                .HasOne(r => r.Requester)
                .WithMany(u => u.Requests)
                .HasForeignKey(r => r.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AcceptedRequest>()
                .HasOne(ar => ar.ServiceRequest)
                .WithOne(sr => sr.AcceptedRequest)
                .HasForeignKey<AcceptedRequest>(ar => ar.ServiceRequestId);

            modelBuilder.Entity<AcceptedRequest>()
                .HasOne(ar => ar.Provider)
                .WithMany(u => u.AcceptedRequests)
                .HasForeignKey(ar => ar.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
            //Not implemented yet
            modelBuilder.Entity<Review>()
                .HasOne(r => r.ServiceRequest)
                .WithMany()
                .HasForeignKey(r => r.ServiceRequestId)
                .OnDelete(DeleteBehavior.Restrict);
            //Not implemented yet
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
            //Not implemented yet
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reviewee)
                .WithMany()
                .HasForeignKey(r => r.RevieweeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}


