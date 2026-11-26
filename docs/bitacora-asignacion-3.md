# Bitácora de delegación — Asignación 3

**Proyecto:** Gestor de Tareas por Proyecto (GestorTareasP3)
**Herramienta:** OpenCode (agente de código), guiada por Claude para planear y verificar
**Fecha:** 25 de noviembre de 2026
**Qué audité:** lo que el agente construyó en la Práctica 5 (notificaciones, cola de correos y reportes), contra mis criterios (`docs/criterios-notificaciones-reportes.md`), los requisitos RF-NOT y RF-REP, los requisitos de diseño RD-02, 03, 06, 07, 08, 10 y 11 y los casos que mis criterios no cubrían.

**Cómo audité.** Partí de `main` con la Práctica 5 fusionada, creé la rama `feature/asignacion-3` y corrí `dotnet test` para tener una línea base (74 pruebas, 0 fallos). Después busqué con `Get-ChildItem` + `Select-String` fechas con otro criterio (RD-11), rutas sin autorización (RD-06), credenciales en el historial (RD-10), referencias entre proyectos (RD-03) y errores que exponen detalles (RD-08). Leí con una función `Ver` el código de los dos reportes, de `ServicioNotificaciones`, de `ColaCorreos` y del procesador del `Enviador`. Lo que descarté, con evidencia: no hay `DateTime.Now` ni `GETDATE` (RD-11); ningún proyecto del Core referencia a Negocio (RD-03); no hay credenciales en el historial (RD-10); solo siete rutas son anónimas y todas lo deben ser (RD-06); los reportes agregan en SQL, filtran por rol y el rango incluye el último día (RF-REP-03, 04 y 05); el marcado de leída rechaza notificaciones ajenas (RF-NOT-05); los reintentos están limitados (RF-NOT-10). Los comandos y sus salidas están en `docs/evidencia-asignacion-3/`.

---

## Hallazgo H1 — La lógica de los reportes vive dentro del endpoint (gravedad: media)

**Qué se pidió:** en la Práctica 5 delegué los dos reportes con los criterios CA-REP-01 a CA-REP-05, que solo comprueban el comportamiento por HTTP. Mi `AGENTS.md` decía que los servicios van en cada proyecto de módulo y los endpoints en la API, pero no prohibía poner lógica en el endpoint. Mi prompt de ajuste del porcentaje apuntaba a `src/GestorTareas.Api/Program.cs` como el lugar del reporte y no pidió separarlo.

**Qué entregó el agente:** los dos reportes completos dentro de `Program.cs` (líneas 1245–1399): `GET /api/reportes/solicitudes-permiso` consulta `ContextoGestionPermisos` y hace el `GroupBy` dentro del manejador, y `GET /api/reportes/progreso-tareas` consulta `NegocioDbContext`. No existía ningún servicio de reportes (`Get-ChildItem .\src -Recurse -Filter *.cs | Where-Object Name -match "Report"` no devolvió nada). Incumple RD-02 (la lógica de negocio no vive en el manejador de la petición) y RD-12 (no se puede probar sin levantar la API). Evidencia: `git show <ba33047~1:src/GestorTareas.Api/Program.cs` y la prueba en rojo `docs/evidencia-asignacion-3/h1-rojo.txt` (`Assert.DoesNotContain() Failure: Sub-string found ".GroupBy("`).

**Por qué pasó:** criterio ausente y instrucción incompleta. Mis criterios verificaban qué devolvía el reporte, no dónde vivía el código, y mi instrucción no decía que la lógica debía ir en un servicio. También fue revisión insuficiente.

**Corrección:** la hizo el agente y yo la verifiqué. Instrucción que le di: crear `ServicioReportesSolicitudes` en GestionPermisos y `ServicioReportesProgreso` en Negocio, moviendo la lógica sin cambiar el JSON, dejar en los endpoints solo la validación del rango, la identidad y el rol, y respetar RD-03. Primero pedí la prueba (rojo) y después la corrección (75 pruebas verdes: `h1-verde.txt`). Revisé que no había otra lógica de reportes duplicada. Commit: `<ba33047` — «fix: mover la lógica de los reportes a servicios (H1)».

**Qué cambio:** agregué CA-REP-06 a `docs/criterios-notificaciones-reportes.md` y una regla a `AGENTS.md` (los endpoints no contienen consultas ni lógica; cada pieza expone un servicio que se prueba sin levantar la API). Desde ahora, al delegar una pieza le digo en qué proyecto y clase debe vivir la lógica.

---

## Hallazgo H2 — Un correo inválido tumba la operación o se encola (gravedad: media)

**Qué se pidió:** CA-NOT-09 (dos canales), CA-NOT-10 (desactivar el correo) y CA-COL-01 (el envío no ocurre dentro de la operación). Ninguno decía qué pasa si el correo del destinatario es inválido. El agente dejó los ganchos para ejecutarse solo tras operaciones exitosas.

**Qué entregó el agente:** `ServicioNotificaciones.NotificarAsync` (líneas 20–53) guarda la notificación y después llama a `colaCorreos.EncolarAsync(correo, ...)` sin proteger esa llamada. Con un destinatario vacío la cola lanza `ArgumentException` después de guardar la notificación, y la operación que la originó (por ejemplo, crear una solicitud, `Program.cs` 342–364, que ya había guardado la solicitud) termina en 500. Con uno mal formado ("abc") lo encola sin quejarse. Incumple RD-07 y el espíritu de RF-NOT-08: un fallo del canal de correo no debe tumbar la operación de negocio. Evidencia: `h2-rojo.txt` (la prueba nueva falla sobre el código anterior).

**Por qué pasó:** criterio ausente. Es uno de los cinco casos que mis criterios no cubrían («un correo con destinatario inválido»). Todos mis criterios usaban usuarios con correos válidos, y yo solo probé esos caminos.

**Corrección:** la hizo el agente con esta instrucción: crear `ValidadorCorreo.EsValido` (no vacío, máximo 254 caracteres y `MailAddress.TryCreate`) y usarlo en `NotificarAsync` antes de encolar, de modo que con un correo inválido se conserve la notificación interna y no se encole nada. Prueba primero (rojo), corrección después (verde: `h2-verde.txt`). Verifiqué que el caso de control con un correo válido sigue encolando. Commit: `<3779514>` — «fix: aislar el canal de correo en NotificarAsync (H2)».

**Qué cambio:** agregué CA-NOT-11 a los criterios y una regla a `AGENTS.md` (toda dirección se valida con `ValidadorCorreo` antes de encolarla, y un fallo del canal de correo nunca tumba la operación). Voy a incluir siempre en mis criterios los casos de borde: vacío, mal formado, fuera de rango.

---

## Hallazgo H3 — La cola de correos acepta destinatarios mal formados (gravedad: baja)

**Qué se pidió:** CA-COL-03 (reintentos limitados) y CA-COL-04 (consulta de la cola). Nada decía que la cola debía rechazar un destinatario con formato inválido.

**Qué entregó el agente:** `ColaCorreos.EncolarAsync` solo valida vacío y longitud. Un "abc" entra a `CorreosEnCola`, y el `Enviador` falla en `new MailAddress(...)` (`Enviador/Program.cs` línea 108) y lo reintenta hasta el máximo (3) aunque ese error no se va a arreglar nunca. Incumple RD-07 y desperdicia intentos de RF-NOT-10. Es la misma regla de H2 repetida en la segunda capa. Evidencia: `h3-rojo.txt`.

**Por qué pasó:** criterio ausente (el mismo caso no cubierto) y revisión insuficiente: al revisar el procesador solo miré los reintentos y los duplicados, no qué entraba a la cola. La dejé como gravedad baja porque no pierde datos ni tumba nada: solo consume tres intentos y termina en `Fallido`.

**Corrección:** la hizo el agente con esta instrucción: usar `ValidadorCorreo.EsValido` en `ColaCorreos.EncolarAsync`, conservar los mensajes de vacío y longitud y agregar uno para el formato inválido. Prueba primero (rojo), corrección después (verde: `h3-verde.txt`). Revisé si alguna prueba existente usaba un destinatario inválido. Commit: `<6ccfc07>` — «fix: validar el formato del destinatario en la cola de correos (H3)».

**Qué cambio:** agregué CA-COL-07 a los criterios y una regla a `AGENTS.md` (cada capa valida lo que recibe, aunque la anterior ya lo haya validado). Al revisar el trabajo de un agente voy a buscar la misma regla repetida en cada capa, no solo donde la encontré primero.

---

## Qué verifiqué en cada corrección

Cada hallazgo tiene una prueba que falla sobre el commit anterior y pasa sobre el corregido. Lo comprobé con un `git worktree` en el padre de cada commit, copiando solo el archivo de prueba: las salidas están en `docs/evidencia-asignacion-3/h1-sobre-codigo-anterior.txt`, `h2-sobre-codigo-anterior.txt` y `h3-sobre-codigo-anterior.txt`. La suite completa se ejecuta con el comando de la sección «Pruebas» del README.