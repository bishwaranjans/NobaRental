using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace NobaRental.Backend.Data.Helpers;

public static class SqlServerDbContextOptionsBuilderExtensions
{
    /// <summary>
    /// For running locally and for tests.
    /// </summary>
    public static SqlServerDbContextOptionsBuilder WithDefaultOptions(this SqlServerDbContextOptionsBuilder builder)
    {
        return builder
            .EnableRetryOnFailure()
            .UseCompatibilityLevel(170)
            .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }

    /// <summary>
    /// For deployed environments.
    /// </summary>
    public static AzureSqlDbContextOptionsBuilder WithDefaultOptions(this AzureSqlDbContextOptionsBuilder builder)
    {
        return builder
            .EnableRetryOnFailure()
            .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }
}
