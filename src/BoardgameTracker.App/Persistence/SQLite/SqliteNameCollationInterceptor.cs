using System.Data.Common;
using BoardgameTracker.Core.Names;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BoardgameTracker.App.Persistence.SQLite;

/// <summary>Registers the domain name semantics on every connection, including already-open test connections.</summary>
public sealed class SqliteNameCollationInterceptor : DbConnectionInterceptor
{
    public override DbConnection ConnectionCreated(ConnectionCreatedEventData eventData, DbConnection result)
    {
        Register(result);
        return result;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) => Register(connection);

    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Register(connection);
        return Task.CompletedTask;
    }

    public static void Register(DbConnection connection)
    {
        if (connection is SqliteConnection sqlite)
            sqlite.CreateCollation("BGT_NAME", (left, right) => EntityName.Comparer.Compare(left?.Trim(), right?.Trim()));
    }
}
