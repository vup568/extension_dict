namespace Infrastructure.Persistence.Repositories;

using Application.Grammar.Ports;
using Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

public class CachedGrammarRepository : IGrammarRepository
{
    private readonly IGrammarRepository _innerRepository;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "ActiveGrammarRules_All";

    public CachedGrammarRepository(IGrammarRepository innerRepository, IMemoryCache cache)
    {
        _innerRepository = innerRepository;
        _cache = cache;
    }

    public async Task<IReadOnlyList<GrammarRule>> GetAllActiveRulesAsync(CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(24);
            return await _innerRepository.GetAllActiveRulesAsync(ct);
        }) ?? Array.Empty<GrammarRule>();
    }
}
