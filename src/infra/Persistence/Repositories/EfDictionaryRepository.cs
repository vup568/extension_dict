using Application.Dictionary.Ports;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core Repository truy vấn dữ liệu từ điển từ PostgreSQL.
/// Sử dụng AsNoTracking(), eager loading đầy đủ các navigation property để tránh N+1 query problem.
/// </summary>
public sealed class EfDictionaryRepository(AppDbContext dbContext) : IDictionaryRepository
{
    public async Task<IReadOnlyList<DictionaryEntry>> SearchByFormOrReadingAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        return await dbContext.DictionaryEntries
            .AsNoTracking()
            .Where(e => e.WrittenForms.Any(w => w.Form == query) ||
                        e.Readings.Any(r => r.ReadingText == query))
            .Include(e => e.WrittenForms)
            .Include(e => e.Readings)
            .Include(e => e.Senses)
                .ThenInclude(s => s.Glosses)
            .Include(e => e.Senses)
                .ThenInclude(s => s.Applicabilities)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DictionaryEntry>> GetByCanonicalIdsAsync(
        IEnumerable<string> canonicalIds,
        CancellationToken cancellationToken = default)
    {
        var idList = canonicalIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        return await dbContext.DictionaryEntries
            .AsNoTracking()
            .Where(e => idList.Contains(e.CanonicalId))
            .Include(e => e.WrittenForms)
            .Include(e => e.Readings)
            .Include(e => e.Senses)
                .ThenInclude(s => s.Glosses)
            .Include(e => e.Senses)
                .ThenInclude(s => s.Applicabilities)
            .ToListAsync(cancellationToken);
    }
}
