using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NobaRental.Backend.Data.Entities;

namespace NobaRental.Backend.Data.Test;

public sealed class NobaRentalDbContextTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public void NobaRentalDbContext_ModelConfiguration_HasExpectedMetadata()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<NobaRentalDbContext>()
            .UseSqlServer("Server=fake;Database=NobaRentalModelTest;Trusted_Connection=True;Encrypt=False;")
            .Options;

        using var ctx = new NobaRentalDbContext(options);

        // Act
        var model = ctx.Model;
        var entityType = model.FindEntityType(typeof(RentalBookingEntity));

        // Assert
        Assert.NotNull(entityType);
        Assert.Equal("RentalBooking", entityType.GetTableName());

        // Primary key
        var primaryKey = entityType.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(RentalBookingEntity.BookingNumber), primaryKey.Properties.Single().Name);

        var bookingNumberProperty = entityType.FindProperty(nameof(RentalBookingEntity.BookingNumber));
        Assert.NotNull(bookingNumberProperty);
        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd, bookingNumberProperty.ValueGenerated);

        // RegistrationNumber
        var regProperty = entityType.FindProperty(nameof(RentalBookingEntity.RegistrationNumber));
        Assert.NotNull(regProperty);
        Assert.Equal(20, regProperty.GetMaxLength());

        // CustomerSsn
        var ssnProperty = entityType.FindProperty(nameof(RentalBookingEntity.CustomerSsn));
        Assert.NotNull(ssnProperty);
        Assert.Equal(20, ssnProperty.GetMaxLength());

        // Monetary precision (18, 2)
        var baseDayRental = entityType.FindProperty(nameof(RentalBookingEntity.BaseDayRental));
        Assert.NotNull(baseDayRental);
        Assert.Equal(18, baseDayRental.GetPrecision());
        Assert.Equal(2, baseDayRental.GetScale());

        var baseKmPrice = entityType.FindProperty(nameof(RentalBookingEntity.BaseKmPrice));
        Assert.NotNull(baseKmPrice);
        Assert.Equal(18, baseKmPrice.GetPrecision());
        Assert.Equal(2, baseKmPrice.GetScale());

        var totalPrice = entityType.FindProperty(nameof(RentalBookingEntity.TotalPrice));
        Assert.NotNull(totalPrice);
        Assert.Equal(18, totalPrice.GetPrecision());
        Assert.Equal(2, totalPrice.GetScale());
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
