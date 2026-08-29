namespace Application.Grammar.Ports;

using Domain.Entities;

/// <summary>
/// Port đại diện cho Repository truy vấn quy tắc ngữ pháp (PostgreSQL / In-Memory RAM Cache).
/// </summary>
public interface IGrammarRepository
{
    Task<IReadOnlyList<GrammarRule>> GetAllActiveRulesAsync(CancellationToken ct = default);
}
