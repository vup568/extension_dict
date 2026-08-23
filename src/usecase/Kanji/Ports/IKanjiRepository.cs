using Domain.Entities;

namespace Application.Kanji.Ports;

/// <summary>
/// Domain Repository Interface cho việc truy vấn Kanji Record từ bộ lưu trữ.
/// </summary>
public interface IKanjiRepository
{
    /// <summary>
    /// Truy vấn danh sách KanjiRecord theo danh sách ký tự kanji.
    /// </summary>
    Task<IReadOnlyList<KanjiRecord>> GetByCharactersAsync(
        IEnumerable<string> characters,
        CancellationToken cancellationToken = default);
}
