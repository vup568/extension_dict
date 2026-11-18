using System.Text.RegularExpressions;
using Npgsql;

namespace Infrastructure.DictionaryImport;

/// <summary>Only fresh loopback databases explicitly named for this workflow may be prepared.</summary>
public static partial class IsolatedImportTarget
{
    [GeneratedRegex("^jp_dict_real_[a-z0-9][a-z0-9_]{0,40}$", RegexOptions.CultureInvariant)]
    private static partial Regex DatabasePattern();

    public static NpgsqlConnectionStringBuilder Check(string connectionString, string confirmedTarget)
    {
        NpgsqlConnectionStringBuilder target;
        try { target = new(connectionString); }
        catch (ArgumentException) { throw new InvalidOperationException("IMPORT_TARGET_INVALID"); }
        // EARS[Unwanted] FR-009: Prefix alone is insufficient; actual database identity/ownership is checked in-session.
        if (!DatabasePattern().IsMatch(confirmedTarget) || target.Database != confirmedTarget ||
            target.Host is not ("127.0.0.1" or "localhost" or "::1") ||
            (!string.IsNullOrEmpty(target.SearchPath) && target.SearchPath != "public") ||
            !string.IsNullOrEmpty(target.Options) || target.Multiplexing || System.Transactions.Transaction.Current is not null)
            throw new InvalidOperationException("IMPORT_TARGET_REFUSED");
        return target;
    }
}
