using Application.KnowledgeReleases;
using FluentAssertions;
using Xunit;

namespace UnitTests;

public sealed class PublishKnowledgeReleaseUseCaseTests
{
    [Fact]
    public async Task Execute_uses_the_authoritative_UTC_time_and_release_id()
    {
        var expectedPublishedAt = new DateTimeOffset(2026, 8, 21, 9, 30, 0, TimeSpan.Zero);
        var publication = new RecordingPublication();
        var useCase = new PublishKnowledgeReleaseUseCase(
            publication,
            new FixedTimeProvider(expectedPublishedAt));

        await useCase.ExecuteAsync(42, TestContext.Current.CancellationToken);

        publication.ReleaseId.Should().Be(42);
        publication.PublishedAt.Should().Be(expectedPublishedAt.UtcDateTime);
    }

    private sealed class RecordingPublication : IKnowledgeReleasePublication
    {
        public long? ReleaseId { get; private set; }
        public DateTime? PublishedAt { get; private set; }

        public Task PublishAsync(long releaseId, DateTime publishedAt, CancellationToken cancellationToken)
        {
            ReleaseId = releaseId;
            PublishedAt = publishedAt;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
