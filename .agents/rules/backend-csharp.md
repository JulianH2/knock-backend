---
name: backend-csharp
description: Reglas al editar la API .NET de Knock
paths: ["src/**/*.cs", "src/**/*.csproj"]
---

# Reglas de la API (C#)

- Antes de crear un tipo o método: `python scripts/codemap.py --query "<qué necesitas>"`. Reutiliza o extiende.
- Copia la forma de `src/Knock.Api/Modules/_Template`; un módulo nunca usa Domain/Data/Services de otro (`I<Modulo>Api` o eventos).
- Controllers delgados: sin `try/catch`, sin `DbContext`, `[ProducesResponseType]` por código, autorización por recurso en el servicio.
- Transiciones de estado solo en el dominio; inválida → `DomainException` → 409. Eventos por outbox; plazos con mensajes programados.
- Nada de SQL interpolado, secretos, `Console.WriteLine`, `TODO` sin issue ni supresiones sin `EXC-NNN`.
- Skills: `vertical-slice-api`, `security-core`.
