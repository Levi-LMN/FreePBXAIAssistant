using FreePBXAIAssistant.Models;
using Microsoft.EntityFrameworkCore;

namespace FreePBXAIAssistant.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CallRecord> CallRecords { get; set; }
        public DbSet<KnowledgeItem> KnowledgeItems { get; set; }
        public DbSet<AISettings> AISettings { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure CallRecord
            modelBuilder.Entity<CallRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CallId).IsUnique();
                entity.HasIndex(e => e.StartTime);
                entity.HasIndex(e => e.Intent);
                entity.Property(e => e.CallerNumber).HasMaxLength(50);
                entity.Property(e => e.Intent).HasMaxLength(100);
                entity.Property(e => e.Outcome).HasMaxLength(100);
            });

            // Configure KnowledgeItem
            modelBuilder.Entity<KnowledgeItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.IsActive);
                entity.Property(e => e.Title).HasMaxLength(500);
                entity.Property(e => e.Category).HasMaxLength(100);
            });

            // Configure AISettings
            modelBuilder.Entity<AISettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Temperature).HasDefaultValue(0.7f);
                entity.Property(e => e.MaxTokens).HasDefaultValue(800);
                entity.Property(e => e.ConfidenceThreshold).HasDefaultValue(0.7f);
            });

            // Configure User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Username).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.Role).HasMaxLength(50);
            });
        }
    }
}