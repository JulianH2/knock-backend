# Checklist — TT-SEC-01

> Estado: borrador. Casillas sin marcar; no representa aprobación, implementación ni ejecución de pruebas.

- [ ] spec con 8 secciones + Aprobación, estado Aprobada (revisor y fecha) y clave KNOCK-36
- [ ] Dudas 1 a 6 de spec §8 resueltas por el líder y spec actualizada con lo decidido (en especial la 1: referencias de Container Apps o proveedor en la API)
- [ ] La implementación de TT-AZ-02 (Key Vault, identidades y roles) existe antes de los casos 07 y 08
- [ ] Cada caso TC-TT-SEC-01-01 a 04 tiene prueba con su ID (`sdd_lint --trace TT-SEC-01` en OK); 05 a 09 con su salida en el PR
- [ ] Ningún valor del inventario en `appsettings*.json`, `.env.example`, `*.tfvars` ni `*.tf`; `.env` en `.gitignore`
- [ ] Producción sin secretos no arranca y el mensaje nombra la clave sin repetir el valor
- [ ] `docs/SECRETOS.md` con inventario, uso local, rotación y respuesta a una filtración, y enlaces desde README y `CONTRIBUTING.md`
- [ ] gitleaks sobre muestra (reglas propias y marcadores) y sobre todo el historial, con hallazgos resueltos (rotados o registrados con huella)
- [ ] API desplegada con secretos desde Key Vault con identidad administrada: referencias sin valores, `/health/ready` correcto y logs de arranque sin secretos (persona)
- [ ] Ensayo de rotación (JWT y versión 2 de las llaves del celular) con evidencia sin valores (persona)
- [ ] Ningún secreto pasó por un prompt, un PR o un chat; evidencia con suscripción, tenant y correos ocultos
- [ ] Sin cambios en archivos protegidos ni paquetes nuevos (salvo la opción B evaluada)
- [ ] Amenazas de §6 con control y prueba
- [ ] Build, formato y gobierno en verde con resultado real
- [ ] `security-reviewer` LIBERA · `traceability-clerk` sin drift
- [ ] `Memoria.md` actualizado · informe final en el PR
