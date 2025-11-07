# Database Migrations

This directory contains Entity Framework Core migrations for the HearLoveen database.

## Applying Migrations

### Development
```bash
# From the solution root
dotnet ef database update --project src/infrastructure/HearLoveen.Infrastructure --startup-project src/api/HearLoveen.Api

# Or with connection string
dotnet ef database update --project src/infrastructure/HearLoveen.Infrastructure --startup-project src/api/HearLoveen.Api --connection "Host=localhost;Database=hearloveen;Username=postgres;Password=postgres"
```

### Production
Migrations are applied automatically via Kubernetes init containers or manually:

```bash
# Manual production deployment
dotnet ef database update --project src/infrastructure/HearLoveen.Infrastructure --startup-project src/api/HearLoveen.Api --connection "$PRODUCTION_CONNECTION_STRING"
```

## Creating New Migrations

```bash
# Add a new migration
dotnet ef migrations add MigrationName --project src/infrastructure/HearLoveen.Infrastructure --startup-project src/api/HearLoveen.Api

# Remove last migration (if not applied)
dotnet ef migrations remove --project src/infrastructure/HearLoveen.Infrastructure --startup-project src/api/HearLoveen.Api
```

## Existing Migrations

- **20250104000000_InitialCreate**: Initial database schema with all core tables
  - Users (Parent, Therapist, Admin)
  - Children and AudioSubmissions
  - FeedbackReports and FeatureVectors
  - ChildCurricula and PhonemeRatings
  - TherapistAssignments and Consents

## Database Schema

### Core Tables
- `Users`: User accounts (parents, therapists, admins)
- `Children`: Child profiles linked to parents
- `AudioSubmissions`: Uploaded audio recordings
- `FeedbackReports`: AI-generated feedback for submissions
- `FeatureVectors`: ML feature data (JSONB)

### Curriculum Tables
- `PhonemeRatings`: ELO-rated phoneme difficulty
- `PhonemePrerequisites`: Phoneme dependency graph
- `ChildCurricula`: Per-child learning progress

### Authorization Tables
- `TherapistAssignments`: Therapist-to-child assignments
- `Consents`: GDPR parental consents

## Notes

- All timestamps use `timestamp with time zone` (UTC)
- JSONB columns for flexible data structures
- Row-level security implemented via EF Core query filters
- Unique constraints on emails, assignments, and curricula
