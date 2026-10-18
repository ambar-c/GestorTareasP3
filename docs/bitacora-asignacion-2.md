# Bitácora — Asignación 2
Proyecto: Gestor de Tareas por Proyecto (C#)
Herramienta: OpenCode + GPT-5.6 Luna

---

## Entrada 1 — Diagnóstico de la solución antes de empezar
**Fecha:** 18 de octubre, 2026
**Qué le pedí:** Que revisara AGENTS.md y la solución sin escribir nada, y me
explicara cómo está armado ControlAcceso (DbContext, migraciones y registro en
Api), cómo se protegen los endpoints por rol, qué criterio de fechas se usa y
el TargetFramework de los proyectos.
**Qué devolvió:** Una descripción correcta: ContextoControlAcceso con su fábrica
de diseño y cinco migraciones, las políticas Autenticado y Administrador en
Politicas.cs, fechas en UTC con DateTime.UtcNow y net10.0 en todos los
proyectos. No tocó ningún archivo.
**Qué tomé / qué descarté:** Lo tomé completo. Lo usé para confirmar que
GestionPermisos tenía que seguir el mismo patrón (DbContext propio, fechas en
UTC, mismas políticas) antes de pedirle código.

---

## Entrada 2 — Crear el proyecto GestionPermisos
**Fecha:** 18 de octubre, 2026
**Qué le pedí:** Una biblioteca de clases GestionPermisos con el mismo
TargetFramework que ControlAcceso, agregada a GestorTareas.slnx y referenciada
desde Api, sin referencias a Api ni a GestorTareas.Negocio, porque el Core no
puede depender del negocio (RD-03).
**Qué devolvió:** El .csproj con net10.0, la solución actualizada y la
referencia desde Api, sin referencias hacia el negocio.
**Qué tomé / qué descarté:** Lo tomé tal cual. Verifiqué en el .csproj que no
hubiera ninguna ProjectReference hacia Api ni Negocio.

---

## Entrada 3 — Entidad, estados y permisos
**Fecha:** 18 de octubre, 2026
**Qué le pedí:** La entidad SolicitudPermiso con los atributos mínimos del Core,
con aprobador, fecha de resolución y motivo como opcionales, y los estados y los
permisos declarados cada uno en un solo lugar. Por ahora el único permiso es
CrearProyecto. Sin transiciones todavía.
**Qué devolvió:** SolicitudPermiso en Entidades/, el enum EstadoSolicitudPermiso
con los cuatro estados en Estados/ y el enum Permiso con CrearProyecto en
Permisos/.
**Qué tomé / qué descarté:** Lo tomé completo. Revisé que los campos de
resolución fueran anulables y que no agregara lógica de transiciones antes de
tiempo.

---

## Entrada 4 — DbContext y registro en Api
**Fecha:** 18 de octubre, 2026
**Qué le pedí:** El DbContext de GestionPermisos siguiendo el patrón de
ControlAcceso, registrado en Api con ConnectionStrings__Default y fechas en UTC.
La migración la corro yo, así que le pedí solo el comando exacto.
**Qué devolvió:** ContextoGestionPermisos, su fábrica de diseño, las referencias
de EF Core y el registro en Program.cs, más el comando de migración con
--project y --startup-project.
**Qué tomé / qué descarté:** Tomé el código después de comparar las versiones de
los paquetes con las de ControlAcceso (10.0.0 en los dos). Del comando descarté
el --startup-project: mis otros proyectos tienen fábrica de diseño y siempre
corro dotnet ef solo con --project, así que lo dejé igual por consistencia.
Ver Entrada 6.

---

## Entrada 5 — Endpoints para crear y consultar solicitudes
**Fecha:** 18 de octubre, 2026
**Qué le pedí:** Crear solicitud (RF-GP-01), pendientes para el Administrador y
consulta de las propias (RF-GP-02), con la lógica en un servicio y el Id del
usuario sacado de la sesión.
**Qué devolvió:** ServicioSolicitudesPermiso y tres rutas en
/api/solicitudes-permisos. Permitió crear a cualquier usuario autenticado, no
solo a Estándar.
**Qué tomé / qué descarté:** Lo tomé. Revisé que el Id saliera de
ClaimTypes.NameIdentifier y no del cuerpo. Dejé que cualquier autenticado pueda
crear porque RF-GP-07 habla de un Administrador con una solicitud creada por él
mismo, y eso solo es posible si un Admin también puede crearlas.

---

## Entrada 6 — Tabla de historial propia para las migraciones
**Fecha:** 18 de octubre, 2026
**Qué le pedí:** Que ContextoGestionPermisos usara su propia tabla de historial,
__EFMigrationsHistory_GestionPermisos, en Program.cs y en la fábrica de diseño.
**Por qué:** Al revisar Program.cs vi que ContextoNotificaciones tiene su propia
tabla de historial y GestionPermisos no. En la Entrada 4 le pedí seguir el
patrón de ControlAcceso, y debí pedirle el de Notificaciones. El hueco estaba en
mi prompt, no en lo que hizo el agente.
**Qué devolvió:** El cambio en los dos archivos, sin tocar nada más.
**Qué tomé / qué descarté:** Lo tomé y lo verifiqué con Select-String antes de
crear la migración. En SSMS confirmé que la migración quedó registrada en
__EFMigrationsHistory_GestionPermisos.

---

## Caso de error detectado y corregido — Validación del permiso
**Fecha:** 18 de octubre, 2026
**Qué le pedí:** Que al crear una solicitud de permiso, un permiso vacío o que
no existe se rechazara de forma controlada (RD-07).
**Qué devolvió (el error):** Validó con Enum.TryParse y Enum.IsDefined. Con un
nombre inventado sí rechazaba, pero con {"permiso":"0"} la API respondió 201 y
guardó una solicitud de CrearProyecto, porque Enum.TryParse acepta el valor
numérico del enum y el 0 sí existe.
**Cómo lo detecté:** Al revisar el código sospeché que Enum.TryParse podía
aceptar números, y lo probé con curl.exe enviando "0" como permiso. Respondió
201 Created con "permisoSolicitado":0.
**Cómo lo corregí:** Le expliqué el problema con la petición exacta y le pedí
que solo aceptara el nombre de un permiso declarado en el enum, sin importar
mayúsculas, y que rechazara cualquier número con el mismo mensaje. Cambió
únicamente ServicioSolicitudesPermiso.cs: ahora compara el texto contra los
nombres del enum antes de convertirlo. Reinicié la API y repetí la prueba:
{"permiso":"0"} ahora responde 400 "El permiso solicitado no es válido." y
CrearProyecto sigue respondiendo 201.