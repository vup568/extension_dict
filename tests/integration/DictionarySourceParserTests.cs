using System.Text;
using System.Text.Json;
using System.Xml;
using FluentAssertions;
using Infrastructure.DictionaryImport.Parsing;
using Xunit;

namespace IntegrationTests;

/// <summary>Original synthetic fixtures, authored for this project; no redistributed corpus content.</summary>
public sealed class DictionarySourceParserTests
{
    private const string Entry = """
        <entry><ent_seq>910001</ent_seq>
          <k_ele><keb>甲</keb><ke_pri>news1</ke_pri></k_ele><k_ele><keb>乙</keb></k_ele>
          <r_ele><reb>こう</reb><re_restr>甲</re_restr></r_ele>
          <r_ele><reb>おつ</reb><re_restr>乙</re_restr></r_ele>
          <r_ele><reb>かな</reb><re_nokanji/></r_ele>
          <sense><stagk>甲</stagk><stagr>こう</stagr><pos>&n;</pos><gloss>synthetic first</gloss></sense>
          <sense><stagr>かな</stagr><gloss>synthetic kana</gloss><gloss xml:lang="vie">chưa duyệt</gloss></sense>
        </entry>
        """;

    [Fact]
    public void Jmdict_preserves_identity_codes_inheritance_no_kanji_and_joint_pairs()
    {
        using var stream = Input("<!DOCTYPE JMdict [<!ENTITY n \"noun (synthetic)\">]><JMdict>" + Entry + "</JMdict>");
        var record = new JmdictParser(new Dictionary<string, string> { ["n"] = "noun (synthetic)" }).Read(stream, TestContext.Current.CancellationToken).Single();
        record.SourceIdentity.Should().Be("910001");
        record.Ordinal.Should().Be(1);
        var entry = record.Entry!;
        entry.CanonicalId.Should().Be("dictionary:jmdict:910001");
        entry.WrittenForms.First().IsCommon.Should().BeTrue();
        entry.Readings.Last().IsNoKanji.Should().BeTrue();
        entry.Readings.First().RestrictedToForms.Should().Equal("甲");
        entry.Senses.Select(s => s.SenseKey).Should().Equal("1", "2");
        entry.Senses.Should().OnlyContain(s => s.PartOfSpeech!.SequenceEqual(new[] { "n" }));
        var firstPair = entry.Senses.First().Applicabilities.Should().ContainSingle().Subject;
        firstPair.WrittenForm!.Form.Should().Be("甲");
        firstPair.ReadingEntity!.ReadingText.Should().Be("こう");
        var kanaPair = entry.Senses.Last().Applicabilities.Should().ContainSingle().Subject;
        kanaPair.WrittenForm.Should().BeNull();
        kanaPair.ReadingEntity!.ReadingText.Should().Be("かな");
        entry.Senses.Last().Glosses.Should().ContainSingle(g => g.LanguageTag == "en");
        JsonDocument.Parse(record.RawJson).RootElement.GetProperty("rawXml").GetString().Should().Contain("&n;").And.Contain("chưa duyệt");
    }

    [Fact]
    public void Jmdict_combined_restrictions_intersect_reading_form_restrictions()
    {
        var fixture = Entry.Replace("<stagk>甲</stagk><stagr>こう</stagr>", "<stagk>甲</stagk><stagk>乙</stagk><stagr>こう</stagr>");
        using var stream = Input("<JMdict>" + fixture.Replace("&n;", "n") + "</JMdict>");
        var pairs = new JmdictParser().Read(stream, TestContext.Current.CancellationToken).Single().Entry!.Senses.First().Applicabilities;
        pairs.Should().ContainSingle(p => p.WrittenForm!.Form == "甲" && p.ReadingEntity!.ReadingText == "こう");
    }

    [Fact]
    public void Jmdict_audit_inventories_impossible_joint_no_kanji_pair_without_importing_or_dropping_it()
    {
        // Synthetic shape matching the contradiction discovered in pinned upstream entry1339160.
        var impossible = Entry.Replace("<stagk>甲</stagk><stagr>こう</stagr>", "<stagk>甲</stagk><stagr>かな</stagr>").Replace("&n;", "n");
        var valid = Entry.Replace("910001", "910002").Replace("&n;", "n");
        using var auditStream = Input("<JMdict>" + impossible + valid + "</JMdict>");
        var inventory = new JmdictParser().Audit(auditStream, TestContext.Current.CancellationToken).ToArray();
        inventory.Should().HaveCount(2);
        inventory[0].SourceIdentity.Should().Be("910001");
        inventory[0].Error.Should().Be("Sense 1 has no applicable form/reading pair.");
        inventory[0].Parsed.Should().BeNull();
        JsonDocument.Parse(inventory[0].RawJson).RootElement.GetProperty("rawXml").GetString().Should().Contain("<re_nokanji");
        inventory[1].Parsed!.Entry!.CanonicalId.Should().Be("dictionary:jmdict:910002");
        using var importStream = Input("<JMdict>" + impossible + valid + "</JMdict>");
        Action import = () => new JmdictParser().Read(importStream, TestContext.Current.CancellationToken).ToArray();
        import.Should().Throw<InvalidDataException>().WithMessage("*910001*Sense 1*");
    }

    [Fact]
    public void Jmdict_audited_snapshot_retains_the_known_impossible_constraints_without_dropping_the_sense()
    {
        using var stream = Input("<JMdict>" + AuditedContradictionFixture + "</JMdict>");
        var record = new JmdictParser(artifactSha256: AuditedSnapshotSha256).Read(stream, TestContext.Current.CancellationToken).Single();
        var second = record.Entry!.Senses.Last();
        second.Glosses.Should().ContainSingle().Which.GlossText.Should().Be("synthetic second");
        var constraint = second.Applicabilities.Should().ContainSingle().Subject;
        constraint.WrittenForm!.Form.Should().Be("出し");
        constraint.ReadingEntity!.ReadingText.Should().Be("ダシ");
        constraint.ReadingEntity.IsNoKanji.Should().BeTrue();
        JsonDocument.Parse(record.RawJson).RootElement.GetProperty("rawXml").GetString().Should().Contain("<stagr>ダシ</stagr>");
    }

    private const string AuditedSnapshotSha256 = "1e89ac398ec0230b7bb1daa246ce0095421bf3667bb6509aab93e26e7fdfd1b5";
    // Synthetic glosses; only the pinned exception's identity and restriction fields reflect the source audit.
    private const string AuditedContradictionFixture = """
        <entry><ent_seq>1339160</ent_seq><k_ele><keb>出し</keb></k_ele><k_ele><keb>出汁</keb></k_ele>
          <r_ele><reb>だし</reb></r_ele><r_ele><reb>ダシ</reb><re_nokanji/></r_ele>
          <sense><gloss>synthetic first</gloss></sense>
          <sense><stagk>出し</stagk><stagr>ダシ</stagr><gloss>synthetic second</gloss></sense>
        </entry>
        """;

    [Theory]
    [InlineData("different-hash")]
    [InlineData("different-entry")]
    [InlineData("different-sense")]
    [InlineData("different-form")]
    [InlineData("different-reading")]
    public void Jmdict_other_contradictions_remain_strict(string change)
    {
        var fixture = change switch
        {
            "different-entry" => AuditedContradictionFixture.Replace("1339160", "1339161"),
            "different-sense" => AuditedContradictionFixture.Replace("<sense><gloss>synthetic first</gloss></sense>", ""),
            "different-form" => AuditedContradictionFixture.Replace("<stagk>出し</stagk>", "<stagk>出汁</stagk>"),
            "different-reading" => AuditedContradictionFixture.Replace("ダシ", "カナ"),
            _ => AuditedContradictionFixture
        };
        using var stream = Input("<JMdict>" + fixture + "</JMdict>");
        var hash = change == "different-hash" ? new string('0', 64) : AuditedSnapshotSha256;
        Action parse = () => new JmdictParser(artifactSha256: hash).Read(stream, TestContext.Current.CancellationToken).ToArray();
        parse.Should().Throw<InvalidDataException>().WithMessage("*no applicable form/reading pair*");
    }

    [Theory]
    [InlineData("<!DOCTYPE JMdict SYSTEM 'file:///does-not-exist'><JMdict/>")]
    [InlineData("<!DOCTYPE JMdict SYSTEM 'https://invalid.example/dtd'><JMdict/>")]
    [InlineData("<!DOCTYPE JMdict [<!ENTITY n SYSTEM 'file:///does-not-exist'>]><JMdict/>")]
    [InlineData("<!DOCTYPE JMdict [<!ENTITY % nested 'boom'>%nested;]><JMdict/>")]
    [InlineData("<!DOCTYPE JMdict [<!ENTITY n '&other;'>]><JMdict/>")]
    [InlineData("<!DOCTYPE JMdict [<!ENTITY unknown 'noun'>]><JMdict/>")]
    [InlineData("<!DOCTYPE JMdict [<!ENTITY n 'changed meaning'>]><JMdict/>")]
    public void Xml_external_nested_unknown_and_changed_entities_fail_closed(string xml)
    {
        using var stream = Input(xml);
        Action parse = () => new JmdictParser(new Dictionary<string, string> { ["n"] = "noun" }).Read(stream, TestContext.Current.CancellationToken).ToArray();
        parse.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Xml_inert_internal_schema_declarations_are_allowed()
    {
        using var stream = Input("<!DOCTYPE JMdict [<!-- synthetic schema --><!ELEMENT JMdict (entry*)><!ELEMENT entry ANY><!ATTLIST gloss xml:lang CDATA #IMPLIED>]><JMdict>" + Entry.Replace("&n;", "n") + "</JMdict>");
        new JmdictParser().Read(stream, TestContext.Current.CancellationToken).Should().ContainSingle();
    }

    [Fact]
    public void Xml_identical_repeated_entity_declarations_are_pinned_and_bounded()
    {
        const string xml = "<!DOCTYPE JMdict [<!ENTITY n 'noun'><!ENTITY n 'noun'>]><JMdict/>";
        using var stream = Input(xml);
        new JmdictParser(new Dictionary<string, string> { ["n"] = "noun" }).Read(stream, TestContext.Current.CancellationToken).Should().BeEmpty();
        using var bounded = Input(xml);
        Action parse = () => new JmdictParser(new Dictionary<string, string> { ["n"] = "noun" }, new SourceParserLimits { MaxEntities = 1 }).Read(bounded, TestContext.Current.CancellationToken).ToArray();
        parse.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Xml_record_depth_prolog_and_expanded_stream_limits_are_enforced()
    {
        var xml = "<JMdict>" + Entry.Replace("&n;", "n") + "</JMdict>";
        foreach (var limits in new[]
                 {
                     new SourceParserLimits { MaxDepth = 2 }, new SourceParserLimits { MaxRecordCharacters = 100 },
                     new SourceParserLimits { MaxInputCharacters = 100 }, new SourceParserLimits { MaxPrologCharacters = 1 }
                 })
        {
            using var stream = Input(xml);
            Action parse = () => new JmdictParser(limits: limits).Read(stream, TestContext.Current.CancellationToken).ToArray();
            parse.Should().Throw<InvalidDataException>("limits {0}", limits);
        }
    }

    [Fact]
    public void Xml_truncated_document_fails_and_cancellation_is_observed()
    {
        using var truncated = Input("<JMdict>" + Entry.Replace("&n;", "n"));
        Action parse = () => new JmdictParser().Read(truncated, TestContext.Current.CancellationToken).ToArray();
        parse.Should().Throw<XmlException>();
        using var stream = Input("<JMdict/>");
        Action cancelled = () => new JmdictParser().Read(stream, new CancellationToken(true)).ToArray();
        cancelled.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void Kanjidic_preserves_supplementary_scalar_hanviet_group_metadata_and_raw_jlpt()
    {
        using var stream = Input("""
            <kanjidic2><header><file_version>synthetic</file_version></header><character>
              <literal>𠮷</literal><codepoint><cp_value cp_type="ucs">20bb7</cp_value></codepoint>
              <radical><rad_value rad_type="classical">30</rad_value></radical>
              <misc><stroke_count>6</stroke_count><stroke_count>7</stroke_count><jlpt>2</jlpt><variant var_type="ucs">5409</variant></misc>
              <reading_meaning><rmgroup><reading r_type="ja_on">キチ</reading><reading r_type="ja_kun">よ.い</reading>
              <reading r_type="vietnam">CÁT</reading><meaning>synthetic luck</meaning><meaning m_lang="vi">nghĩa chưa duyệt</meaning>
              </rmgroup><nanori>よし</nanori></reading_meaning>
            </character></kanjidic2>
            """);
        var record = new KanjidicParser().Read(stream, TestContext.Current.CancellationToken).Single();
        record.SourceIdentity.Should().Be("20BB7");
        var kanji = record.Kanji!;
        kanji.Character.Should().Be("𠮷");
        kanji.CanonicalId.Should().Be("kanji:20BB7");
        kanji.UnicodeCodepoint.Should().Be("U+20BB7");
        kanji.StrokeCount.Should().Be(6);
        kanji.RadicalNumber.Should().Be(30);
        kanji.KunReadings.Should().Equal("よ.い");
        kanji.HanViet.Should().Be("CÁT");
        kanji.MeaningsEn.Should().Equal("synthetic luck");
        kanji.MeaningsVi.Should().BeEmpty();
        kanji.JlptLevel.Should().BeNull();
        JsonDocument.Parse(record.RawJson).RootElement.GetProperty("rawXml").GetString().Should().Contain("<jlpt>2</jlpt>").And.Contain("<rmgroup>").And.Contain("5409");
        JsonDocument.Parse(record.RawJson).RootElement.GetProperty("headerXml").GetString().Should().Contain("synthetic");
    }

    [Fact]
    public void Kanjidic_literal_and_source_codepoint_must_agree()
    {
        using var stream = Input("<kanjidic2><character><literal>甲</literal><codepoint><cp_value cp_type='ucs'>4E59</cp_value></codepoint></character></kanjidic2>");
        Action parse = () => new KanjidicParser().Read(stream, TestContext.Current.CancellationToken).ToArray();
        parse.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Vietnamese_inventory_retains_duplicate_headwords_full_content_tabs_and_html_quarantined()
    {
        var longContent = "<script>unsafe()</script>" + new string('ấ', 4000) + "\\nmore\tlast";
        using var stream = Input($"甲\t{longContent}\r\n甲\tnghĩa khác\nmalformed\n\n");
        var records = new VietnameseInventoryParser(new string('A', 64)).Read(stream, TestContext.Current.CancellationToken).ToArray();
        records.Should().HaveCount(4);
        records.Select(r => r.SourceIdentity).Should().OnlyHaveUniqueItems();
        records[0].SourceIdentity.Should().Be(new string('a', 64) + ":1");
        records[0].Content.Should().Be(longContent);
        records[0].RawLine.Should().Be("甲\t" + longContent);
        records[1].Headword.Should().Be("甲");
        records.Should().OnlyContain(r => r.Disposition == "quarantined");
        records[2].Reason.Should().Be("malformed-tab-record");
        records[3].RawLine.Should().BeEmpty();
    }

    [Fact]
    public void Vietnamese_over_limit_record_fails_without_silent_truncation()
    {
        using var stream = Input("甲\t" + new string('a', 200));
        Action parse = () => new VietnameseInventoryParser(new string('a', 64), new SourceParserLimits { MaxRecordCharacters = 100 }).Read(stream, TestContext.Current.CancellationToken).ToArray();
        parse.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Parser_yields_records_before_later_records_are_materialized()
    {
        using var stream = Input("<JMdict>" + Entry.Replace("&n;", "n") + "<entry><ent_seq>invalid</ent_seq></entry></JMdict>");
        using var iterator = new JmdictParser().Read(stream, TestContext.Current.CancellationToken).GetEnumerator();
        iterator.MoveNext().Should().BeTrue();
        iterator.Current.SourceIdentity.Should().Be("910001");
        Action next = () => iterator.MoveNext();
        next.Should().Throw<InvalidDataException>();
        stream.CanRead.Should().BeTrue();
    }

    private static MemoryStream Input(string value) => new(Encoding.UTF8.GetBytes(value));
}
