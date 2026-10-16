using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Knock.Api.Modules.Catalog.Migrations
{
    /// <inheritdoc />
    internal partial class InitialPostgis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.EnsureSchema("catalog");
            migrationBuilder.EnsureSchema("identity");
            migrationBuilder.EnsureSchema("requests");
            migrationBuilder.EnsureSchema("messaging");
            migrationBuilder.EnsureSchema("reputation");
            migrationBuilder.EnsureSchema("trust");
            migrationBuilder.EnsureSchema("notifications");
            migrationBuilder.EnsureSchema("assistant");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSchema("assistant");
            migrationBuilder.DropSchema("notifications");
            migrationBuilder.DropSchema("trust");
            migrationBuilder.DropSchema("reputation");
            migrationBuilder.DropSchema("messaging");
            migrationBuilder.DropSchema("requests");
            migrationBuilder.DropSchema("identity");
            migrationBuilder.DropSchema("catalog");
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS postgis RESTRICT;");
        }
    }
}
