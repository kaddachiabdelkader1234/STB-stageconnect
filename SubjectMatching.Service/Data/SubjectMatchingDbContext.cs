using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SubjectMatching.Service.Models;

namespace SubjectMatching.Service.Data;

public class SubjectMatchingDbContext : DbContext
{
    public SubjectMatchingDbContext(DbContextOptions<SubjectMatchingDbContext> options)
        : base(options)
    {
    }

    public DbSet<InternshipSubject> Subjects => Set<InternshipSubject>();
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<SubjectMatch> SubjectMatches => Set<SubjectMatch>();
    public DbSet<SubjectAssignment> SubjectAssignments => Set<SubjectAssignment>();
    public DbSet<SubjectChangeRequest> SubjectChangeRequests => Set<SubjectChangeRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var listStringConverter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
        );

        modelBuilder.Entity<InternshipSubject>(entity =>
        {
            entity.ToTable("internship_subjects");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.ProblemStatement).IsRequired().HasMaxLength(4000);
            entity.Property(e => e.Department).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TypeStage).IsRequired().HasMaxLength(50);

            entity.Property(e => e.RequiredSkills)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            entity.Property(e => e.PreferredSkills)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            entity.Property(e => e.Difficulty).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasIndex(e => e.Department);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<CandidateProfile>(entity =>
        {
            entity.ToTable("candidate_profiles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Skills)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            entity.Property(e => e.Projects)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            entity.Property(e => e.Languages)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            entity.HasIndex(e => e.StagiaireId);
            entity.HasIndex(e => e.UtilisateurId);
        });

        modelBuilder.Entity<SubjectMatch>(entity =>
        {
            entity.ToTable("subject_matches");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MatchedSkills)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            entity.Property(e => e.MissingSkills)
                .HasConversion(listStringConverter)
                .HasColumnType("jsonb");

            entity.HasIndex(e => e.StagiaireId);
            entity.HasIndex(e => e.SubjectId);
        });

        modelBuilder.Entity<SubjectAssignment>(entity =>
        {
            entity.ToTable("subject_assignments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasIndex(e => e.StagiaireId);
            entity.HasIndex(e => e.UtilisateurId);
            entity.HasIndex(e => e.SubjectId);
        });

        modelBuilder.Entity<SubjectChangeRequest>(entity =>
        {
            entity.ToTable("subject_change_requests");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasIndex(e => e.AssignmentId);
            entity.HasIndex(e => e.StagiaireId);
            entity.HasIndex(e => e.UtilisateurId);
            entity.HasIndex(e => e.Status);
        });
    }
}
