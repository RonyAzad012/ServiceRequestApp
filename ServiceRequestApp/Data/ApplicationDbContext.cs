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
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Notification> Notifications { get; set; }

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

            // Category relationships
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.PrimaryCategory)
                .WithMany(c => c.Providers)
                .HasForeignKey(u => u.PrimaryCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Payment transaction relationships
            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(pt => pt.ServiceRequest)
                .WithMany()
                .HasForeignKey(pt => pt.ServiceRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(pt => pt.User)
                .WithMany()
                .HasForeignKey(pt => pt.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review relationships
            modelBuilder.Entity<Review>()
                .HasOne(r => r.ServiceRequest)
                .WithMany()
                .HasForeignKey(r => r.ServiceRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reviewee)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.RevieweeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Invoice relationships
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.ServiceRequest)
                .WithMany()
                .HasForeignKey(i => i.ServiceRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Requester)
                .WithMany()
                .HasForeignKey(i => i.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Provider)
                .WithMany()
                .HasForeignKey(i => i.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification relationships
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}




