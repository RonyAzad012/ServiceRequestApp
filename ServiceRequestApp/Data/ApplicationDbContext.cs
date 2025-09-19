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
        public DbSet<ServiceRequestApplication> ServiceRequestApplications { get; set; }
        //Not implemented yet
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Message> Messages { get; set; }

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

            modelBuilder.Entity<ServiceRequest>()
                .HasOne(r => r.Category)
                .WithMany(c => c.ServiceRequests)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ServiceRequestApplication>()
                .HasIndex(a => new { a.ServiceRequestId, a.ProviderId })
                .IsUnique();

            modelBuilder.Entity<ServiceRequestApplication>()
                .HasOne(a => a.ServiceRequest)
                .WithMany()
                .HasForeignKey(a => a.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ServiceRequestApplication>()
                .HasOne(a => a.Provider)
                .WithMany()
                .HasForeignKey(a => a.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.ServiceRequest)
                .WithMany()
                .HasForeignKey(m => m.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}




