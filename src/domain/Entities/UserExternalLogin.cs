// EARS[Entity]: External Login — OAuth providers (AUTH-003)
namespace Domain.Entities;

/// <summary>
/// Liên kết OAuth giữa User Account và provider bên ngoài (ví dụ: Google).
/// Một user có thể có nhiều external login (AUTH-003).
/// </summary>
public class UserExternalLogin
{
    public long Id { get; set; }
    public long UserId { get; set; }

    /// <summary>Tên provider: "google".</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>ID duy nhất của user trên provider (Google sub ID).</summary>
    public string ProviderUserId { get; set; } = string.Empty;

    /// <summary>Email từ provider profile (tham khảo).</summary>
    public string? ProviderEmail { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
