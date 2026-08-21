// EARS[Entity]: Dictionary Entry — canonical lexical entry (ID-001, ID-005)
namespace Domain.Entities;

/// <summary>
/// Mục từ canonical trong từ điển. Mỗi entry có canonical ID toàn cục
/// dạng "dictionary:xxxxx" và chứa nhiều Written Form, Reading, Dictionary Sense.
/// </summary>
public class DictionaryEntry
{
    public long Id { get; set; }

    /// <summary>
    /// Canonical identifier toàn cục, format "dictionary:xxxxx".
    /// Ổn định qua mọi resource revision (ID-001, ID-005, ID-006).
    /// </summary>
    public string CanonicalId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<WrittenForm> WrittenForms { get; set; } = [];
    public ICollection<Reading> Readings { get; set; } = [];
    public ICollection<DictionarySense> Senses { get; set; } = [];
}
