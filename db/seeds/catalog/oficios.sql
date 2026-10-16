-- Canonical active trades for catalog.oficios.
-- Requires the Catalog module migration to create catalog.oficios with a generated id
-- and a UNIQUE clave constraint.
INSERT INTO catalog.oficios (clave, nombre, modalidad, activo)
VALUES
    ('mec', 'Mecánica', 'taller', TRUE),
    ('plo', 'Plomería', 'domicilio', TRUE),
    ('ele', 'Electricidad', 'domicilio', TRUE)
ON CONFLICT (clave) DO UPDATE
SET nombre = EXCLUDED.nombre,
    modalidad = EXCLUDED.modalidad,
    activo = EXCLUDED.activo;
