using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NobaRental.Backend.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CarCategory",
            columns: table => new
            {
                Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                DayMultiplier = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                KmMultiplier = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                ChargesKilometers = table.Column<bool>(type: "bit", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CarCategory", x => x.Code);
            });

        migrationBuilder.CreateTable(
            name: "Station",
            columns: table => new
            {
                Code = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                City = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Station", x => x.Code);
            });

        migrationBuilder.CreateTable(
            name: "Car",
            columns: table => new
            {
                RegistrationNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                CategoryCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                CurrentMeterReadingKm = table.Column<long>(type: "bigint", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                CurrentStationCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                BaseDayRental = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                BaseKmPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Car", x => x.RegistrationNumber);
                table.ForeignKey(
                    name: "FK_Car_CarCategory_CategoryCode",
                    column: x => x.CategoryCode,
                    principalTable: "CarCategory",
                    principalColumn: "Code",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Car_Station_CurrentStationCode",
                    column: x => x.CurrentStationCode,
                    principalTable: "Station",
                    principalColumn: "Code",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "RentalBooking",
            columns: table => new
            {
                BookingNumber = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                RegistrationNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                CustomerSsn = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: false),
                CategoryCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                AppliedDayMultiplier = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                AppliedKmMultiplier = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                PickupStationCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                PickupDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                PickupMeterReadingKm = table.Column<long>(type: "bigint", nullable: false),
                ReturnStationCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true),
                ReturnDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ReturnMeterReadingKm = table.Column<long>(type: "bigint", nullable: true),
                BaseDayRental = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                BaseKmPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                Currency = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RentalBooking", x => x.BookingNumber);
                table.ForeignKey(
                    name: "FK_RentalBooking_CarCategory_CategoryCode",
                    column: x => x.CategoryCode,
                    principalTable: "CarCategory",
                    principalColumn: "Code",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_RentalBooking_Car_RegistrationNumber",
                    column: x => x.RegistrationNumber,
                    principalTable: "Car",
                    principalColumn: "RegistrationNumber",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_RentalBooking_Station_PickupStationCode",
                    column: x => x.PickupStationCode,
                    principalTable: "Station",
                    principalColumn: "Code",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_RentalBooking_Station_ReturnStationCode",
                    column: x => x.ReturnStationCode,
                    principalTable: "Station",
                    principalColumn: "Code",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "CarCategory",
            columns: new[] { "Code", "ChargesKilometers", "CreatedAt", "DayMultiplier", "DeletedAt", "IsActive", "IsDeleted", "KmMultiplier", "ModifiedAt", "Name" },
            values: new object[,]
            {
                    { "COMBI", true, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.3m, null, true, false, 1.0m, null, "Combi" },
                    { "SMALL", false, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.0m, null, true, false, 0.0m, null, "Small car" },
                    { "TRUCK", true, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 1.5m, null, true, false, 1.5m, null, "Truck" }
            });

        migrationBuilder.CreateIndex(
            name: "IX_Car_CategoryCode",
            table: "Car",
            column: "CategoryCode");

        migrationBuilder.CreateIndex(
            name: "IX_Car_CurrentStationCode",
            table: "Car",
            column: "CurrentStationCode");

        migrationBuilder.CreateIndex(
            name: "IX_RentalBooking_CategoryCode",
            table: "RentalBooking",
            column: "CategoryCode");

        migrationBuilder.CreateIndex(
            name: "IX_RentalBooking_PickupStationCode",
            table: "RentalBooking",
            column: "PickupStationCode");

        migrationBuilder.CreateIndex(
            name: "IX_RentalBooking_RegistrationNumber",
            table: "RentalBooking",
            column: "RegistrationNumber",
            unique: true,
            filter: "[Status] = 1");

        migrationBuilder.CreateIndex(
            name: "IX_RentalBooking_ReturnStationCode",
            table: "RentalBooking",
            column: "ReturnStationCode");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RentalBooking");

        migrationBuilder.DropTable(
            name: "Car");

        migrationBuilder.DropTable(
            name: "CarCategory");

        migrationBuilder.DropTable(
            name: "Station");
    }
}
