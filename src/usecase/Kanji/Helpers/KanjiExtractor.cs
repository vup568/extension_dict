using System.Text;

namespace Application.Kanji.Helpers;

/// <summary>
/// Helper bóc tách các ký tự Kanji (CJK Unified Ideographs) từ chuỗi tiếng Nhật.
/// Hỗ trợ Unicode Surrogate Pairs (Rune), loại bỏ lặp lại và bảo toàn thứ tự xuất hiện đầu tiên.
/// </summary>
public static class KanjiExtractor
{
    public static IReadOnlyList<string> ExtractUniqueKanji(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rune in text.EnumerateRunes())
        {
            if (IsKanjiRune(rune))
            {
                var character = rune.ToString();
                if (seen.Add(character))
                {
                    result.Add(character);
                }
            }
        }

        return result;
    }

    private static bool IsKanjiRune(Rune rune)
    {
        int v = rune.Value;

        return (v >= 0x4E00 && v <= 0x9FFF) ||   // CJK Unified Ideographs
               (v >= 0x3400 && v <= 0x4DBF) ||   // Extension A
               (v >= 0x20000 && v <= 0x2A6DF) || // Extension B (e.g. 𠮟 U+20B9F)
               (v >= 0x2A700 && v <= 0x2B73F) || // Extension C
               (v >= 0x2B740 && v <= 0x2B81F) || // Extension D
               (v >= 0x2B820 && v <= 0x2CEAF) || // Extension E
               (v >= 0x2CEB0 && v <= 0x2EBEF) || // Extension F
               (v >= 0x30000 && v <= 0x3134F) || // Extension G
               (v >= 0xF900 && v <= 0xFAFF) ||   // CJK Compatibility Ideographs
               (v >= 0x2F800 && v <= 0x2FA1F);   // CJK Compatibility Supplement
    }
}
