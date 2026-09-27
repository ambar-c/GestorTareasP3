# Bitácora — Asignación 1
Proyecto: Gestor de Tareas por Proyecto (C#)
Herramienta: OpenCode + OpenRouter (North Mini Code, free — cohere/north-mini-code:free)

## Entrada 1 — Crear la plantilla de la bitácora
**Fecha:** 25 de septiembre, 2026
**Qué le pedí:** Crear docs/bitacora-asignacion-1.md con una estructura de entradas
(fecha, qué pedí, qué devolvió, qué tomé/descarté) usando placeholders genéricos
como <título breve>, sin contenido real todavía.
**Qué devolvió:** Un archivo con la estructura correcta, pero en vez de dejar los
placeholders vacíos, rellenó dos entradas completas describiendo trabajo de
arquitectura y de entidades como si el agente lo hubiera realizado en esta sesión.
**Qué tomé / qué descarté:** Tomé la estructura general (encabezados, negritas,
separadores). Descarté por completo el contenido de las dos entradas inventadas.

---

## Caso de error detectado y corregido
**Fecha:** 25 de septiembre, 2026
**Qué le pedí:** Una plantilla de bitácora con placeholders vacíos, sin contenido
real.
**Qué devolvió (el error):** El agente fabricó dos entradas de trabajo (diseño de
arquitectura y documentación de entidades) que nunca se le delegaron a él en esta
sesión — ese trabajo se hizo manualmente en el README, fuera de OpenCode. Presentó
contenido ficticio como si fuera un registro real de su propio trabajo.
**Cómo lo detecté:** Comparé lo que pedí (placeholders vacíos) contra lo que
devolvió (narrativa detallada y específica) y noté que no correspondía a ninguna
tarea que yo le hubiera pedido resolver a él directamente.
**Cómo lo corregí:** Eliminé las dos entradas inventadas, agregué instrucciones
explícitas contra la fabricación de contenido en AGENTS.md ("no inventes
contenido"), y reemplacé el registro por la descripción real de esta misma
interacción.

---

## Entrada 2 — Generar plantilla de Pull Request
**Fecha:** 26 de septiembre, 2026
**Qué le pedí:** Crear .github/PULL_REQUEST_TEMPLATE.md con las cuatro secciones
acordadas (Qué cambia, Por qué, Cómo probarlo, Qué NO incluye), vacías, sin
contenido de relleno, y sin tocar ningún otro archivo.
**Qué devolvió:** El archivo exacto solicitado, con las 4 secciones vacías en el
orden correcto. No modificó ningún otro archivo.
**Qué tomé / qué descarté:** Tomé el archivo completo tal cual — no hubo nada que
descartar esta vez.

---

## Entrada 3 — Evaluación del .gitignore del repo de mi compañero
**Fecha:** 26 de septiembre, 2026
**Qué le pedí:** Evaluar si el .gitignore existente cubría lo típico para C#/.NET,
y señalar solo lo que faltara sin reescribir el archivo completo.
**Qué devolvió (el error):** Recomendó 5 líneas nuevas, pero 2 estaban mal:
packages.lock.json (recomendó ignorarlo cuando la práctica estándar de .NET es
comitearlo, para builds reproducibles) y *.Tests.csproj (ignoraría el propio
código fuente del proyecto de pruebas, no un artefacto generado). Una tercera
línea (*.csproj.user) era redundante con *.user, ya presente en el archivo.
**Cómo lo detecté:** Verifiqué cada línea contra la documentación oficial de
NuGet sobre packages y control de versiones antes de aplicar el cambio.
**Cómo lo corregí:** Apliqué solo las 2 líneas correctas (TestResults/,
coverage.*) y descarté las otras 3.

---

## Entrada 4 — Evaluación del README de ejecución del repo de mi compañero
**Fecha:** 26 de septiembre, 2026
**Qué le pedí:** Verificar si el README de mi compañero ya documentaba cómo
ejecutar el proyecto, y si faltaba, completarlo solo con pasos verificados,
sin inventar.
**Qué devolvió:** Detectó que el proyecto no es ejecutable todavía (Program.cs
vacío, sin .csproj ni .sln), y en vez de inventar comandos, ofreció crear el
proyecto .NET base y un "Hola Mundo" para dejarlo funcional.
**Qué tomé / qué descarté:** Rechacé la oferta de crear el .csproj y Program.cs
del agente — hacerlo habría significado construir parte del proyecto de mi
compañero con mi propia herramienta, lo cual viola la regla de que cada quien
construye su proyecto solo. Reemplacé la idea de "README de ejecución" por una
sección honesta de "Estado del proyecto", documentando la realidad actual sin
inventar nada.

---

## Nota final
Dos de los tres PRs recibidos de mi compañero (#2 y #3 en mi repositorio)
resultaron ser el mismo cambio duplicado al .gitignore (fusionados por error
casi simultáneo). Se dejó registro de revisión (comentario de línea + veredicto
Approve) en ambos después de la fusión, documentando la duplicidad.