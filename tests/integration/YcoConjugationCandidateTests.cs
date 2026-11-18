using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Application.Conjugation.Ports;
using Application.Dictionary;
using Application.Dictionary.Ports;
using Domain.Entities;
using FluentAssertions;
using Grpc.Core;
using Infrastructure.Adapters;
using Infrastructure.Adapters.Grpc;
using Infrastructure.Conjugation;
using NSubstitute;
using Xunit;

namespace IntegrationTests;

public sealed class YcoConjugationCandidateTests
{
    private const string Candidate = "candidate-20261110.1";
    private static readonly string CorpusRoot = Path.Combine(ConjugationCorpusFiles.Root,
        ".sdd/specs/005-yomitan-reading-experience/corpus");
    private static readonly Lazy<Task<string>> CandidateContent = new(CompileCandidateAsync);

    [Theory]
    [InlineData("AUX-01-NONPAST")]
    [InlineData("AUX-01-PAST")]
    [InlineData("AUX-02-NONPAST")]
    [InlineData("AUX-02-PAST")]
    [InlineData("AUX-02-WRONG-ONBIN-NONPAST")]
    [InlineData("AUX-02-WRONG-ONBIN-PAST")]
    [InlineData("AUX-02-RESTRICTED-SENSE")]
    [InlineData("AUX-03-EXACT-INTERJECTION")]
    public async Task CompiledCandidateMatchesFrozenWordExpectations(string caseId)
    {
        using var corpus = ReadCorpus();
        var item = ReadCase(corpus, caseId);
        var input = item.GetProperty("input");
        var text = Field(input, "text");
        var fixtures = InputFixtures(corpus, input);
        var entries = FixturesToEntries(fixtures.Values);
        var (lookup, provider, dataset, words) = Setup(Candidate, await CandidateContent.Value, entries);

        var result = await lookup.ExecuteAsync(text, TestContext.Current.CancellationToken);

        var expected = item.GetProperty("expected").GetProperty("occurrences");
        if (expected.GetArrayLength() == 0)
        {
            result.Matches.Should().BeEmpty();
            result.Alternatives.Should().BeNullOrEmpty();
            result.Capabilities.Conjugation.Should().Be("unavailable");
            provider.CallCount.Should().Be(1);
            if (Field(input, "fixture_policy") == "restricted_entry")
            {
                // The decoded base exists. Its exact written/reading pair cannot borrow a POS from the other pair.
                await words.Received(1).SearchByFormOrReadingAsync("読む", Arg.Any<CancellationToken>());
            }
            else
            {
                words.ReceivedCalls().Should().ContainSingle("wrong onbin should fail before any base-form search");
            }
            return;
        }

        var occurrence = expected.EnumerateArray().Single();
        var match = occurrence.GetProperty("matches").EnumerateArray().Single();
        var fixture = fixtures[Field(match, "fixture_id")];
        var entry = result.Matches.Should().ContainSingle().Which;
        entry.CanonicalId.Should().Be(Field(fixture, "canonical_id"));
        entry.WrittenForms.Should().ContainSingle().Which.Form.Should().Be(Field(fixture, "form"));
        entry.Readings.Should().ContainSingle().Which.ReadingText.Should().Be(Field(fixture, "reading"));
        var sense = entry.Senses.Should().ContainSingle().Which;
        sense.Position.Should().Be(fixture.GetProperty("sense_ordinal").GetInt16());
        sense.PartOfSpeech.Should().Equal(fixture.GetProperty("pos").EnumerateArray().Select(pos => pos.GetString()!));
        var construction = Field(match, "construction");
        if (construction == "exact")
        {
            result.Alternatives.Should().BeNullOrEmpty();
            result.Conjugation.Should().BeNull();
            provider.CallCount.Should().Be(0);
            dataset.ReceivedCalls().Should().BeEmpty();
            words.ReceivedCalls().Should().ContainSingle();
            return;
        }

        var explanation = result.Alternatives.Should().ContainSingle().Which;
        explanation.RuleId.Should().Be(construction switch
        {
            "te-shimau-nonpast" => "conjugation:verb-te-shimau",
            "te-shimau-past" => "conjugation:verb-te-shimau:plain-past",
            _ => throw new InvalidOperationException($"Unsupported corpus construction: {construction}")
        });
        explanation.DatasetVersion.Should().Be(Candidate);
        explanation.RuleVersion.Should().Be(1);
        explanation.LanguageTag.Should().Be("vi");
        explanation.Explanation.Should().Contain("có thể ngoài ý muốn tùy ngữ cảnh");
        explanation.BaseForm.Should().Be(Field(fixture, "form"));
        explanation.Surface.Should().Be(Field(occurrence, "surface"));
        (explanation.Span.Start, explanation.Span.End, explanation.Span.Unit).Should()
            .Be((occurrence.GetProperty("span")[0].GetInt32(), occurrence.GetProperty("span")[1].GetInt32(), "utf16"));
        explanation.Segments.Select(segment => (segment.Surface, segment.Span.Start, segment.Span.End, segment.Span.Unit)).Should()
            .Equal(match.GetProperty("segments").EnumerateArray().Select(segment =>
                (Field(segment, "surface"), segment.GetProperty("span")[0].GetInt32(), segment.GetProperty("span")[1].GetInt32(), "utf16")));
        result.Capabilities.Conjugation.Should().Be("completed");
        provider.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ExistingCandidatePinKeepsItsExplanationsAndNewRulesRequireTheNewPin()
    {
        using var corpus = ReadCorpus();
        var input = ReadCase(corpus, "AUX-01-PAST").GetProperty("input");
        var entries = FixturesToEntries(InputFixtures(corpus, input).Values);
        var (baseline, _, _, _) = Setup(ConjugationCandidateData.Version, ConjugationCandidateData.ReadContent(), entries);
        var (candidate, _, _, _) = Setup(Candidate, await CandidateContent.Value, entries);
        var ct = TestContext.Current.CancellationToken;

        var baselineNewSurface = await baseline.ExecuteAsync(Field(input, "text"), ct);
        baselineNewSurface.Alternatives.Should().BeNullOrEmpty();
        baselineNewSurface.Capabilities.Conjugation.Should().Be("unavailable");
        var baselineProgressive = (await baseline.ExecuteAsync("食べている", ct)).Alternatives.Should().ContainSingle().Which;
        var candidateProgressive = (await candidate.ExecuteAsync("食べている", ct)).Alternatives.Should().ContainSingle().Which;
        baselineProgressive.RuleId.Should().Be("conjugation:verb-te-iru");
        candidateProgressive.Should().BeEquivalentTo(baselineProgressive with { DatasetVersion = Candidate });
    }

    private static (LookupWordUseCase Lookup, ObservedTokenizer Provider, IConjugationDatasetRepository Dataset, IDictionaryRepository Words)
        Setup(string version, string content, DictionaryEntry[] entries)
    {
        var dataset = Substitute.For<IConjugationDatasetRepository>();
        dataset.GetAsync(version, Arg.Any<CancellationToken>()).Returns(new ConjugationDataset { Version = version, Content = content });
        var words = Substitute.For<IDictionaryRepository>();
        words.SearchByFormOrReadingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var query = call.ArgAt<string>(0);
            return Task.FromResult<IReadOnlyList<DictionaryEntry>>(entries.Where(entry =>
                entry.WrittenForms.Any(form => form.Form == query) || entry.Readings.Any(reading => reading.ReadingText == query)).ToArray());
        });
        var provider = new ObservedTokenizer();
        return (new(words, new GrpcDictionaryTokenizerAdapter(provider, dataset, version)), provider, dataset, words);
    }

    private static JsonDocument ReadCorpus() => ConjugationCorpusFiles.Read(Path.Combine(CorpusRoot, "yco-20261110.1", "cases.json"));

    private static JsonElement ReadCase(JsonDocument corpus, string caseId) => corpus.RootElement.GetProperty("cases").EnumerateArray()
        .Single(item => Field(item, "case_id") == caseId);

    private static Dictionary<string, JsonElement> InputFixtures(JsonDocument corpus, JsonElement input)
    {
        var ids = input.GetProperty("fixture_ids").EnumerateArray().Select(value => value.GetString()!).ToHashSet();
        return corpus.RootElement.GetProperty("dictionary_fixtures").EnumerateArray()
            .Where(item => ids.Contains(Field(item, "fixture_id"))).ToDictionary(item => Field(item, "fixture_id"));
    }

    private static DictionaryEntry[] FixturesToEntries(IEnumerable<JsonElement> fixtures) =>
        fixtures.GroupBy(fixture => Field(fixture, "canonical_id")).Select(group =>
        {
            var entry = new DictionaryEntry { CanonicalId = group.Key };
            foreach (var fixture in group)
            {
                var form = Field(fixture, "form");
                var reading = Field(fixture, "reading");
                var id = entry.Senses.Count + 1;
                string[]? readingRestrictions = fixture.TryGetProperty("reading_restricted_to", out var restrictions)
                    ? restrictions.EnumerateArray().Select(value => value.GetString()!).ToArray() : null;
                entry.WrittenForms.Add(new() { Id = id, Form = form });
                entry.Readings.Add(new() { Id = id, ReadingText = reading, RestrictedToForms = readingRestrictions });
                var ordinal = fixture.TryGetProperty("sense_ordinal", out var position) ? position.GetInt16() : checked((short)id);
                entry.Senses.Add(new()
                {
                    SenseKey = fixture.TryGetProperty("sense_key", out var key) ? key.GetString()! : ordinal.ToString(CultureInfo.InvariantCulture),
                    Position = ordinal,
                    PartOfSpeech = fixture.GetProperty("pos").EnumerateArray().Select(value => value.GetString()!).ToArray(),
                    Applicabilities = readingRestrictions is null ? [] : [new() { WrittenFormId = id, ReadingId = id }]
                });
            }
            return entry;
        }).ToArray();

    private static string Field(JsonElement item, string property) => item.GetProperty(property).GetString()!;

    private static async Task<string> CompileCandidateAsync()
    {
        var info = new ProcessStartInfo("node")
        {
            WorkingDirectory = ConjugationCorpusFiles.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        info.ArgumentList.Add(Path.Combine(CorpusRoot, "compile-yco-candidate.mjs"));
        using var process = Process.Start(info)!;
        var ct = TestContext.Current.CancellationToken;
        var output = process.StandardOutput.ReadToEndAsync(ct);
        var errors = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        Assert.True(process.ExitCode == 0, await errors);
        return await output;
    }

    private sealed class ObservedTokenizer : TokenizerService.TokenizerServiceClient
    {
        public int CallCount { get; private set; }

        public override AsyncUnaryCall<TokenizeResponse> TokenizeAsync(TokenizeRequest request, CallOptions options)
        {
            CallCount++;
            Assert.Equal("C", request.Mode);
            options.CancellationToken.ThrowIfCancellationRequested();
            return new(Task.FromResult(ObservedResponse(request.Text)), Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess, () => new Metadata(), () => { });
        }

        private static TokenizeResponse ObservedResponse(string text)
        {
            // Mode C observations from the existing SudachiPy tokenizer. Malformed 読んて still has the 読む head.
            (string Surface, string Base, string Pos) lexical = text.StartsWith("食べ", StringComparison.Ordinal)
                ? ("食べ", "食べる", "動詞,一般,*,*,下一段-バ行,連用形-一般")
                : ("読ん", "読む", "動詞,一般,*,*,五段-マ行,連用形-撥音便");
            Assert.StartsWith(lexical.Surface, text, StringComparison.Ordinal);
            var connective = text[2..3];
            var suffix = text[3..];
            var pieces = new List<(string Surface, string Base, string Pos)>
            {
                lexical,
                (connective, connective, "助詞,接続助詞,*,*,*,*")
            };
            switch (suffix)
            {
                case "しまう": pieces.Add(("しまう", "しまう", "動詞,非自立可能,*,*,五段-ワア行,終止形-一般")); break;
                case "しまった":
                    pieces.Add(("しまっ", "しまう", "動詞,非自立可能,*,*,五段-ワア行,連用形-促音便"));
                    pieces.Add(("た", "た", "助動詞,*,*,*,助動詞-タ,終止形-一般"));
                    break;
                case "いる": pieces.Add(("いる", "いる", "動詞,非自立可能,*,*,上一段-ア行,終止形-一般")); break;
                default: throw new InvalidOperationException("Missing tokenizer observation for the corpus input.");
            }

            var response = new TokenizeResponse();
            var start = 0;
            foreach (var (surface, root, pos) in pieces)
            {
                var end = start + surface.Length;
                response.Tokens.Add(new TokenSpec { Surface = surface, BaseForm = root, Pos = pos, SpanStart = start, SpanEnd = end });
                start = end;
            }
            return response;
        }
    }
}
