using Knock.Api.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Knock.Api.Modules.Catalog.Data;

internal sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    /// <summary>UUID v7 generado en PostgreSQL 16 (sin <c>uuidv7()</c>) para las filas que insertan las semillas SQL.</summary>
    private const string _uuidV7Sql =
        "encode(set_bit(set_bit(overlay(uuid_send(gen_random_uuid()) placing "
        + "substring(int8send(floor(extract(epoch from clock_timestamp()) * 1000)::bigint) from 3) "
        + "from 1 for 6), 52, 1), 53, 1), 'hex')::uuid";

    public DbSet<Oficio> Oficios => Set<Oficio>();

    public DbSet<Colonia> Colonias => Set<Colonia>();

    /// <summary>
    /// <c>lower(text)</c> de PostgreSQL para consultas LINQ: <c>string.ToLower</c> lo bloquean CA1304/CA1308 y depende de la cultura.
    /// Solo se traduce a SQL; llamarlo en memoria lanza <see cref="NotSupportedException"/>.
    /// </summary>
    [DbFunction("lower", IsBuiltIn = true)]
    public static string Lower(string value) => throw new NotSupportedException($"{nameof(Lower)}({value}) only translates to SQL.");

    /// <summary>Configuración compartida por el registro del módulo y la fábrica de diseño de EF.</summary>
    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, postgres => postgres.UseNetTopologySuite())
            .UseSnakeCaseNamingConvention();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<Oficio>(oficio =>
        {
            oficio.ToTable("oficios", table =>
                table.HasCheckConstraint("ck_oficios_modalidad", "modalidad IN ('domicilio', 'taller')"));
            oficio.Property(o => o.Id).HasDefaultValueSql(_uuidV7Sql);
            oficio.Property(o => o.Clave).HasMaxLength(16);
            oficio.Property(o => o.Nombre).HasMaxLength(100);
            oficio.Property(o => o.Modalidad).HasMaxLength(16);
            oficio.HasIndex(o => o.Clave).IsUnique();
        });

        modelBuilder.Entity<Colonia>(colonia =>
        {
            colonia.ToTable("colonias");
            colonia.Property(c => c.Id).HasDefaultValueSql(_uuidV7Sql);
            colonia.Property(c => c.Nombre).HasMaxLength(150);
            colonia.Property(c => c.Cp).HasMaxLength(5);
            colonia.Property(c => c.Municipio).HasMaxLength(100);
            colonia.Property(c => c.Geom).HasColumnType("geography (MultiPolygon, 4326)");
            colonia.Property(c => c.Centro).HasColumnType("geography (Point, 4326)");
            colonia.HasIndex(c => c.Geom).HasMethod("gist");
            colonia.HasIndex(c => c.Centro).HasMethod("gist");
            colonia.HasIndex(c => c.Cp);
            colonia.HasIndex(c => new { c.Municipio, c.Cp, c.Nombre }).IsUnique();
        });
    }
}
