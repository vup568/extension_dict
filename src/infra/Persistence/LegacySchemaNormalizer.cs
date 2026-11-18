using System.Data;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;

namespace Infrastructure.Persistence;

/// <summary>Explicit, transactionally guarded recovery for the documented pre-EF local schema.</summary>
/// <remarks>Never call automatically during startup. The operator must verify a backup and clone first.</remarks>
public static class LegacySchemaNormalizer
{
    private const string Initial = "20260821075041_InitialCreate";
    private const string StrictIndex = "legacy_uq_sense_applicabilities_combo";
    private const string StrictDefinition = "UNIQUE USING btree (sense_id, COALESCE(written_form_id, (0)::bigint), COALESCE(reading_id, (0)::bigint))";
    private static readonly HashSet<string> NullableJson = ["kanji_records.source_metadata", "grammar_rules.examples", "grammar_rules.matcher_metadata", "grammar_rules.source_metadata", "source_manifests.metadata"];
    private static readonly HashSet<string> MissingIndexes = ["IX_knowledge_release_manifests_manifest_id", "IX_localized_glosses_source_record_id", "IX_sense_applicabilities_reading_id", "IX_sense_applicabilities_written_form_id"];
    private static readonly HashSet<string> TimestampTables = ["dictionary_entries", "dictionary_senses", "grammar_rules", "kanji_records", "learning_references", "localized_glosses", "readings", "users", "written_forms"];

    public static async Task NormalizeAndBaselineAsync(string targetConnection, string referenceConnection, CancellationToken cancellationToken = default)
    {
        await using var reference = new NpgsqlConnection(referenceConnection);
        await using var target = new NpgsqlConnection(targetConnection);
        await reference.OpenAsync(cancellationToken);
        await target.OpenAsync(cancellationToken);
        Require(reference.DataSource != target.DataSource || reference.Database != target.Database);
        await using var referenceTransaction = await reference.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        await RunAsync(reference, "SET TRANSACTION READ ONLY", cancellationToken);
        Require(await ScalarAsync(reference, "SELECT count(*) FROM \"__EFMigrationsHistory\"", cancellationToken) is long referenceCount && referenceCount == 1);
        Require((string?)await ScalarAsync(reference, "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\"", cancellationToken) == Initial);
        var version = (string?)await ScalarAsync(reference, "SELECT \"ProductVersion\" FROM \"__EFMigrationsHistory\"", cancellationToken);
        Require(version == "10.0.4");
        var expected = await ReadCatalogAsync(reference, cancellationToken);

        await using var transaction = await target.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await RunAsync(target, "SET LOCAL lock_timeout = '5s'; SET LOCAL search_path = public, pg_catalog", cancellationToken);
        var actual = await ReadCatalogAsync(target, cancellationToken);
        Require(actual.Where(x => x.Kind == "table").Select(Key).Order().SequenceEqual(expected.Where(x => x.Kind == "table").Select(Key).Order()));
        await RunAsync(target, "LOCK TABLE " + string.Join(",", expected.Where(x => x.Kind == "table").Select(x => "public." + Quote(x.Name))) + " IN ACCESS EXCLUSIVE MODE", cancellationToken);
        Require(await ScalarAsync(target, "SELECT count(*) FROM \"__EFMigrationsHistory\"", cancellationToken) is long targetCount && targetCount == 0);
        actual = await ReadCatalogAsync(target, cancellationToken);
        var changes = Compare(expected, actual, allowChanges: true, allowPreservedIndex: false);
        await ValidateSerialSequencesAsync(target, expected, actual, cancellationToken);
        foreach (var sql in changes) await RunAsync(target, sql, cancellationToken);
        _ = Compare(expected, await ReadCatalogAsync(target, cancellationToken), allowChanges: false, allowPreservedIndex: changes.Any(x => x.Contains(StrictIndex, StringComparison.Ordinal)));
        await using var insert = new NpgsqlCommand("INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES (@migration, @version)", target);
        insert.Parameters.AddWithValue("migration", Initial);
        insert.Parameters.AddWithValue("version", version!);
        await insert.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static List<string> Compare(List<Item> expected, List<Item> actual, bool allowChanges, bool allowPreservedIndex)
    {
        var changes = new List<string>();
        foreach (var kind in new[] { "table", "other" })
        {
            var want = expected.Where(x => x.Kind == kind).OrderBy(Key).ToArray();
            var have = actual.Where(x => x.Kind == kind).OrderBy(Key).ToArray();
            Require(want.Length == have.Length);
            for (var i = 0; i < want.Length; i++) Require(Key(want[i]) == Key(have[i]) && want[i].Fields.SequenceEqual(have[i].Fields));
        }
        Require(expected.All(x => x.Kind != "function" && x.Kind != "trigger"));
        foreach (var item in actual.Where(x => x.Kind == "function"))
        {
            Require(item.Name == "trg_set_updated_at" && item.Fields.Length == 11);
            Require(item.Fields[..4].SequenceEqual(new[] { "plpgsql", "false", "", "trigger" }));
            Require(Regex.Replace(item.Fields[4], @"\s+", "") == "BEGINNEW.updated_at=now();RETURNNEW;END;");
            Require(item.Fields[5..].SequenceEqual(new[] { "", "v", "u", "false", "false", "f" }));
        }
        Require(actual.Count(x => x.Kind == "function") <= 1);
        foreach (var item in actual.Where(x => x.Kind == "trigger"))
        {
            Require(TimestampTables.Contains(item.Table) && item.Name == "trg_" + item.Table + "_updated_at");
            Require(item.Fields.SequenceEqual(new[] { "trg_set_updated_at", "public", "19", "O", "", "0", "", "", "0" }));
            Require(actual.Any(x => x.Kind == "function" && x.Name == "trg_set_updated_at"));
        }

        var columns = actual.Where(x => x.Kind == "column").ToDictionary(Key);
        Require(columns.Count == expected.Count(x => x.Kind == "column"));
        foreach (var want in expected.Where(x => x.Kind == "column"))
        {
            Require(columns.TryGetValue(Key(want), out var have));
            var fields = (string[])have!.Fields.Clone();
            if (fields[4] == "" && want.Fields[4] == "d")
            {
                var sequence = actual.SingleOrDefault(x => x.Kind == "sequence" && x.Table == have.Table && x.Fields[7] == have.Name);
                Require(sequence is not null && sequence.Fields[8] == "a");
                Require(fields[2] == "nextval('" + sequence!.Name + "'::regclass)" || fields[2] == "nextval('public." + sequence.Name + "'::regclass)");
                fields[2] = "";
                fields[4] = "d"; // Documented serial/identity compatibility: do not change or consume sequences.
            }
            if (fields[1] == "true" && want.Fields[1] == "false" && NullableJson.Contains(want.Table + "." + want.Name))
            {
                Require(allowChanges);
                changes.Add("ALTER TABLE public." + Quote(want.Table) + " ALTER COLUMN " + Quote(want.Name) + " DROP NOT NULL");
                fields[1] = "false";
            }
            Require(fields.SequenceEqual(want.Fields));
        }
        var sequences = actual.Where(x => x.Kind == "sequence").ToDictionary(Key);
        Require(sequences.Count == expected.Count(x => x.Kind == "sequence"));
        foreach (var want in expected.Where(x => x.Kind == "sequence"))
        {
            Require(sequences.TryGetValue(Key(want), out var have));
            var fields = (string[])have!.Fields.Clone();
            Require(fields[8] is "a" or "i");
            fields[8] = want.Fields[8];
            Require(fields.SequenceEqual(want.Fields));
        }

        var constraints = actual.Where(x => x.Kind == "constraint" && x.Fields[0] != "u").ToList();
        foreach (var want in expected.Where(x => x.Kind == "constraint"))
        {
            var matches = constraints.Where(x => x.Table == want.Table && x.Fields.SequenceEqual(want.Fields)).ToArray();
            Require(matches.Length == 1);
            var have = matches[0];
            if (have.Name != want.Name)
            {
                Require(allowChanges && want.Fields[0] is "p" or "f");
                changes.Add("ALTER TABLE public." + Quote(want.Table) + " RENAME CONSTRAINT " + Quote(have.Name) + " TO " + Quote(want.Name));
            }
            constraints.Remove(have);
        }
        Require(constraints.Count == 0);

        var indexes = actual.Where(x => x.Kind == "index").ToList();
        var strict = indexes.SingleOrDefault(x => x.Table == "sense_applicabilities" && x.Fields[0] == StrictDefinition);
        if (strict is not null)
        {
            Require(strict.Fields.SequenceEqual(new[] { StrictDefinition, "true", "true", "" }));
            if (strict.Name == "uq_sense_applicabilities_combo")
            {
                Require(allowChanges);
                changes.Add("ALTER INDEX public." + Quote(strict.Name) + " RENAME TO " + Quote(StrictIndex));
            }
            else Require(allowPreservedIndex && strict.Name == StrictIndex);
            indexes.Remove(strict); // Keep the old stricter uniqueness; add EF's index alongside it.
        }
        foreach (var want in expected.Where(x => x.Kind == "index"))
        {
            var matches = indexes.Where(x => x.Table == want.Table && x.Fields[..3].SequenceEqual(want.Fields[..3])).ToArray();
            Require(matches.Length <= 1);
            if (matches.Length == 0)
            {
                Require(allowChanges && (MissingIndexes.Contains(want.Name) || strict is not null && want.Name == "uq_sense_applicabilities_combo"));
                changes.Add("CREATE " + (want.Fields[0].StartsWith("UNIQUE ", StringComparison.Ordinal) ? "UNIQUE " : "") + "INDEX " + Quote(want.Name) + " ON public." + Quote(want.Table) + " " + want.Fields[0].Replace("UNIQUE ", "", StringComparison.Ordinal));
                continue;
            }
            var have = matches[0];
            Require(have.Fields[3] is "" or "u");
            if (have.Name != want.Name && have.Fields[3] == "")
            {
                Require(allowChanges);
                changes.Add("ALTER INDEX public." + Quote(have.Name) + " RENAME TO " + Quote(want.Name));
            }
            indexes.Remove(have);
        }
        Require(indexes.Count == 0);
        foreach (var unique in actual.Where(x => x.Kind == "constraint" && x.Fields[0] == "u"))
        {
            Require(unique.Fields[3] == "false" && unique.Fields[4] == "false");
            Require(actual.Any(x => x.Kind == "index" && x.Table == unique.Table && x.Name == unique.Name && x.Fields[3] == "u" && x.Fields[1] == "true"));
        }
        return changes;
    }

    private static async Task ValidateSerialSequencesAsync(NpgsqlConnection connection, List<Item> expected, List<Item> actual, CancellationToken cancellationToken)
    {
        foreach (var column in actual.Where(x => x.Kind == "column" && x.Fields[4] == "" && expected.Any(y => Key(y) == Key(x) && y.Fields[4] == "d")))
        {
            var sequence = actual.Single(x => x.Kind == "sequence" && x.Table == column.Table && x.Fields[7] == column.Name);
            var maximum = await ScalarAsync(connection, "SELECT max(" + Quote(column.Name) + ") FROM public." + Quote(column.Table), cancellationToken);
            if (maximum is null or DBNull) continue;
            await using var command = new NpgsqlCommand("SELECT last_value, is_called FROM public." + Quote(sequence.Name), connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            Require(await reader.ReadAsync(cancellationToken));
            Require(reader.GetInt64(0) >= (long)maximum && (reader.GetBoolean(1) || reader.GetInt64(0) > (long)maximum));
        }
    }

    private static async Task<List<Item>> ReadCatalogAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(CatalogSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<Item>();
        while (await reader.ReadAsync(cancellationToken))
            items.Add(new Item(reader.GetString(0), reader.GetString(1), reader.GetString(2), JsonSerializer.Deserialize<string[]>(reader.GetString(3))!));
        Require(items.Select(Key).Distinct().Count() == items.Count);
        return items;
    }

    private static async Task RunAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
    private static async Task<object?> ScalarAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync(cancellationToken);
    }
    private static string Quote(string value) => "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    private static string Key(Item item) => item.Kind + "." + item.Table + "." + item.Name;
    private static void Require(bool condition, [CallerLineNumber] int validationCheck = 0)
    {
        if (condition) return;
        var error = new InvalidOperationException("Legacy recovery refused: schema, migration history, or sequence state does not match the reviewed baseline. No recovery changes were committed.");
        error.Data["LegacyValidationCheck"] = validationCheck;
        throw error;
    }
    private sealed record Item(string Kind, string Table, string Name, string[] Fields);

    private const string CatalogSql = """
        WITH classes AS (SELECT c.* FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public')
        SELECT 'table', '', c.relname::text, jsonb_build_array(c.relkind::text,c.relpersistence::text,c.relrowsecurity::text,c.relforcerowsecurity::text,c.relispartition::text,c.relreplident::text)::text
        FROM classes c WHERE c.relkind IN ('r','p')
        UNION ALL
        SELECT 'column', c.relname::text, a.attname::text,
          jsonb_build_array(format_type(a.atttypid,a.atttypmod),a.attnotnull::text,COALESCE(pg_get_expr(d.adbin,d.adrelid),''),a.attgenerated::text,a.attidentity::text,a.attnum::text,COALESCE(coll.collname::text,''))::text
        FROM classes c JOIN pg_attribute a ON a.attrelid=c.oid LEFT JOIN pg_attrdef d ON d.adrelid=c.oid AND d.adnum=a.attnum
          LEFT JOIN pg_collation coll ON coll.oid=a.attcollation WHERE c.relkind IN ('r','p') AND a.attnum>0 AND NOT a.attisdropped
        UNION ALL
        SELECT 'constraint', c.relname::text, k.conname::text, jsonb_build_array(k.contype::text,k.convalidated::text,pg_get_constraintdef(k.oid),k.condeferrable::text,k.condeferred::text)::text
        FROM classes c JOIN pg_constraint k ON k.conrelid=c.oid
        UNION ALL
        SELECT 'index', t.relname::text, c.relname::text,
          jsonb_build_array(trim(regexp_replace(pg_get_indexdef(i.indexrelid),'^CREATE( UNIQUE)? INDEX [^ ]+ ON public\.[^ ]+ ','\1 ')),i.indisvalid::text,i.indisready::text,COALESCE(k.contype::text,''))::text
        FROM classes c JOIN pg_index i ON i.indexrelid=c.oid JOIN pg_class t ON t.oid=i.indrelid
          LEFT JOIN pg_constraint k ON k.conindid=i.indexrelid AND k.contype IN ('u','x') WHERE NOT i.indisprimary
        UNION ALL
        SELECT 'sequence', COALESCE(t.relname::text,''), c.relname::text,
          jsonb_build_array(format_type(s.seqtypid,NULL),s.seqstart::text,s.seqmin::text,s.seqmax::text,s.seqincrement::text,s.seqcache::text,s.seqcycle::text,COALESCE(a.attname::text,''),COALESCE(d.deptype::text,''))::text
        FROM classes c JOIN pg_sequence s ON s.seqrelid=c.oid LEFT JOIN pg_depend d ON d.objid=c.oid AND d.classid='pg_class'::regclass AND d.refclassid='pg_class'::regclass AND d.deptype IN ('a','i')
          LEFT JOIN pg_class t ON t.oid=d.refobjid LEFT JOIN pg_attribute a ON a.attrelid=t.oid AND a.attnum=d.refobjsubid
        UNION ALL
        SELECT 'function', '', p.proname::text,
          jsonb_build_array(l.lanname::text,p.prosecdef::text,pg_get_function_identity_arguments(p.oid),pg_get_function_result(p.oid),p.prosrc,COALESCE(p.proconfig::text,''),p.provolatile::text,p.proparallel::text,p.proisstrict::text,p.proleakproof::text,p.prokind::text)::text
        FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace JOIN pg_language l ON l.oid=p.prolang WHERE n.nspname='public'
        UNION ALL
        SELECT 'trigger', c.relname::text, t.tgname::text,
          jsonb_build_array(p.proname::text,pn.nspname::text,t.tgtype::text,t.tgenabled::text,COALESCE(pg_get_expr(t.tgqual,t.tgrelid),''),t.tgnargs::text,encode(t.tgargs,'hex'),t.tgattr::text,t.tgconstraint::text)::text
        FROM classes c JOIN pg_trigger t ON t.tgrelid=c.oid JOIN pg_proc p ON p.oid=t.tgfoid JOIN pg_namespace pn ON pn.oid=p.pronamespace WHERE NOT t.tgisinternal
        UNION ALL SELECT 'other',c.relname::text,r.rulename::text,jsonb_build_array(pg_get_ruledef(r.oid))::text
          FROM classes c JOIN pg_rewrite r ON r.ev_class=c.oid WHERE r.rulename<>'_RETURN'
        UNION ALL SELECT 'other',c.relname::text,'inherits:' || n.nspname::text || '.' || p.relname::text,jsonb_build_array(i.inhseqno::text)::text
          FROM classes c JOIN pg_inherits i ON i.inhrelid=c.oid JOIN pg_class p ON p.oid=i.inhparent JOIN pg_namespace n ON n.oid=p.relnamespace
        UNION ALL SELECT 'other',p.relname::text,'external-child:' || n.nspname::text || '.' || c.relname::text,jsonb_build_array(i.inhseqno::text)::text
          FROM classes p JOIN pg_inherits i ON i.inhparent=p.oid JOIN pg_class c ON c.oid=i.inhrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname<>'public'
        UNION ALL SELECT 'other','',c.relname::text,jsonb_build_array(c.relkind::text)::text FROM classes c WHERE c.relkind NOT IN ('r','p','i','S','t')
        UNION ALL SELECT 'other','',t.typname::text,jsonb_build_array(t.typtype::text)::text FROM pg_type t JOIN pg_namespace n ON n.oid=t.typnamespace WHERE n.nspname='public' AND t.typtype IN ('d','e','r','m')
        """;
}
