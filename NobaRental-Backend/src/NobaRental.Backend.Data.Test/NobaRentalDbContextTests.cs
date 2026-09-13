using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Data.Entities;

namespace NobaRental.Backend.Data.Test;

public sealed class NobaRentalDbContextTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public void RentalBookingEntity_Configuration_HasExpectedMetadata()
    {
        using var ctx = CreateTestDbContext();
        var entityType = ctx.Model.FindEntityType(typeof(RentalBookingEntity));

        Assert.NotNull(entityType);
        Assert.Equal("RentalBooking", entityType.GetTableName());

        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(RentalBookingEntity.BookingNumber), primaryKey.Properties.Single().Name);

        var bookingNumberProperty = entityType.FindProperty(nameof(RentalBookingEntity.BookingNumber));
        Assert.NotNull(bookingNumberProperty);
        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd, bookingNumberProperty.ValueGenerated);

        var regProperty = entityType.FindProperty(nameof(RentalBookingEntity.RegistrationNumber));
        Assert.NotNull(regProperty);
        Assert.Equal(20, regProperty.GetMaxLength());

        var ssnProperty = entityType.FindProperty(nameof(RentalBookingEntity.CustomerSsn));
        Assert.NotNull(ssnProperty);
        Assert.Equal(20, ssnProperty.GetMaxLength());

        AssertMonetaryPrecision(entityType, nameof(RentalBookingEntity.BaseDayRental));
        AssertMonetaryPrecision(entityType, nameof(RentalBookingEntity.BaseKmPrice));
        AssertMonetaryPrecision(entityType, nameof(RentalBookingEntity.TotalPrice));
    }

    [Fact]
    public void CarEntity_Configuration_HasExpectedMetadata()
    {
        using var ctx = CreateTestDbContext();
        var entityType = ctx.Model.FindEntityType(typeof(CarEntity));

        Assert.NotNull(entityType);
        Assert.Equal("Car", entityType.GetTableName());

        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(CarEntity.RegistrationNumber), primaryKey.Properties.Single().Name);

        AssertMonetaryPrecision(entityType, nameof(CarEntity.BaseDayRental));
        AssertMonetaryPrecision(entityType, nameof(CarEntity.BaseKmPrice));
    }

    private static NobaRentalDbContext CreateTestDbContext()
    {
        var options = new DbContextOptionsBuilder<NobaRentalDbContext>()
            .UseSqlServer("Server=fake;Database=NobaRentalModelTest;Trusted_Connection=True;Encrypt=False;")
            .Options;
        return new NobaRentalDbContext(options);
    }

    private static void AssertMonetaryPrecision(Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType, string propertyName)
    {
        var property = entityType.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(18, property.GetPrecision());
        Assert.Equal(2, property.GetScale());
    }

    [Fact]
    public async Task NobaRentalDbContext_EnsureCreated_WhenSqlServerAvailable()
    {
        // Arrange
        const string connectionString = "Server=(localdb)\\mssqllocaldb;Database=NobaRental_EnsureCreatedTest;Trusted_Connection=True;Encrypt=False;Connect Timeout=5";

        if (!await CanConnectToSqlServer(connectionString))
        {
            // Skip when running in an environment without SQL Server / LocalDB
            return;
        }

        var options = new DbContextOptionsBuilder<NobaRentalDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var ctx = new NobaRentalDbContext(options);

        // Act & Assert
        await ctx.Database.EnsureDeletedAsync(Token);
        var created = await ctx.Database.EnsureCreatedAsync(Token);
        Assert.True(created);

        await ctx.Database.EnsureDeletedAsync(Token);
    }

    private static async Task<bool> CanConnectToSqlServer(string connectionString)
    {
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(Token);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
