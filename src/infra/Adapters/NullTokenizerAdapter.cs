using Application.Dictionary.Models;
using Application.Dictionary.Ports;

namespace Infrastructure.Adapters;

/// <summary>
/// Fallback Tokenizer Adapter mặc định trong môi trường Development/Testing.
/// Báo capability unavailable để ứng dụng chạy độc lập mà không giả định Tokenizer Sidecar đã xử lý yêu cầu.
/// </summary>
public sealed class NullTokenizerAdapter : ITokenizerAdapter
{
    public bool IsAvailable => false;

    public Task<DeinflectionResult> DeinflectAsync(string surfaceText, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DeinflectionResult.Unavailable(surfaceText));
    }
}
