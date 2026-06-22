using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudyGo.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Institutions",
                columns: new[] { "Id", "Name" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), "Institución Educativa StudyGo" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "DisplayName", "Email", "InstitutionId", "Password" },
                values: new object[,]
                {
                    { new Guid("1fbed7cb-b26a-49c3-b9a5-b1f74111548a"), "Steven Florez", "stevenflorez2304@gmail.com", new Guid("00000000-0000-0000-0000-000000000001"), "OAuth_External_Account" },
                    { new Guid("b0cf00ae-dc66-4092-8113-efa1b46959a6"), "Luis Alejandro Londoño", "londonovalleluisalejandro@gmail.com", new Guid("00000000-0000-0000-0000-000000000001"), "OAuth_External_Account" },
                    { new Guid("c20a47c0-8977-4e0a-b612-7f8d7cd4398d"), "Usuario Isaza", "isazaj601@gmail.com", new Guid("00000000-0000-0000-0000-000000000001"), "OAuth_External_Account" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("1fbed7cb-b26a-49c3-b9a5-b1f74111548a"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("b0cf00ae-dc66-4092-8113-efa1b46959a6"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("c20a47c0-8977-4e0a-b612-7f8d7cd4398d"));

            migrationBuilder.DeleteData(
                table: "Institutions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));
        }
    }
}
