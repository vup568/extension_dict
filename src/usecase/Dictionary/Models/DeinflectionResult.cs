namespace Application.Dictionary.Models;

/// <summary>
/// Kết quả giải mã biến đổi từ (Deinflection) từ Tokenizer Sidecar.
/// </summary>
public sealed record DeinflectionResult(
    bool IsSuccess,
    string SurfaceText,
    string BaseForm,
    string RuleExplanation)
{
    public static DeinflectionResult Empty(string surfaceText) =>
        new(false, surfaceText, string.Empty, string.Empty);
}
