# Checklist — 057 Borrado de fotos de verificación (KNOCK-87)

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (lo cambia una persona) y clave KNOCK-87
- [ ] Dudas de §8 (correo, llave en Key Vault, respaldos, formato de casos) resueltas o aceptadas por escrito
- [ ] Prerrequisitos fusionados: spec 025 (almacén cifrado), spec 054 (ejecutor de Identidad), spec 053 (decisión)
- [ ] Cada caso TC-HU-068-01 a 14 tiene prueba con su ID (`_TC_HU_068_KK`)
- [ ] Amenazas de §6 con control y prueba (la de respaldos pendiente de §8-4)
- [ ] Prueba de dos instancias y de decisión contra vencimiento en verde
- [ ] La purga destruye la llave antes de borrar blobs; ninguna otra clase borra blobs de verificación
- [ ] Sin CURP, HMAC, rutas de blob, enlaces ni llave en registros, eventos, tareas ni auditoría
- [ ] Script de `db/` idempotente; `docs/events.md` al día; OpenAPI de Identidad sin cambios
- [ ] Build, formato, Unit, Integration, Security y Database en verde (resultados reales)
- [ ] Cobertura y mutación (70 % del módulo) en metas; sin supresiones sin EXC-NNN
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final y «Uso de IA» en el PR
