using Application.Dictionary.Models;

namespace Application.Dictionary.Ports;

/// <summary>
/// Port kết nối tới Tokenizer Sidecar (Go/Python) để phân tích morphology và deinflection.
/// Tuân thủ EXT-001 (Provider Isolation): cách ly hoàn toàn raw labels của sidecar khỏi Domain/Application.
/// </summary>
public interface ITokenizerAdapter
{
    /// <summary>
    /// Giải mã biến đổi từ (Deinflect) một quan sát từ vựng tiếng Nhật về dạng gốc (Base Form).
    /// </summary>
    Task<DeinflectionResult> DeinflectAsync(string surfaceText, CancellationToken cancellationToken = default);
}
