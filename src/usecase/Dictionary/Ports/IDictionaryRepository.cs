using Domain.Entities;

namespace Application.Dictionary.Ports;

/// <summary>
/// Port cho thao tác truy vấn dữ liệu từ điển từ database (Infrastructure).
/// </summary>
public interface IDictionaryRepository
{
    /// <summary>
    /// Tìm kiếm từ điển theo dạng viết (Form) hoặc dạng đọc (ReadingText).
    /// </summary>
    Task<IReadOnlyList<DictionaryEntry>> SearchByFormOrReadingAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách từ điển theo danh sách Canonical IDs toàn cục (dạng "dictionary:xxxxx").
    /// </summary>
    Task<IReadOnlyList<DictionaryEntry>> GetByCanonicalIdsAsync(IEnumerable<string> canonicalIds, CancellationToken cancellationToken = default);
}
