using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Domain.Identity;
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
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
            entity.HasIndex(link => new { link.ParentProfileId, link.StudentProfileId }).IsUnique();
            entity.HasIndex(link => new { link.StudentProfileId, link.IsActive });
            entity.HasOne<ParentProfile>().WithMany().HasForeignKey(link => link.ParentProfileId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<StudentProfile>().WithMany().HasForeignKey(link => link.StudentProfileId).OnDelete(DeleteBehavior.NoAction);
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
