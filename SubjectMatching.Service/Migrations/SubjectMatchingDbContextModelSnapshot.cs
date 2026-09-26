using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SubjectMatching.Service.Data;

#nullable disable

namespace SubjectMatching.Service.Migrations;

[DbContext(typeof(SubjectMatchingDbContext))]
partial class SubjectMatchingDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("SubjectMatching.Service.Models.CandidateProfile", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<string>("CvFileHash")
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<string>("Education")
                    .HasColumnType("text");

                b.Property<string>("Experience")
                    .HasColumnType("text");

                b.Property<string>("Languages")
                    .IsRequired()
                    .HasColumnType("jsonb");

                b.Property<DateTime>("ParsedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("Projects")
                    .IsRequired()
                    .HasColumnType("jsonb");

                b.Property<string>("Provider")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("character varying(50)");

                b.Property<string>("Skills")
                    .IsRequired()
                    .HasColumnType("jsonb");

                b.Property<Guid>("StagiaireId")
                    .HasColumnType("uuid");

                b.Property<long>("UtilisateurId")
                    .HasColumnType("bigint");

                b.HasKey("Id");

                b.HasIndex("StagiaireId");

                b.HasIndex("UtilisateurId");

                b.ToTable("candidate_profiles", (string)null);
            });

        modelBuilder.Entity("SubjectMatching.Service.Models.InternshipSubject", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<int>("AvailablePositions")
                    .HasColumnType("integer");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<long>("CreatedBy")
                    .HasColumnType("bigint");

                b.Property<string>("Department")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<string>("Description")
                    .IsRequired()
                    .HasMaxLength(4000)
                    .HasColumnType("character varying(4000)");

                b.Property<string>("Difficulty")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnType("character varying(20)");

                b.Property<string>("EducationRequirements")
                    .HasMaxLength(500)
                    .HasColumnType("character varying(500)");

                b.Property<DateOnly>("EndDate")
                    .HasColumnType("date");

                b.Property<string>("ExperienceRequirements")
                    .HasMaxLength(500)
                    .HasColumnType("character varying(500)");

                b.Property<int>("FilledPositions")
                    .HasColumnType("integer");

                b.Property<string>("PreferredSkills")
                    .IsRequired()
                    .HasColumnType("jsonb");

                b.Property<string>("ProblemStatement")
                    .IsRequired()
                    .HasMaxLength(4000)
                    .HasColumnType("character varying(4000)");

                b.Property<string>("RequiredSkills")
                    .IsRequired()
                    .HasColumnType("jsonb");

                b.Property<DateOnly>("StartDate")
                    .HasColumnType("date");

                b.Property<string>("Status")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnType("character varying(20)");

                b.Property<string>("Title")
                    .IsRequired()
                    .HasMaxLength(250)
                    .HasColumnType("character varying(250)");

                b.Property<string>("TypeStage")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("character varying(50)");

                b.Property<DateTime>("UpdatedAt")
                    .HasColumnType("timestamp with time zone");

                b.HasKey("Id");

                b.HasIndex("Department");

                b.HasIndex("Status");

                b.ToTable("internship_subjects", (string)null);
            });

        modelBuilder.Entity("SubjectMatching.Service.Models.SubjectAssignment", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<DateTime>("ProposedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<DateTime?>("RespondedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<decimal>("ScoreAtProposal")
                    .HasColumnType("numeric(5,2)");

                b.Property<Guid>("StagiaireId")
                    .HasColumnType("uuid");

                b.Property<string>("Status")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnType("character varying(20)");

                b.Property<Guid>("SubjectId")
                    .HasColumnType("uuid");

                b.Property<long>("UtilisateurId")
                    .HasColumnType("bigint");

                b.HasKey("Id");

                b.HasIndex("StagiaireId");

                b.HasIndex("SubjectId");

                b.HasIndex("UtilisateurId");

                b.ToTable("subject_assignments", (string)null);
            });

        modelBuilder.Entity("SubjectMatching.Service.Models.SubjectChangeRequest", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<string>("AdminComment")
                    .HasMaxLength(2000)
                    .HasColumnType("character varying(2000)");

                b.Property<Guid>("AssignmentId")
                    .HasColumnType("uuid");

                b.Property<Guid>("CurrentSubjectId")
                    .HasColumnType("uuid");

                b.Property<string>("Reason")
                    .IsRequired()
                    .HasMaxLength(2000)
                    .HasColumnType("character varying(2000)");

                b.Property<Guid>("RequestedSubjectId")
                    .HasColumnType("uuid");

                b.Property<DateTime>("RequestedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<DateTime?>("ReviewedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<Guid>("StagiaireId")
                    .HasColumnType("uuid");

                b.Property<string>("Status")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnType("character varying(20)");

                b.Property<long>("UtilisateurId")
                    .HasColumnType("bigint");

                b.HasKey("Id");

                b.HasIndex("AssignmentId");

                b.HasIndex("CurrentSubjectId");

                b.HasIndex("RequestedSubjectId");

                b.HasIndex("StagiaireId");

                b.HasIndex("Status");

                b.HasIndex("UtilisateurId");

                b.ToTable("subject_change_requests", (string)null);
            });

        modelBuilder.Entity("SubjectMatching.Service.Models.SubjectMatch", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<decimal>("CompatibilityScore")
                    .HasColumnType("numeric(5,2)");

                b.Property<string>("Explanation")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<DateTime>("GeneratedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("MatchedSkills")
                    .IsRequired()
                    .HasColumnType("jsonb");

                b.Property<string>("MissingSkills")
                    .IsRequired()
                    .HasColumnType("jsonb");

                b.Property<Guid>("StagiaireId")
                    .HasColumnType("uuid");

                b.Property<Guid>("SubjectId")
                    .HasColumnType("uuid");

                b.HasKey("Id");

                b.HasIndex("StagiaireId");

                b.HasIndex("SubjectId");

                b.ToTable("subject_matches", (string)null);
            });

        modelBuilder.Entity("SubjectMatching.Service.Models.SubjectAssignment", b =>
            {
                b.HasOne("SubjectMatching.Service.Models.InternshipSubject", "Subject")
                    .WithMany()
                    .HasForeignKey("SubjectId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Subject");
            });

        modelBuilder.Entity("SubjectMatching.Service.Models.SubjectChangeRequest", b =>
            {
                b.HasOne("SubjectMatching.Service.Models.SubjectAssignment", "Assignment")
                    .WithMany()
                    .HasForeignKey("AssignmentId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("SubjectMatching.Service.Models.InternshipSubject", "CurrentSubject")
                    .WithMany()
                    .HasForeignKey("CurrentSubjectId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.HasOne("SubjectMatching.Service.Models.InternshipSubject", "RequestedSubject")
                    .WithMany()
                    .HasForeignKey("RequestedSubjectId")
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                b.Navigation("Assignment");

                b.Navigation("CurrentSubject");

                b.Navigation("RequestedSubject");
            });

        modelBuilder.Entity("SubjectMatching.Service.Models.SubjectMatch", b =>
            {
                b.HasOne("SubjectMatching.Service.Models.InternshipSubject", "Subject")
                    .WithMany()
                    .HasForeignKey("SubjectId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Subject");
            });
#pragma warning restore 612, 618
    }
}
