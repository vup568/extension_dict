using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace UnitTests;

public sealed class DictionaryEntryTests
{
    [Fact]
    public void New_entry_has_empty_navigation_collections()
    {
        var entry = new DictionaryEntry();

        entry.WrittenForms.Should().BeEmpty();
        entry.Readings.Should().BeEmpty();
        entry.Senses.Should().BeEmpty();
    }
}
