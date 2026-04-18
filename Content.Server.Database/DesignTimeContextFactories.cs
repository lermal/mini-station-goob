// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT
#if TOOLS

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SQLitePCL;

// ReSharper disable UnusedType.Global

namespace Content.Server.Database;

public sealed class DesignTimeContextFactoryPostgres : IDesignTimeDbContextFactory<PostgresServerDbContext>
{
    public PostgresServerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostgresServerDbContext>();
        optionsBuilder.UseNpgsql("Server=localhost");
        return new PostgresServerDbContext(optionsBuilder.Options);
    }
}

public sealed class DesignTimeContextFactorySqlite : IDesignTimeDbContextFactory<SqliteServerDbContext>
{
    public SqliteServerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteServerDbContext>();
        optionsBuilder.UseSqlite("Data Source=:memory:");
        return new SqliteServerDbContext(optionsBuilder.Options);
    }
}

#endif
