using Madar.Application.Cases;
using Madar.Contracts.Cases;
using Madar.Domain.Cases;
using Madar.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Madar.Infrastructure.Cases;

public sealed class CaseCommentStore(MadarDbContext dbContext)
    : ICaseCommentStore, ICaseCommentQueryService
{
    public async Task AddAsync(
        CaseComment comment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comment);
        await dbContext.CaseComments.AddAsync(comment, cancellationToken);
    }

    public Task<CaseCommentDto?> GetByIdAsync(
        Guid commentId,
        CancellationToken cancellationToken = default) =>
        ProjectComments()
            .SingleOrDefaultAsync(item => item.Id == commentId, cancellationToken);

    public async Task<IReadOnlyList<CaseCommentDto>> ListForCaseAsync(
        Guid caseId,
        CancellationToken cancellationToken = default) =>
        await ProjectComments()
            .Where(item => item.CaseId == caseId)
            .OrderBy(item => item.CreatedUtc)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

    private IQueryable<CaseCommentDto> ProjectComments() =>
        from comment in dbContext.CaseComments.AsNoTracking()
        join user in dbContext.Set<MadarUser>().AsNoTracking()
            on comment.AuthorUserId equals user.Id
        select new CaseCommentDto(
            comment.Id,
            comment.CaseId,
            comment.AuthorUserId,
            user.DisplayName,
            comment.Body,
            comment.CreatedUtc);
}
