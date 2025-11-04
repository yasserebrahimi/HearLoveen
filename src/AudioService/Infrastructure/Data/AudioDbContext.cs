using HearLoveen.AudioService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HearLoveen.AudioService.Infrastructure.Data;

public class AudioDbContext : DbContext
{
    public AudioDbContext(DbContextOptions<AudioDbContext> options) : base(options)
    {
    }
    
    public DbSet<AudioRecording> AudioRecordings => Set<AudioRecording>();
    public DbSet<Child> Children => Set<Child>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<AudioRecording>(entity =>
        {
            entity.ToTable("audio_recordings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BlobStoragePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.AudioFormat).HasMaxLength(10);
            entity.Property(e => e.RecordingEnvironment).HasMaxLength(50);
            entity.HasIndex(e => new { e.ChildId, e.CreatedAt });
        });
        
        modelBuilder.Entity<Child>(entity =>
        {
            entity.ToTable("children");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.HasMany(e => e.Recordings).WithOne().HasForeignKey("ChildId");
        });
    }
}
