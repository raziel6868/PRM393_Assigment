using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Domain.School;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.School;

public sealed class ClubAdministrationService(
    MyFSchoolDbContext dbContext,
    TimeProvider timeProvider) : IClubAdministrationService
{
    private const int MaxPageSize = 100;

    public async Task<OperationResult<ClubResult>> CreateAsync(
        CreateClubCommand command,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow();
        var club = new Club
        {
            Id = Guid.NewGuid(),
            Code = command.Code.Trim(),
            DisplayName = command.DisplayName.Trim(),
            Description = command.Description?.Trim(),
            Category = command.Category.Trim(),
            MaxMembers = command.MaxMembers,
            IsActive = true,
            CreatedAtUtc = nowUtc
        };
        dbContext.Clubs.Add(club);
        await dbContext.SaveChangesAsync(cancellationToken);
        var count = await dbContext.ClubMemberships
            .CountAsync(m => m.ClubId == club.Id && m.Status == ClubMembershipStatus.Active, cancellationToken);
        return OperationResult<ClubResult>.Success(ToResult(club, count));
    }

    public async Task<OperationResult<ClubResult>> UpdateAsync(
        UpdateClubCommand command,
        CancellationToken cancellationToken)
    {
        var club = await dbContext.Clubs
            .SingleOrDefaultAsync(c => c.Id == command.ClubId, cancellationToken);
        if (club is null) return OperationResult<ClubResult>.Failure("clubNotFound");
        if (!RowVersionMatches(command.RowVersion, club.RowVersion))
            return OperationResult<ClubResult>.Failure("concurrencyConflict");

        club.DisplayName = command.DisplayName.Trim();
        club.Description = command.Description?.Trim();
        club.Category = command.Category.Trim();
        club.MaxMembers = command.MaxMembers;
        club.IsActive = command.IsActive;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<ClubResult>.Failure("concurrencyConflict");
        }

        var count = await dbContext.ClubMemberships
            .CountAsync(m => m.ClubId == club.Id && m.Status == ClubMembershipStatus.Active, cancellationToken);
        return OperationResult<ClubResult>.Success(ToResult(club, count));
    }

    public async Task<OperationResult<ClubPage>> ListPublicAsync(
        string? search,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var boundedPage = page < 1 ? 1 : page;
        var boundedPageSize = pageSize switch
        {
            < 1 => 20,
            > MaxPageSize => MaxPageSize,
            _ => pageSize
        };

        var query = dbContext.Clubs.AsNoTracking().Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(c => c.DisplayName.ToLower().Contains(searchLower)
                || c.Code.ToLower().Contains(searchLower));
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(c => c.Category == category);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)boundedPageSize);
        var clubs = await query
            .OrderBy(c => c.Category)
            .ThenBy(c => c.DisplayName)
            .Skip((boundedPage - 1) * boundedPageSize)
            .Take(boundedPageSize)
            .ToListAsync(cancellationToken);

        var clubIds = clubs.Select(c => c.Id).ToList();
        var memberCounts = await dbContext.ClubMemberships
            .Where(m => clubIds.Contains(m.ClubId) && m.Status == ClubMembershipStatus.Active)
            .GroupBy(m => m.ClubId)
            .Select(g => new { ClubId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var countMap = memberCounts.ToDictionary(x => x.ClubId, x => x.Count);

        var items = clubs.Select(c => ToResult(c, countMap.GetValueOrDefault(c.Id, 0))).ToList();
        return OperationResult<ClubPage>.Success(new ClubPage(items, boundedPage, boundedPageSize, totalCount, totalPages));
    }

    public async Task<OperationResult<ClubDetailResult>> GetDetailAsync(
        Guid clubId,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var club = await dbContext.Clubs.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clubId, cancellationToken);
        if (club is null) return OperationResult<ClubDetailResult>.Failure("clubNotFound");

        var memberCount = await dbContext.ClubMemberships
            .CountAsync(m => m.ClubId == clubId && m.Status == ClubMembershipStatus.Active, cancellationToken);

        string membershipStatus = "notJoined";
        DateTimeOffset? joinedAtUtc = null;
        if (actorUserId.HasValue)
        {
            var studentProfileId = await dbContext.StudentProfiles
                .Where(p => p.UserId == actorUserId.Value && p.IsActive)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (studentProfileId.HasValue)
            {
                var membership = await dbContext.ClubMemberships.AsNoTracking()
                    .Where(m => m.ClubId == clubId && m.StudentProfileId == studentProfileId.Value)
                    .OrderByDescending(m => m.JoinedAtUtc)
                    .FirstOrDefaultAsync(cancellationToken);
                if (membership is not null)
                {
                    membershipStatus = membership.Status.ToWire();
                    joinedAtUtc = membership.JoinedAtUtc;
                }
            }
        }

        return OperationResult<ClubDetailResult>.Success(new ClubDetailResult(
            club.Id, club.Code, club.DisplayName, club.Description, club.Category,
            club.MaxMembers, memberCount, club.IsActive,
            Convert.ToBase64String(club.RowVersion), membershipStatus, joinedAtUtc));
    }

    public async Task<OperationResult<ClubDetailResult>> JoinAsync(
        JoinClubCommand command,
        CancellationToken cancellationToken)
    {
        var club = await dbContext.Clubs
            .SingleOrDefaultAsync(c => c.Id == command.ClubId && c.IsActive, cancellationToken);
        if (club is null) return OperationResult<ClubDetailResult>.Failure("clubNotFound");

        var studentProfile = await dbContext.StudentProfiles
            .SingleOrDefaultAsync(p => p.UserId == command.ActorUserId && p.IsActive, cancellationToken);
        if (studentProfile is null) return OperationResult<ClubDetailResult>.Failure("studentProfileNotFound");

        var existing = await dbContext.ClubMemberships
            .Where(m => m.ClubId == command.ClubId && m.StudentProfileId == studentProfile.Id)
            .OrderByDescending(m => m.JoinedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == ClubMembershipStatus.Active)
                return OperationResult<ClubDetailResult>.Failure("alreadyMember");
            if (existing.Status == ClubMembershipStatus.Pending)
                return OperationResult<ClubDetailResult>.Failure("requestPending");
        }

        if (club.MaxMembers.HasValue)
        {
            var currentCount = await dbContext.ClubMemberships
                .CountAsync(m => m.ClubId == command.ClubId && m.Status == ClubMembershipStatus.Active, cancellationToken);
            if (currentCount >= club.MaxMembers.Value)
                return OperationResult<ClubDetailResult>.Failure("clubAtCapacity");
        }

        var nowUtc = timeProvider.GetUtcNow();
        var membership = new ClubMembership
        {
            Id = Guid.NewGuid(),
            ClubId = command.ClubId,
            StudentProfileId = studentProfile.Id,
            Status = ClubMembershipStatus.Active,
            JoinedAtUtc = nowUtc
        };
        dbContext.ClubMemberships.Add(membership);

        var memberCount = await dbContext.ClubMemberships
            .CountAsync(m => m.ClubId == club.Id && m.Status == ClubMembershipStatus.Active, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<ClubDetailResult>.Success(new ClubDetailResult(
            club.Id, club.Code, club.DisplayName, club.Description, club.Category,
            club.MaxMembers, memberCount, club.IsActive,
            Convert.ToBase64String(club.RowVersion), "active", nowUtc));
    }

    public async Task<OperationResult<ClubDetailResult>> LeaveAsync(
        LeaveClubCommand command,
        CancellationToken cancellationToken)
    {
        var club = await dbContext.Clubs
            .SingleOrDefaultAsync(c => c.Id == command.ClubId && c.IsActive, cancellationToken);
        if (club is null) return OperationResult<ClubDetailResult>.Failure("clubNotFound");

        var studentProfile = await dbContext.StudentProfiles
            .SingleOrDefaultAsync(p => p.UserId == command.ActorUserId && p.IsActive, cancellationToken);
        if (studentProfile is null) return OperationResult<ClubDetailResult>.Failure("studentProfileNotFound");

        var membership = await dbContext.ClubMemberships
            .Where(m => m.ClubId == command.ClubId && m.StudentProfileId == studentProfile.Id && m.Status == ClubMembershipStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
        if (membership is null) return OperationResult<ClubDetailResult>.Failure("notAMember");

        var nowUtc = timeProvider.GetUtcNow();
        membership.Status = ClubMembershipStatus.Left;
        membership.LeftAtUtc = nowUtc;

        await dbContext.SaveChangesAsync(cancellationToken);

        var memberCount = await dbContext.ClubMemberships
            .CountAsync(m => m.ClubId == club.Id && m.Status == ClubMembershipStatus.Active, cancellationToken);
        return OperationResult<ClubDetailResult>.Success(new ClubDetailResult(
            club.Id, club.Code, club.DisplayName, club.Description, club.Category,
            club.MaxMembers, memberCount, club.IsActive,
            Convert.ToBase64String(club.RowVersion), "left", membership.JoinedAtUtc));
    }

    private static ClubResult ToResult(Club club, int memberCount) => new(
        club.Id, club.Code, club.DisplayName, club.Description, club.Category,
        club.MaxMembers, memberCount, club.IsActive, Convert.ToBase64String(club.RowVersion));

    private static bool RowVersionMatches(byte[] supplied, byte[] persisted) =>
        supplied.Length == persisted.Length && CryptographicOperations.FixedTimeEquals(supplied, persisted);
}
