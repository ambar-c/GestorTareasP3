# Bitácora — Práctica 2
Proyecto: Gestor de Tareas por Proyecto (C#)
Herramienta: OpenCode + GPT-5.6 Luna

## Antes de empezar — Diagnóstico de la variante A
**Fecha:** 23 de octubre, 2026
Mi matrícula termina en 3, así que me toca la variante A de los defectos
publicados. Antes de pedirle nada al agente revisé mis 16 pruebas de la
Asignación 2 contra los seis defectos. Detectaban el 1, el 2, el 3 y el 6, pero
no el 4 (no había ninguna prueba de la consulta del Estándar) ni el 5 (el par
Aprobada → Pendiente no estaba entre las transiciones prohibidas). Además
ninguna prueba decía qué requisito cubría, y faltaban RF-GP-01, RF-GP-02, el
motivo ausente (RF-GP-04), aprobar dos veces (RF-GP-06) y rechazar la propia
solicitud (RF-GP-07).

---

## Entrada 1 — Identificar cada prueba con su RF-GP
**Fecha:** 23 de octubre, 2026
**Qué le pedí:** Que cada prueba de tests/GestorTareas.Pruebas/GestionPermisos
llevara al inicio del nombre el requisito que cubre, con el formato RF_GP_0N_.
Le pasé la lista con el requisito de cada uno de los 9 métodos y le dije que
solo cambiara los nombres, sin tocar el cuerpo de las pruebas ni nada en src.
**Qué devolvió:** Los 9 métodos renombrados, confirmando que no tocó el cuerpo
de las pruebas ni src.
**Qué tomé / qué descarté:** Tomé el cambio completo. Lo verifiqué con
git diff --stat: solo cambiaron los dos archivos de pruebas, 9 líneas fuera y
9 dentro, una por método. dotnet test: 16 de 16.

---

## Entrada 2 — Completar las transiciones prohibidas
**Fecha:** 23 de octubre, 2026
**Qué le pedí:** Que la teoría de transiciones prohibidas cubriera las 13
transiciones fuera de la tabla (con 4 estados hay 16 combinaciones y solo 3
permitidas), agregando los 7 pares que faltaban. Antes de cambiar nada, que me
confirmara que EstadoSolicitudPermiso solo tiene Pendiente, Aprobada, Rechazada
y Aplicada.
**Qué devolvió:** Primero confirmó los 4 estados del enum, y después agregó los
7 InlineData sin tocar los 6 que ya estaban.
**Qué tomé / qué descarté:** Tomé el cambio completo. dotnet test: 23 de 23.
Después ensayé el defecto 5: agregué (Aprobada, Pendiente) a la tabla de
MaquinaEstadosSolicitud, falló el caso (Aprobada, Pendiente) y deshice el
cambio con git restore.

---

## Entrada 3 — Pruebas de crear y consultar (RF-GP-01 y RF-GP-02)
**Fecha:** 23 de octubre, 2026
**Qué le pedí:** Tres pruebas nuevas con el mismo estilo de las demás: que
CrearAsync deja la solicitud en Pendiente; que ListarPropiasAsync, con dos
solicitudes de un usuario y una de otro, devuelve solo las dos suyas; y que
ListarPendientesAsync, con una Pendiente, una Rechazada y una Aplicada,
devuelve solo la Pendiente.
**Qué devolvió:** Las tres pruebas, sin modificar las existentes ni src, y me
dijo que pasaban 26.
**Qué tomé / qué descarté:** Tomé las tres, pero no me quedé con el "26
aprobadas" del agente: corrí dotnet test yo misma, 26 de 26. Después ensayé el
defecto 4: cambié == por != en ListarPropiasAsync, falló la prueba de RF-GP-02
(esperaba 2 solicitudes y recibió 1) y deshice el cambio.

---

## Entrada 4 — Motivo ausente, doble aprobación y rechazo propio
**Fecha:** 23 de octubre, 2026
**Qué le pedí:** Tres pruebas más: rechazar con motivo null (RF-GP-04); que un
Administrador no pueda rechazar su propia solicitud (RF-GP-07); y aprobar dos
veces la misma solicitud, primero un Administrador A y luego otro B, donde la
segunda debe rechazarse y el aprobador debe seguir siendo A (RF-GP-06).
**Qué devolvió:** Las tres pruebas, sin tocar las existentes ni src, y me dijo
que pasaban 29.
**Qué tomé / qué descarté:** Tomé las tres. dotnet test: 29 de 29. Después
ensayé el defecto 3: borré las dos verificaciones de que el Administrador no
sea el solicitante, la de AprobarAsync y la de RechazarAsync. Fallaron las dos
pruebas de RF-GP-07 y deshice el cambio.

---

## Ensayo de los defectos 1, 2 y 6
**Fecha:** 24 de octubre, 2026
Planté cada defecto sin commit, corrí la suite y lo deshice con git restore:
- Defecto 1: en AprobarAsync cambié la condición que le pregunta a la máquina de
  estados por "solicitud.Estado == Rechazada". Mi código no escribe "el estado
  es Pendiente", así que este es el cambio equivalente; no toqué la tabla de
  transiciones porque eso sería otro defecto. Fallaron las dos pruebas de
  aprobar una solicitud ya aplicada (RF-GP-06).
- Defecto 2: agregué (Rechazada, Aplicada) a la tabla. Falló ese caso en las
  transiciones prohibidas.
- Defecto 6: borré la línea que guarda el motivo del rechazo. Falló la prueba
  de RF-GP-04 que verifica el motivo guardado.
Con esto la suite detecta los seis defectos de mi variante.

---

## Nota final
En esta práctica el agente no cometió errores en las cuatro tareas: hizo lo que
le pedí y no tocó src. Aun así verifiqué cada cambio con git diff --stat y
corriendo las pruebas yo misma, porque una prueba que pasa no dice mucho si no
la he visto fallar. Con 16 pruebas en verde mi suite no veía dos de los seis
defectos. El defecto 5 no lo atrapa ninguna prueba del servicio, porque ningún
endpoint intenta pasar de Aprobada a Pendiente; solo se ve probando la máquina
de estados directamente.