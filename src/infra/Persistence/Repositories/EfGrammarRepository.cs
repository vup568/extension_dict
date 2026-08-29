namespace Infrastructure.Persistence.Repositories;

using System.Text.Json;
using Application.Grammar.Ports;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class EfGrammarRepository : IGrammarRepository
{
    private readonly AppDbContext _dbContext;

    public EfGrammarRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GrammarRule>> GetAllActiveRulesAsync(CancellationToken ct = default)
    {
        var rules = await _dbContext.GrammarRules
            .AsNoTracking()
            .ToListAsync(ct);

        if (rules.Count > 0)
        {
            return rules;
        }

        // Tự động Seed dữ liệu mẫu nếu bảng grammar_rules trong PostgreSQL đang trống
        await SeedInitialRulesAsync(ct);

        return await _dbContext.GrammarRules
            .AsNoTracking()
            .ToListAsync(ct);
    }

    private async Task SeedInitialRulesAsync(CancellationToken ct)
    {
        try
        {
            var seedFilePath = Path.Combine(AppContext.BaseDirectory, "Persistence", "SeedData", "grammar_rules_seed.json");
            if (!File.Exists(seedFilePath))
            {
                // Fallback tìm file trong thư mục làm việc nếu không tìm thấy trong BaseDirectory
                seedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "src", "infra", "Persistence", "SeedData", "grammar_rules_seed.json");
            }

            List<SeedGrammarRuleDto>? seedData = null;
            if (File.Exists(seedFilePath))
            {
                var json = await File.ReadAllTextAsync(seedFilePath, ct);
                seedData = JsonSerializer.Deserialize<List<SeedGrammarRuleDto>>(json);
            }

            var now = DateTime.UtcNow;

            if (seedData != null && seedData.Count > 0)
            {
                foreach (var seed in seedData)
                {
                    _dbContext.GrammarRules.Add(new GrammarRule
                    {
                        CanonicalId = seed.canonical_id ?? string.Empty,
                        Pattern = seed.pattern ?? string.Empty,
                        JlptLevel = seed.jlpt_level,
                        MeaningVi = seed.meaning_vi,
                        MeaningEn = seed.meaning_en,
                        Formation = seed.formation,
                        MatcherMetadata = seed.matcher_metadata,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }
            else
            {
                // Fallback dữ liệu mẫu cứng nếu không đọc được file
                _dbContext.GrammarRules.AddRange(
                    new GrammarRule
                    {
                        CanonicalId = "grammar:n5:te-iru",
                        Pattern = "〜ている",
                        JlptLevel = "N5",
                        MeaningVi = "Diễn tả hành động đang diễn ra hoặc trạng thái kết quả kéo dài.",
                        MeaningEn = "Expresses an action in progress or a continuous state.",
                        Formation = "V-て + いる",
                        MatcherMetadata = "[{\"surface\":\"て|で\"},{\"base\":\"いる\"}]",
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new GrammarRule
                    {
                        CanonicalId = "grammar:n4:te-wa-ikenai",
                        Pattern = "〜てはいけない",
                        JlptLevel = "N4",
                        MeaningVi = "Cấm đoán, không được phép làm gì.",
                        MeaningEn = "Must not do (prohibition).",
                        Formation = "V-て + は + いけない",
                        MatcherMetadata = "[{\"surface\":\"て|で\"},{\"surface\":\"は\"},{\"base\":\"いける\"},{\"base\":\"ない\"}]",
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new GrammarRule
                    {
                        CanonicalId = "grammar:n5:nai-de",
                        Pattern = "〜ないで",
                        JlptLevel = "N5",
                        MeaningVi = "Xin đừng làm gì / Làm việc này mà không làm việc kia.",
                        MeaningEn = "Please don't do / Without doing.",
                        Formation = "V-ない + で",
                        MatcherMetadata = "[{\"base\":\"ない\"},{\"surface\":\"で\"}]",
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new GrammarRule
                    {
                        CanonicalId = "grammar:n5:te-kara",
                        Pattern = "〜てから",
                        JlptLevel = "N5",
                        MeaningVi = "Sau khi làm gì thì làm tiếp việc khác.",
                        MeaningEn = "After doing something.",
                        Formation = "V-て + から",
                        MatcherMetadata = "[{\"surface\":\"て|で\"},{\"surface\":\"から\"}]",
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    new GrammarRule
                    {
                        CanonicalId = "grammar:n5:hou-ga-ii",
                        Pattern = "〜ほうがいい",
                        JlptLevel = "N5",
                        MeaningVi = "Khuyên nên hoặc không nên làm gì.",
                        MeaningEn = "Had better do / It is better to...",
                        Formation = "V-た / V-ない + ほう + が + いい",
                        MatcherMetadata = "[{\"base\":\"ほう\"},{\"surface\":\"が\"},{\"base\":\"いい\"}]",
                        CreatedAt = now,
                        UpdatedAt = now
                    }
                );
            }

            await _dbContext.SaveChangesAsync(ct);
        }
        catch
        {
            // Bỏ qua lỗi nếu seed bị trùng lặp hoặc lỗi không mong muốn
        }
    }

    private class SeedGrammarRuleDto
    {
        public string? canonical_id { get; set; }
        public string? pattern { get; set; }
        public string? jlpt_level { get; set; }
        public string? meaning_vi { get; set; }
        public string? meaning_en { get; set; }
        public string? formation { get; set; }
        public string? matcher_metadata { get; set; }
    }
}
