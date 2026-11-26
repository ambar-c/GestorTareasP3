# Instrucciones para el agente

## Propósito del proyecto

GestorTareasP3 es un gestor de tareas por proyecto: organiza tareas por proyecto y prioridad, con vista de progreso. Es un proyecto de Programación III. Tiene un Core (control de acceso, gestión de permisos, manejador de documentos, notificaciones y reportes) y un módulo de negocio (proyectos y tareas).

## Lenguaje y framework

- C# con .NET (API mínima en `src/GestorTareas.Api/Program.cs`).
- Entity Framework Core con SQL Server. Cada módulo tiene su propio `DbContext` y su propia tabla de historial de migraciones.
- Proyectos en `src/`: `ControlAcceso`, `GestionPermisos`, `Documentos`, `Negocio`, `Notificaciones`, `Enviador` y `Api`. Las pruebas están en `tests/GestorTareas.Pruebas`.

## Cómo correrlo

Desde la raíz del repositorio, con Docker Desktop encendido:

```powershell
Copy-Item .env.example .env
docker compose up --build
```

La API responde en `http://localhost:5065` (`/salud` devuelve `OK`). El Enviador de correos se ejecuta aparte con `docker compose run --rm enviador`.

## Cómo probarlo

```powershell
dotnet test
```

No necesita Docker ni una base de datos real: usa una base en memoria (InMemory) o las entidades directamente.

## Convenciones

- Responde siempre en español. Nombres de clases, métodos y variables en español, como en el resto del código.
- No inventes contenido: si te piden una plantilla vacía, entrega placeholders reales, no texto ficticio.
- Sigue el estilo del código que ya existe: servicios en cada proyecto de módulo y los endpoints registrados en la API con su política de autorización.
- Toda operación nueva exige sesión válida y usa las políticas que ya existen (`Autenticado`, `Administrador`).
- Los cambios de base de datos van en migraciones nuevas del módulo que corresponda. Nunca edites una migración existente.
- No hagas commits, ramas, tags ni Pull Requests: los hago yo. Tú solo escribes código.

## Qué no debes tocar

- Los archivos `.env` y cualquier archivo de secretos.
- Las migraciones que ya existen (`Migrations/`).
- `docs/bitacora-*.md`, `docs/maquina-de-estados.md` y los tags del repositorio.
- La lógica de Control de acceso, Gestión de permisos, Documentos y la máquina de estados de Tarea, salvo lo mínimo que te pida explícitamente (por ejemplo, llamar al servicio de notificaciones desde donde ocurre el evento).
- No modifiques ni crees archivos fuera de lo pedido explícitamente.

## Secretos

- Nunca escribas credenciales (contraseñas, claves de API, usuario o clave de SMTP) en ningún archivo del repositorio, ni en código, ni en pruebas, ni en documentación.
- Nunca leas, abras ni imprimas el archivo `.env`.
- Toda configuración sensible se lee de variables de entorno. Si necesitas una variable nueva, agrégala a `.env.example` con un valor de ejemplo falso y documéntala en el README.

- Los endpoints no contienen consultas, agregaciones ni reglas de negocio: solo validan la entrada, identifican al usuario y su rol, llaman a un servicio y devuelven el resultado. Cada pieza expone un servicio que se prueba sin levantar la API (RD-02, RD-12).

- Toda dirección de correo se valida con ValidadorCorreo antes de encolarla. Un fallo del canal de correo nunca debe tumbar la operación de negocio ni la notificación interna (RD-07, RF-NOT-08).

- Cada capa valida lo que recibe, aunque la capa anterior ya lo haya validado: la cola de correos comprueba el formato del destinatario con ValidadorCorreo y no confía en quien la llama (RD-07).