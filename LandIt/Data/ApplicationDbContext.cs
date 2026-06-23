using LandIt.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace LandIt.Data
{

    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            
        }

        public DbSet<Recruiter> Recruiters { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<Resume> Resumes { get; set; }
        public DbSet<ATSresult> ATSresults { get; set; }
        public DbSet<QuestionRequest> QuestionRequests { get; set; }
        public DbSet<GeneratedQuestion> GeneratedQuestions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<RecruiterReview> RecruiterReviews { get; set; }
        public DbSet<RecruiterAvailability> RecruiterAvailabilities { get; set; }
        public DbSet<GeneratedResume> GeneratedResumes { get; set; }
        public DbSet<Testimonial> Testimonials { get; set; }

        public DbSet<ContactMessage> ContactMessages { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //storing the enums as strings in the database for better readability and maintainability

            builder.Entity<AppUser>()
                .Property(u => u.Region)
                .HasConversion<string>();

            builder.Entity<Booking>()
                .Property(b => b.Status)
                .HasConversion<string>();

            builder.Entity<Booking>()
                .Property(b => b.PaymentStatus)
                .HasConversion<string>();

            builder.Entity<Payment>()
                .Property(p => p.Status)
                .HasConversion<string>();

            builder.Entity<Recruiter>()
                .Property(r => r.Status)
                .HasConversion<string>();

            builder.Entity<Recruiter>()
                .Property(r => r.Region)
                .HasConversion<string>();


            builder.Entity<Testimonial>()
                .Property(t => t.Source)
                .HasConversion<string>();


            // Prevent cascade delete cycles
            // (AppUser → Booking → Recruiter would cause multiple cascade paths)
            builder.Entity<Booking>()
                .HasOne(b => b.Recruiter)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RecruiterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Recruiter>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<RecruiterReview>()
                .HasOne(r => r.Recruiter)
                .WithMany(r => r.Reviews)
                .HasForeignKey(r => r.RecruiterId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-one: TimeSlot → Booking
            builder.Entity<TimeSlot>()
                .HasOne(t => t.Booking)
                .WithOne(b => b.TimeSlot)
                .HasForeignKey<Booking>(b => b.TimeSlotId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-one: Booking → Payment
            builder.Entity<Booking>()
                .HasOne(b => b.Payment)
                .WithOne(p => p.Booking)
                .HasForeignKey<Payment>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Testimonial>()
                .HasOne(t => t.User)
                .WithMany(u => u.Testimonials)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-one: Booking ↔ RecruiterReview
            builder.Entity<Booking>()
                .HasOne(b => b.RecruiterReview)
                .WithOne(r => r.Booking)
                .HasForeignKey<RecruiterReview>(r => r.BookingId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    

    }
