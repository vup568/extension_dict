using Application.Dictionary.Models;
using Application.Dictionary.Ports;

namespace Infrastructure.Adapters;

/// <summary>
/// Fallback Tokenizer Adapter mặc định trong môi trường Development/Testing.
/// Trả về DeinflectionResult.Empty để ứng dụng chạy độc lập mà không bắt buộc có Tokenizer Sidecar container.
/// </summary>
public sealed class NullTokenizerAdapter : ITokenizerAdapter
{
    public Task<DeinflectionResult> DeinflectAsync(string surfaceText, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DeinflectionResult.Empty(surfaceText));
    }
}
