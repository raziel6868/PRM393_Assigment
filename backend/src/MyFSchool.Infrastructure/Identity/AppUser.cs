using Microsoft.AspNetCore.Identity;

namespace MyFSchool.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; }

    public DateTimeOffset? TemporaryPasswordExpiresAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
