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
        // Global query filters: therapist scope (restrict by ChildId)
        bool IsTherapist() => _currentUser?.IsTherapist == true && !_currentUser.IsAdmin;
        var scope = _currentUser?.ChildScope?.ToList() ?? new List<Guid>();

        modelBuilder.Entity<AudioSubmission>().HasQueryFilter(e => !IsTherapist() || scope.Contains(e.ChildId));
        modelBuilder.Entity<FeatureVector>().HasQueryFilter(e => !IsTherapist() || scope.Contains(e.SubmissionId == Guid.Empty ? Guid.Empty : e.SubmissionId));
        modelBuilder.Entity<FeedbackReport>().HasQueryFilter(e => !IsTherapist() || scope.Contains(
            (from s in Set<AudioSubmission>() where s.Id == e.SubmissionId select s.ChildId).FirstOrDefault()
        ));

    }
}
