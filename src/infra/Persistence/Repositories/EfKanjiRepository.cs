using Application.Kanji.Ports;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core Repository truy vấn dữ liệu KanjiRecord từ PostgreSQL.
/// Sử dụng AsNoTracking() cho đường đọc (read-only query).
/// </summary>
public sealed class EfKanjiRepository(AppDbContext dbContext) : IKanjiRepository
{
    public async Task<IReadOnlyList<KanjiRecord>> GetByCharactersAsync(
        IEnumerable<string> characters,
        CancellationToken cancellationToken = default)
    {
        var charList = characters.Distinct().ToList();
        if (charList.Count == 0)
        {
            return [];
        }

        return await dbContext.KanjiRecords
            .AsNoTracking()
            .Where(k => charList.Contains(k.Character))
            .ToListAsync(cancellationToken);
    }
}
