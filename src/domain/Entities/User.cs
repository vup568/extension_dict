// EARS[Entity]: User Account (AUTH-001..004, SEC-005)
namespace Domain.Entities;

/// <summary>
/// Principal xác thực đại diện cho một learner trên Extension và Web.
/// Hỗ trợ hybrid auth: email/password + Google OAuth (AUTH-003).
/// Password hash bằng BCrypt hoặc Argon2id, không bao giờ plaintext (SEC-005).
/// </summary>
public class User
{
    public long Id { get; set; }

    /// <summary>Email duy nhất cho mỗi user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Tên hiển thị (tùy chọn).</summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// BCrypt/Argon2id hash. NULL khi user chỉ đăng ký qua OAuth.
    /// Tuyệt đối KHÔNG lưu plaintext (AGENTS.md §4.1, SEC-005).
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>Thời điểm xác nhận email. NULL nếu chưa xác nhận.</summary>
    public DateTime? EmailVerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<UserExternalLogin> ExternalLogins { get; set; } = [];
    public ICollection<LearningReference> LearningReferences { get; set; } = [];
}
