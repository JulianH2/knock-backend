using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Knock.Api.Modules.Catalog.Migrations
{
    /// <inheritdoc />
    internal partial class CatalogBaseOficiosColonias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "colonias",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "encode(set_bit(set_bit(overlay(uuid_send(gen_random_uuid()) placing substring(int8send(floor(extract(epoch from clock_timestamp()) * 1000)::bigint) from 3) from 1 for 6), 52, 1), 53, 1), 'hex')::uuid"),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    cp = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    municipio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    piloto = table.Column<bool>(type: "boolean", nullable: false),
                    geom = table.Column<MultiPolygon>(type: "geography (MultiPolygon, 4326)", nullable: true),
                    centro = table.Column<Point>(type: "geography (Point, 4326)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_colonias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oficios",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "encode(set_bit(set_bit(overlay(uuid_send(gen_random_uuid()) placing substring(int8send(floor(extract(epoch from clock_timestamp()) * 1000)::bigint) from 3) from 1 for 6), 52, 1), 53, 1), 'hex')::uuid"),
                    clave = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    modalidad = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oficios", x => x.id);
                    table.CheckConstraint("ck_oficios_modalidad", "modalidad IN ('domicilio', 'taller')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_colonias_centro",
                schema: "catalog",
                table: "colonias",
                column: "centro")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_colonias_cp",
                schema: "catalog",
                table: "colonias",
                column: "cp");

            migrationBuilder.CreateIndex(
                name: "ix_colonias_geom",
                schema: "catalog",
                table: "colonias",
                column: "geom")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_colonias_municipio_cp_nombre",
                schema: "catalog",
                table: "colonias",
                columns: new[] { "municipio", "cp", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_oficios_clave",
                schema: "catalog",
                table: "oficios",
                column: "clave",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "colonias",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "oficios",
                schema: "catalog");
        }
    }
}
