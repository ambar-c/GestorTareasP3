# Criterios de aceptación: notificaciones y reportes

Práctica 5. Estos criterios los escribí antes de delegar al agente. Cada uno se comprueba con la API (`http://localhost:5065`) y las cuentas de prueba: un Administrador (el inicial), dos Estándar (Estándar A y Estándar B). Todas las operaciones exigen sesión válida: sin sesión devuelven `401`.

## Notificaciones

**CA-NOT-01 — Crear una solicitud notifica a los Administradores** (RF-NOT-01)
- Dado: un Estándar con sesión y un Administrador activo.
- Cuando: el Estándar crea una solicitud de permiso.
- Entonces: `GET /api/notificaciones` como Administrador muestra una notificación nueva sin leer sobre esa solicitud; el mismo `GET` como Estándar (cualquiera, incluido el solicitante) no la muestra.

**CA-NOT-02 — Resolver notifica al solicitante** (RF-NOT-02)
- Dado: una solicitud pendiente de Estándar A.
- Cuando: el Administrador la aprueba.
- Entonces: Estándar A tiene una notificación nueva que dice que fue aprobada, y ningún otro usuario la recibe.

**CA-NOT-03 — Un rechazo incluye el motivo** (RF-NOT-02)
- Dado: una solicitud pendiente de Estándar A.
- Cuando: el Administrador la rechaza con el motivo "Falta justificación".
- Entonces: la notificación de Estándar A dice que fue rechazada e incluye el texto "Falta justificación".

**CA-NOT-04 — Asignar un responsable notifica al asignado** (RF-NOT-03)
- Dado: una tarea en estado Pendiente y Estándar B como usuario activo.
- Cuando: el Administrador ejecuta `PUT /api/tareas/{id}/responsable` con el id de Estándar B.
- Entonces: Estándar B tiene una notificación nueva sin leer que menciona el título de la tarea, y ningún otro usuario la recibe. Los pasos para provocarlo están en la sección `Notificaciones` del README.

**CA-NOT-05 — Si la asignación falla, no hay notificación** (RF-NOT-03)
- Dado: una tarea que no existe, o una tarea Completada o Cancelada, o un Estándar que no creó el proyecto.
- Cuando: se intenta asignar un responsable.
- Entonces: la operación devuelve error (`404`, `400` o `403`) y no se crea ninguna notificación.

**CA-NOT-06 — Solo veo mis notificaciones, separadas por leídas** (RF-NOT-04)
- Dado: Estándar A y Estándar B con notificaciones distintas.
- Cuando: Estándar A ejecuta `GET /api/notificaciones`.
- Entonces: solo aparecen las de Estándar A, y cada una indica si está leída. Con `?leida=false` aparecen solo las no leídas.

**CA-NOT-07 — Marcar como leída** (RF-NOT-05)
- Dado: una notificación de Estándar A sin leer.
- Cuando: Estándar A ejecuta `POST /api/notificaciones/{id}/leer`.
- Entonces: responde `200` y la notificación aparece como leída en su listado.

**CA-NOT-08 — No puedo marcar la de otro** (RF-NOT-05)
- Dado: una notificación de Estándar A sin leer.
- Cuando: Estándar B ejecuta `POST /api/notificaciones/{id}/leer` con ese id.
- Entonces: responde `403` y la notificación de Estándar A sigue sin leer.

**CA-NOT-09 — Dos canales** (RF-NOT-06)
- Dado: Estándar A con el correo activado.
- Cuando: se genera una notificación para Estándar A (por ejemplo, CA-NOT-02).
- Entonces: la notificación aparece en su bandeja interna y existe un correo en la cola dirigido al correo de Estándar A con el mismo contenido (`GET /api/correos` como Administrador).

**CA-NOT-10 — Desactivar el correo** (RF-NOT-07)
- Dado: Estándar A con el correo desactivado mediante `PUT /api/notificaciones/preferencias` con `{"correoActivo": false}`.
- Cuando: se genera una notificación para Estándar A.
- Entonces: aparece en su bandeja interna y no se agrega ningún correo nuevo a la cola para ella. Al volver a activarlo, las siguientes notificaciones vuelven a generar correo.

**CA-NOT-11 — Un correo inválido no tumba la notificación** (RF-NOT-06, RD-07) — agregado en la Asignación 3 (H2)
- Dado: un usuario cuyo correo está vacío o mal formado.
- Cuando: se genera una notificación para él.
- Entonces: la operación de negocio termina bien, la notificación aparece en su bandeja interna y no se agrega nada a la cola de correos.

## Cola de correos

**CA-COL-01 — El envío no ocurre dentro de la operación** (RF-NOT-08)
- Dado: el servidor de correo apagado o con un `SMTP_HOST` inválido.
- Cuando: se ejecuta una operación que genera correo (por ejemplo, asignar un responsable).
- Entonces: la operación termina con éxito y el correo queda en la cola con estado `Pendiente`.

**CA-COL-02 — Procesador independiente** (RF-NOT-09)
- Dado: correos en estado `Pendiente`.
- Cuando: se ejecuta el procesador (`docker compose run --rm enviador`).
- Entonces: los toma y los envía sin que la API ni la operación que los creó se ejecuten otra vez.

**CA-COL-03 — Reintentos limitados** (RF-NOT-10)
- Dado: un correo cuyo envío falla siempre.
- Cuando: se ejecuta el procesador 3 veces (el máximo es 3 intentos, declarado en el README).
- Entonces: el correo queda en estado `Fallido` con `intentos = 3`, y una cuarta ejecución no lo vuelve a intentar.

**CA-COL-04 — Consulta de la cola** (RF-NOT-11)
- Dado: correos enviados, pendientes y fallidos.
- Cuando: el Administrador ejecuta `GET /api/correos`.
- Entonces: cada correo muestra estado, intentos, fecha de creación, fecha de envío (si la hay) y el último error (el mensaje del error, no solo su tipo). Un Estándar que ejecuta la misma operación recibe `403`.

**CA-COL-05 — Sin duplicados** (RF-NOT-12)
- Dado: un correo `Pendiente` y el servidor de correo disponible.
- Cuando: se ejecuta el procesador dos veces seguidas.
- Entonces: el correo se envía una sola vez; la segunda ejecución informa 0 enviados y el destinatario recibe un solo mensaje.

**CA-COL-06 — Credenciales fuera del repositorio** (RF-NOT-13)
- Dado: el repositorio clonado.
- Cuando: se busca en los archivos y en todo el historial (`git log -p`) usuario o clave de SMTP reales.
- Entonces: no aparecen. Solo existen en el `.env` local, que está en `.gitignore`; el `.env.example` tiene valores de ejemplo falsos.

**CA-COL-07 — La cola solo acepta destinatarios válidos** (RF-NOT-10, RD-07) — agregado en la Asignación 3 (H3)
- Dado: un destinatario con formato inválido ("abc", "a@", "Ana <ana@correo.com>").
- Cuando: se intenta encolar un correo para él.
- Entonces: la cola lo rechaza con un error controlado y no queda ningún registro. Un correo que nunca puede enviarse no consume intentos del procesador.

## Reportes

**CA-REP-01 — Reporte del Core con valores agrupados** (RF-REP-01, RF-REP-03)
- Dado: 6 solicitudes de permiso en distintos estados y meses.
- Cuando: el Administrador ejecuta `GET /api/reportes/solicitudes-permiso`.
- Entonces: devuelve filas agrupadas por período (mes) y estado, con el total de cada grupo. La cantidad de filas es menor que la cantidad de solicitudes, y la suma de los totales es igual a la cantidad de solicitudes.

**CA-REP-02 — Reporte de negocio que responde una pregunta** (RF-REP-02, RF-REP-03)
- Dado: varios proyectos con tareas en distintos estados y prioridades.
- Cuando: el Administrador ejecuta `GET /api/reportes/progreso-tareas`.
- Entonces: responde "¿cuántas tareas tiene cada proyecto por estado y qué porcentaje está completado?": una fila por proyecto con total de tareas, total por estado y porcentaje de tareas Completadas calculado sobre las tareas no canceladas del proyecto, redondeado a un decimal. Hay menos filas que tareas.

**CA-REP-03 — Los reportes respetan el rol** (RF-REP-04)
- Dado: Estándar A y Estándar B con solicitudes de permiso y proyectos propios.
- Cuando: Estándar A ejecuta ambos reportes.
- Entonces: el reporte de solicitudes solo cuenta las solicitudes de Estándar A, y el de progreso solo incluye los proyectos que Estándar A creó. Ningún total ni nombre de proyecto de Estándar B aparece.

**CA-REP-04 — Filtro por rango de fechas** (RF-REP-05)
- Dado: solicitudes y tareas creadas en dos días distintos, usando `FechaCreacion` de las solicitudes para el reporte de solicitudes y `FechaCreacion` de las tareas para el reporte de progreso.
- Cuando: se ejecuta cada reporte con `?desde=YYYY-MM-DD&hasta=YYYY-MM-DD` para dos rangos distintos que contienen datos diferentes.
- Entonces: el reporte de solicitudes filtra por la fecha de creación de las solicitudes y el reporte de progreso por la fecha de creación de las tareas. Con rango, el reporte de progreso incluye solo proyectos con al menos una tarea dentro del rango y calcula sus conteos usando únicamente esas tareas; sin rango incluye todos los proyectos del alcance, incluso los que no tienen tareas. Los resultados de los dos rangos son distintos. Un rango sin datos devuelve una lista vacía, no un error. Si `desde` es posterior a `hasta`, responde `400`.

**CA-REP-05 — Los reportes no devuelven listados** (RF-REP-03)
- Dado: cualquier conjunto de datos con más de un registro por grupo.
- Cuando: se ejecuta cualquiera de los dos reportes.
- Entonces: la agregación la hace la base de datos (o una función propia del proyecto) y la respuesta no contiene los registros individuales.


**CA-REP-06 — La lógica del reporte no vive en el endpoint** (RD-02, RD-12) — agregado en la Asignación 3 (H1)
- Dado: el código del proyecto.
- Cuando: reviso Program.cs.
- Entonces: los endpoints de reportes solo validan el rango, identifican al usuario, llaman a un servicio y devuelven el resultado. Las consultas y agregaciones están en un servicio que se prueba sin levantar la API. La prueba `H1_Program_no_contiene_consultas_de_reportes` lo comprueba.