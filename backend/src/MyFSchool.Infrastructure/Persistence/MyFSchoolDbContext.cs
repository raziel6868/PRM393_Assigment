using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Domain.Identity;
using MyFSchool.Domain.School;
using MyFSchool.Infrastructure.Identity;

namespace MyFSchool.Infrastructure.Persistence;

public sealed class MyFSchoolDbContext(DbContextOptions<MyFSchoolDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<PasswordHelpRequest> PasswordHelpRequests => Set<PasswordHelpRequest>();

    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();

    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();

    public DbSet<ParentProfile> ParentProfiles => Set<ParentProfile>();

    public DbSet<ParentStudentLink> ParentStudentLinks => Set<ParentStudentLink>();

    public DbSet<SchoolYear> SchoolYears => Set<SchoolYear>();

    public DbSet<ClassRoom> ClassRooms => Set<ClassRoom>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<TeacherClassSubjectAssignment> TeacherClassSubjectAssignments => Set<TeacherClassSubjectAssignment>();

    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public DbSet<Club> Clubs => Set<Club>();

    public DbSet<ClubMembership> ClubMemberships => Set<ClubMembership>();

    public DbSet<Assessment> Assessments => Set<Assessment>();

    public DbSet<GradeEntry> GradeEntries => Set<GradeEntry>();

    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();

    public DbSet<SchoolEvent> SchoolEvents => Set<SchoolEvent>();

    public DbSet<FeedPost> FeedPosts => Set<FeedPost>();

    public DbSet<AnnouncementDelivery> AnnouncementDeliveries => Set<AnnouncementDelivery>();

    public DbSet<AnnouncementReadState> AnnouncementReadStates => Set<AnnouncementReadState>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users", table => table.HasCheckConstraint(
                "CK_Users_TemporaryPasswordState",
                "([MustChangePassword] = 1 AND [TemporaryPasswordExpiresAtUtc] IS NOT NULL) OR " +
                "([MustChangePassword] = 0 AND [TemporaryPasswordExpiresAtUtc] IS NULL)"));
            entity.Property(user => user.DisplayName).HasMaxLength(200);
            entity.Property(user => user.CreatedAtUtc).HasPrecision(0);
            entity.Property(user => user.UpdatedAtUtc).HasPrecision(0);
            entity.Property(user => user.TemporaryPasswordExpiresAtUtc).HasPrecision(0);
            entity.HasIndex(user => user.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique()
                .HasFilter("[NormalizedEmail] IS NOT NULL");
        });
        builder.Entity<AppRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("UserTokens");

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasMaxLength(88).IsRequired();
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(88);
            entity.Property(token => token.RevocationReason).HasMaxLength(100);
            entity.Property(token => token.CreatedAtUtc).HasPrecision(0);
            entity.Property(token => token.ExpiresAtUtc).HasPrecision(0);
            entity.Property(token => token.RevokedAtUtc).HasPrecision(0);
            entity.Property(token => token.RowVersion).IsRowVersion();
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => new { token.UserId, token.FamilyId });
            entity.HasOne<AppUser>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PasswordHelpRequest>(entity =>
        {
            entity.ToTable("PasswordHelpRequests");
            entity.HasKey(request => request.Id);
            entity.Property(request => request.RequestedAtUtc).HasPrecision(0);
            entity.Property(request => request.ResolvedAtUtc).HasPrecision(0);
            entity.Property(request => request.RowVersion).IsRowVersion();
            entity.HasIndex(request => request.UserId).IsUnique().HasFilter("[Status] = 0");
            entity.HasOne<AppUser>().WithMany().HasForeignKey(request => request.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<AppUser>().WithMany().HasForeignKey(request => request.ResolvedByUserId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<SecurityAuditEvent>(entity =>
        {
            entity.ToTable("SecurityAuditEvents");
            entity.HasKey(audit => audit.Id);
            entity.Property(audit => audit.EventType).HasMaxLength(100).IsRequired();
            entity.Property(audit => audit.CorrelationId).HasMaxLength(100).IsRequired();
            entity.Property(audit => audit.OccurredAtUtc).HasPrecision(0);
            entity.HasIndex(audit => new { audit.SubjectUserId, audit.OccurredAtUtc });
            entity.HasOne<AppUser>().WithMany().HasForeignKey(audit => audit.SubjectUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<AppUser>().WithMany().HasForeignKey(audit => audit.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        });

        ConfigureProfile<TeacherProfile>(builder, "TeacherProfiles", "EmployeeCode");
        ConfigureProfile<StudentProfile>(builder, "StudentProfiles", "StudentCode");
        ConfigureProfile<ParentProfile>(builder, "ParentProfiles", "ParentCode");

        builder.Entity<ParentStudentLink>(entity =>
        {
            entity.ToTable("ParentStudentLinks");
            entity.HasKey(link => link.Id);
            entity.Property(link => link.CreatedAtUtc).HasPrecision(0);
            entity.Property(link => link.RowVersion).IsRowVersion();
            entity.HasIndex(link => new { link.ParentProfileId, link.StudentProfileId }).IsUnique();
            entity.HasIndex(link => new { link.ParentProfileId, link.IsActive });
            entity.HasIndex(link => new { link.StudentProfileId, link.IsActive });
            entity.HasOne<ParentProfile>().WithMany().HasForeignKey(link => link.ParentProfileId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<StudentProfile>().WithMany().HasForeignKey(link => link.StudentProfileId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<SchoolYear>(entity =>
        {
            entity.ToTable("SchoolYears");
            entity.HasKey(year => year.Id);
            entity.Property(year => year.Code).HasMaxLength(20).IsRequired();
            entity.Property(year => year.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(year => year.CreatedAtUtc).HasPrecision(0);
            entity.Property(year => year.RowVersion).IsRowVersion();
            entity.HasIndex(year => year.Code).IsUnique();
            entity.ToTable(t => t.HasCheckConstraint("CK_SchoolYears_DateRange", "[EndDate] >= [StartDate]"));
        });

        builder.Entity<ClassRoom>(entity =>
        {
            entity.ToTable("ClassRooms");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Code).HasMaxLength(20).IsRequired();
            entity.Property(item => item.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.CreatedAtUtc).HasPrecision(0);
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasIndex(item => new { item.Code, item.SchoolYearId }).IsUnique();
            entity.HasIndex(item => item.SchoolYearId);
            entity.HasOne<SchoolYear>().WithMany().HasForeignKey(item => item.SchoolYearId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TeacherProfile>().WithMany().HasForeignKey(item => item.HomeroomTeacherProfileId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Subject>(entity =>
        {
            entity.ToTable("Subjects");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Code).HasMaxLength(20).IsRequired();
            entity.Property(item => item.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.CreatedAtUtc).HasPrecision(0);
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasIndex(item => item.Code).IsUnique();
        });

        builder.Entity<TeacherClassSubjectAssignment>(entity =>
        {
            entity.ToTable("TeacherClassSubjectAssignments");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.CreatedAtUtc).HasPrecision(0);
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasIndex(item => new { item.TeacherProfileId, item.ClassId, item.SubjectId, item.SchoolYearId }).IsUnique();
            entity.HasIndex(item => new { item.ClassId, item.SubjectId, item.SchoolYearId });
            entity.HasOne<TeacherProfile>().WithMany().HasForeignKey(item => item.TeacherProfileId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ClassRoom>().WithMany().HasForeignKey(item => item.ClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Subject>().WithMany().HasForeignKey(item => item.SubjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SchoolYear>().WithMany().HasForeignKey(item => item.SchoolYearId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StudentEnrollment>(entity =>
        {
            entity.ToTable("StudentEnrollments");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.CreatedAtUtc).HasPrecision(0);
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasIndex(item => new { item.StudentProfileId, item.ClassId, item.SchoolYearId }).IsUnique();
            entity.HasIndex(item => new { item.ClassId, item.SchoolYearId });
            entity.HasOne<StudentProfile>().WithMany().HasForeignKey(item => item.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ClassRoom>().WithMany().HasForeignKey(item => item.ClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SchoolYear>().WithMany().HasForeignKey(item => item.SchoolYearId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AttendanceRecord>(entity =>
        {
            entity.ToTable("AttendanceRecords");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.AttendanceDate).HasConversion(
                value => value.ToDateTime(TimeOnly.MinValue),
                value => DateOnly.FromDateTime(value));
            entity.Property(item => item.Note).HasMaxLength(500);
            entity.Property(item => item.RecordedAtUtc).HasPrecision(0);
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasIndex(item => new { item.StudentProfileId, item.ClassId, item.AttendanceDate, item.Session }).IsUnique();
            entity.HasIndex(item => new { item.ClassId, item.AttendanceDate, item.Session });
            entity.HasOne<StudentProfile>().WithMany().HasForeignKey(item => item.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ClassRoom>().WithMany().HasForeignKey(item => item.ClassId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("LeaveRequests");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.StartDate).HasConversion(
                value => value.ToDateTime(TimeOnly.MinValue),
                value => DateOnly.FromDateTime(value));
            entity.Property(item => item.EndDate).HasConversion(
                value => value.ToDateTime(TimeOnly.MinValue),
                value => DateOnly.FromDateTime(value));
            entity.Property(item => item.Reason).HasMaxLength(500).IsRequired();
            entity.Property(item => item.DecisionNote).HasMaxLength(500);
            entity.Property(item => item.CreatedAtUtc).HasPrecision(0);
            entity.Property(item => item.ReviewedAtUtc).HasPrecision(0);
            entity.Property(item => item.DecidedAtUtc).HasPrecision(0);
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasIndex(item => new { item.StudentProfileId, item.Status });
            entity.HasIndex(item => new { item.Status, item.CreatedAtUtc });
            entity.HasOne<StudentProfile>().WithMany().HasForeignKey(item => item.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Club>(entity =>
        {
            entity.ToTable("Clubs");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Code).HasMaxLength(20).IsRequired();
            entity.Property(item => item.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(2000);
            entity.Property(item => item.Category).HasMaxLength(50).IsRequired();
            entity.Property(item => item.CreatedAtUtc).HasPrecision(0);
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasIndex(item => item.Code).IsUnique();
            entity.HasIndex(item => item.Category);
        });

        builder.Entity<ClubMembership>(entity =>
        {
            entity.ToTable("ClubMemberships");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.JoinedAtUtc).HasPrecision(0);
            entity.Property(item => item.LeftAtUtc).HasPrecision(0);
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasIndex(item => new { item.ClubId, item.StudentProfileId });
            entity.HasIndex(item => new { item.ClubId, item.Status });
            entity.HasOne<Club>().WithMany().HasForeignKey(item => item.ClubId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<StudentProfile>().WithMany().HasForeignKey(item => item.StudentProfileId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Assessment>(entity =>
        {
            entity.ToTable("Assessments");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Code).HasMaxLength(50).IsRequired();
            entity.Property(item => item.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.AssessmentType).HasMaxLength(50).IsRequired();
            entity.Property(item => item.CreatedAtUtc).HasPrecision(0);
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasIndex(item => new { item.Code, item.ClassId, item.SchoolYearId }).IsUnique();
            entity.HasOne<SchoolYear>().WithMany().HasForeignKey(item => item.SchoolYearId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ClassRoom>().WithMany().HasForeignKey(item => item.ClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Subject>().WithMany().HasForeignKey(item => item.SubjectId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GradeEntry>(entity =>
        {
            entity.ToTable("GradeEntries");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.TeacherComment).HasMaxLength(2000);
            entity.Property(item => item.RecordedAtUtc).HasPrecision(0);
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasIndex(item => new { item.AssessmentId, item.StudentProfileId }).IsUnique();
            entity.HasOne<Assessment>().WithMany().HasForeignKey(item => item.AssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<StudentProfile>().WithMany().HasForeignKey(item => item.StudentProfileId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TimetableEntry>(entity =>
        {
            entity.ToTable("TimetableEntries");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Room).HasMaxLength(100);
            entity.Property(item => item.StartTime).HasConversion(
                value => value.ToTimeSpan(),
                value => TimeOnly.FromTimeSpan(value));
            entity.Property(item => item.EndTime).HasConversion(
                value => value.ToTimeSpan(),
                value => TimeOnly.FromTimeSpan(value));
            entity.Property(item => item.CreatedAtUtc).HasPrecision(0);
            entity.HasIndex(item => new { item.ClassId, item.DayOfWeek });
            entity.HasIndex(item => new { item.TeacherProfileId, item.DayOfWeek });
            entity.HasOne<ClassRoom>().WithMany().HasForeignKey(item => item.ClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Subject>().WithMany().HasForeignKey(item => item.SubjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TeacherProfile>().WithMany().HasForeignKey(item => item.TeacherProfileId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SchoolYear>().WithMany().HasForeignKey(item => item.SchoolYearId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SchoolEvent>(entity =>
        {
            entity.ToTable("SchoolEvents");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(5000);
            entity.Property(item => item.Location).HasMaxLength(500);
            entity.Property(item => item.OrganizerContact).HasMaxLength(200);
            entity.Property(item => item.Audience).HasMaxLength(50).IsRequired();
            entity.Property(item => item.CreatedAtUtc).HasPrecision(0);
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasIndex(item => item.EndAtUtc);
        });

        builder.Entity<FeedPost>(entity =>
        {
            entity.ToTable("FeedPosts");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Title).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Body).HasMaxLength(4000).IsRequired();
            entity.Property(item => item.AuthorDisplayName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.ImageUrl).HasMaxLength(500);
            entity.Property(item => item.CreatedAtUtc).HasPrecision(0);
            entity.Property(item => item.PublishedAtUtc).HasPrecision(0);
            entity.Property(item => item.RowVersion).IsRowVersion();
            entity.HasIndex(item => item.IsPublished);
            entity.HasIndex(item => item.CreatedAtUtc);
            entity.HasOne<ClassRoom>().WithMany().HasForeignKey(item => item.TargetClassId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AnnouncementDelivery>(entity =>
        {
            entity.ToTable("AnnouncementDeliveries");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.RecipientDisplayName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.SentAtUtc).HasPrecision(0);
            entity.Property(item => item.FailureReason).HasMaxLength(500);
            entity.HasIndex(item => new { item.FeedPostId, item.RecipientUserId, item.Channel }).IsUnique();
            entity.HasOne<FeedPost>().WithMany(p => p.Deliveries).HasForeignKey(item => item.FeedPostId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AnnouncementReadState>(entity =>
        {
            entity.ToTable("AnnouncementReadStates");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ReadAtUtc).HasPrecision(0);
            entity.HasIndex(item => new { item.FeedPostId, item.UserId }).IsUnique();
            entity.HasOne<FeedPost>().WithMany(p => p.ReadStates).HasForeignKey(item => item.FeedPostId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureProfile<TProfile>(ModelBuilder builder, string tableName, string codeProperty)
        where TProfile : class
    {
        var entity = builder.Entity<TProfile>();
        entity.ToTable(tableName);
        entity.HasKey("Id");
        entity.Property<string>(codeProperty).HasMaxLength(50).IsRequired();
        entity.HasIndex(codeProperty).IsUnique();
        entity.HasIndex("UserId").IsUnique();
        entity.HasOne<AppUser>().WithMany().HasForeignKey("UserId").OnDelete(DeleteBehavior.Cascade);
    }
}
