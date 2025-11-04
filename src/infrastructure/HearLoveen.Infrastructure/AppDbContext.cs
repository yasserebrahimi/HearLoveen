using HearLoveen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

using HearLoveen.Infrastructure;
namespace HearLoveen.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ICurrentUser _currentUser;
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser) : base(options) { _currentUser = currentUser; }
    public DbSet<User> Users => Set<User>();
    public DbSet<Child> Children => Set<Child>();
    public DbSet<AudioSubmission> AudioSubmissions => Set<AudioSubmission>();
    public DbSet<FeatureVector> FeatureVectors => Set<FeatureVector>();
    public DbSet<FeedbackReport> FeedbackReports => Set<FeedbackReport>();
    public DbSet<Consent> Consents => Set<Consent>();
    public DbSet<PhonemeRating> PhonemeRatings => Set<PhonemeRating>();
    public DbSet<PhonemePrerequisite> PhonemePrerequisites => Set<PhonemePrerequisite>();
    public DbSet<ChildCurriculum> ChildCurricula => Set<ChildCurriculum>();
    public DbSet<TherapistAssignment> TherapistAssignments => Set<TherapistAssignment>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChildCurriculum>().HasKey(x => x.Id);
        modelBuilder.Entity<ChildCurriculum>().HasIndex(x => x.ChildId).IsUnique();
        modelBuilder.Entity<TherapistAssignment>().HasKey(x => x.Id);
        modelBuilder.Entity<TherapistAssignment>().HasIndex(x => new { x.TherapistUserId, x.ChildId }).IsUnique();
        modelBuilder.Entity<User>().HasKey(x => x.Id);
        modelBuilder.Entity<Child>().HasKey(x => x.Id);
        modelBuilder.Entity<AudioSubmission>().HasKey(x => x.Id);
        modelBuilder.Entity<FeatureVector>().HasKey(x => x.Id);
        modelBuilder.Entity<FeedbackReport>().HasKey(x => x.Id);
        modelBuilder.Entity<Consent>().HasKey(x => x.Id);
        modelBuilder.Entity<AudioSubmission>().Property(x => x.BlobUrl).HasMaxLength(512);
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();

        // Configure relationships for efficient query filtering
        modelBuilder.Entity<FeatureVector>()
            .HasOne<AudioSubmission>()
            .WithMany()
            .HasForeignKey(fv => fv.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FeedbackReport>()
            .HasOne<AudioSubmission>()
            .WithMany()
            .HasForeignKey(fr => fr.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Global query filters: therapist scope (restrict by ChildId)
        // Only apply filters if user is a therapist (non-admin)
        bool IsTherapist() => _currentUser?.IsTherapist == true && !_currentUser.IsAdmin;
        var scope = _currentUser?.ChildScope?.ToHashSet() ?? new HashSet<Guid>();

        // Direct filtering on AudioSubmission by ChildId
        modelBuilder.Entity<AudioSubmission>().HasQueryFilter(e =>
            !IsTherapist() || scope.Contains(e.ChildId));

        // Filter FeatureVector via navigation to AudioSubmission
        // This uses SQL JOIN instead of N+1 queries
        modelBuilder.Entity<FeatureVector>().HasQueryFilter(e =>
            !IsTherapist() ||
            EF.Property<AudioSubmission>(e, "AudioSubmission") == null ||
            scope.Contains(EF.Property<AudioSubmission>(e, "AudioSubmission").ChildId));

        // Filter FeedbackReport via navigation to AudioSubmission
        // This uses SQL JOIN instead of subquery
        modelBuilder.Entity<FeedbackReport>().HasQueryFilter(e =>
            !IsTherapist() ||
            EF.Property<AudioSubmission>(e, "AudioSubmission") == null ||
            scope.Contains(EF.Property<AudioSubmission>(e, "AudioSubmission").ChildId));

        // Add index on ChildId for better filter performance
        modelBuilder.Entity<AudioSubmission>().HasIndex(x => x.ChildId);
        modelBuilder.Entity<FeedbackReport>().HasIndex(x => x.SubmissionId);
        modelBuilder.Entity<FeatureVector>().HasIndex(x => x.SubmissionId);
    }
}
