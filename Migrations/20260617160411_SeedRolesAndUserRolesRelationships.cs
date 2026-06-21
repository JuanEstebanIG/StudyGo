using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudyGo.Migrations
{
    /// <inheritdoc />
    public partial class SeedRolesAndUserRolesRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("a1111111-1111-1111-1111-111111111111"), "Administrador" },
                    { new Guid("b2222222-2222-2222-2222-222222222222"), "Docente" },
                    { new Guid("c3333333-3333-3333-3333-333333333333"), "Estudiante" }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { new Guid("c3333333-3333-3333-3333-333333333333"), new Guid("1fbed7cb-b26a-49c3-b9a5-b1f74111548a") },
                    { new Guid("a1111111-1111-1111-1111-111111111111"), new Guid("b0cf00ae-dc66-4092-8113-efa1b46959a6") },
                    { new Guid("c3333333-3333-3333-3333-333333333333"), new Guid("c20a47c0-8977-4e0a-b612-7f8d7cd4398d") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("b2222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { new Guid("c3333333-3333-3333-3333-333333333333"), new Guid("1fbed7cb-b26a-49c3-b9a5-b1f74111548a") });

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { new Guid("a1111111-1111-1111-1111-111111111111"), new Guid("b0cf00ae-dc66-4092-8113-efa1b46959a6") });

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { new Guid("c3333333-3333-3333-3333-333333333333"), new Guid("c20a47c0-8977-4e0a-b612-7f8d7cd4398d") });

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a1111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("c3333333-3333-3333-3333-333333333333"));
        }
    }
}
