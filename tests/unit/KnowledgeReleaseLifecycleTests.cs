using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace UnitTests;

public sealed class KnowledgeReleaseLifecycleTests
{
    [Fact]
    public void Publish_transitions_a_draft_release_once()
    {
        var release = new KnowledgeRelease();
        var publishedAt = new DateTime(2026, 8, 21, 8, 0, 0, DateTimeKind.Utc);

        Action publish = () => release.Publish(publishedAt);

        publish.Should().NotThrow();
        release.PublishedAt.Should().Be(publishedAt);
    }

    [Fact]
    public void Publish_rejects_an_already_published_release()
    {
        var release = new KnowledgeRelease();
        var publishedAt = new DateTime(2026, 8, 21, 8, 0, 0, DateTimeKind.Utc);
        release.Publish(publishedAt);

        Action republish = () => release.Publish(publishedAt.AddMinutes(1));

        republish.Should().Throw<InvalidOperationException>();
    }
}
