namespace Application.Analysis.Exceptions;

/// <summary>
/// Exception thrown khi tất cả các capability phân tích (Vocabulary, Kanji, Grammar)
/// đều thất bại — không có bất kỳ kết quả partial nào khả dụng.
/// EARS[Event]: WHEN all analysis capabilities fail, THEN return HTTP 503 SERVICE_UNAVAILABLE.
/// </summary>
public sealed class AllCapabilitiesFailedException : Exception
{
    public AllCapabilitiesFailedException()
        : base("Tất cả các dịch vụ phân tích đều không khả dụng.") { }
}
