## Context

Duodécimo cambio. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. El asistente no tiene reglas de negocio propias: orquesta casos de uso de `taquilles-i-zones`, `alumnes-i-assignacions`, `pagaments`, `copies-de-seguretat` y `llicencies-client`. Ya está decidido en `arquitectura-base` que la ruta de la base de datos tiene un valor por defecto y es configurable y que una base nueva se crea aplicando todas las migraciones; en `copies-de-seguretat`, que se puede restaurar una copia en una instalación nueva; y en `cursos-i-historial`, que el asistente de cierre es un patrón de pasos opcionales con estado derivado.

Restricciones: conserjes no técnicos, sin obligación de tener todos los datos el primer día; un solo PC y una sola instancia; datos de menores.

## Goals / Non-Goals

**Goals:**
- Un modelo de pasos declarativo, verificable sin base de datos ni interfaz.
- Estado de los pasos siempre coherente con los datos, sin duplicarlos.
- Ningún paso puede dejar datos a medias.

**Non-Goals:**
- Reglas de negocio propias, identidad del centro, selección de idioma y pantallas.

## Decisions

### D1. Pasos como definiciones declarativas
`SetupStep`: identificador, clave de recurso del título y de la explicación, obligatoriedad, pasos previos requeridos, y una función de estado que recibe un resumen de datos y devuelve hecho o pendiente. La lista es un catálogo registrado en el arranque, en el orden curso, importes, zonas, taquillas, alumnos y licencia. Añadir un paso es declararlo, no cambiar el asistente. *Alternativa descartada*: un flujo lineal con código por pantalla; impediría saltar y reabrir pasos y duplicaría la lógica de estado.

### D2. Estado derivado y marcadores mínimos
El estado sale de una única consulta de resumen (¿hay curso activo? ¿tiene importes? ¿cuántas zonas, taquillas y alumnos? ¿estado de la licencia?), evaluada por funciones puras. Solo se guardan dos marcadores en la base de datos: los pasos omitidos y el descarte del asistente. Consecuencias: cambios hechos fuera del asistente se reflejan solos y restaurar una copia o abrir una base existente no deja el asistente desfasado. Un paso omitido pasa a hecho cuando los datos lo cumplen y su omisión se olvida.

### D3. Obligatoriedad por regla de estado, no por bloqueo de dominio
Curso activo e importes son obligatorios porque sin ellos las reglas de `alumnes-i-assignacions` y `pagaments` ya rechazan asignar y cobrar. El asistente no añade bloqueos: se ofrece al arrancar mientras falten y no se puede descartar, pero no impide consultar ni usar los ajustes. *Alternativa descartada*: pantalla modal que bloquee la aplicación; el conserje no podría, por ejemplo, restaurar una copia o mirar la configuración de zonas.

### D4. Reanudación
Al arrancar, un servicio decide si abre el asistente: sí si hay algún paso obligatorio pendiente; si no, solo si nunca se ha descartado ni terminado y hay pasos pendientes en la primera ejecución. Se abre en el primer paso pendiente. Un descarte solo se permite con lo obligatorio hecho. Cuando aparece un paso obligatorio pendiente nuevo (por ejemplo, el curso pasa a cierre y no hay curso activo), el asistente vuelve a ofrecerse aunque se hubiera descartado.

### D5. Primera ejecución antes de existir la base
El arranque comprueba la ruta configurada antes de abrir nada. Si no hay fichero, muestra la primera ejecución con el estado guardado solo en los ajustes locales (ruta y, después, la licencia): no puede depender de la base. Empezar de cero invoca el migrador con una base vacía; restaurar invoca el asistente de restauración de `copies-de-seguretat`. En ambos casos, al terminar, el arranque continúa con el flujo normal y D4 decide qué abrir. Un fichero existente pero ilegible sigue el tratamiento de `arquitectura-base` y no se trata como primera ejecución, para no ofrecer crear una base nueva sobre datos que pueden recuperarse.

### D6. Dependencias entre pasos
Importes, taquillas y alumnos dependen de otros: los importes del curso activo, las taquillas de al menos una zona y los alumnos del curso activo. El asistente no impide abrirlos, pero muestra qué falta y enlaza con el paso previo. La regla vive en las definiciones (D1), no en cada pantalla.

### D7. Guardado inmediato y reutilización
Cada paso llama a los casos de uso existentes, que ya son transaccionales y validan, y por tanto guardan por sí mismos. El asistente no acumula cambios para un guardado final: abandonar en cualquier punto es seguro. Las importaciones reutilizan el flujo de análisis y confirmación con su revisión previa.

### D8. Curso siguiente
Los pasos de curso e importes no distinguen entre la primera instalación y el arranque de un curso nuevo: el estado se calcula sobre el curso activo, así que cuando el anterior pasa a cierre y no hay activo, el asistente vuelve a ofrecer crear y activar el siguiente y proponer sus importes. No añade lógica nueva a `cursos-i-historial`.

### D9. Feedback
Resultado con recuentos, explicación del bloqueo de cada paso, confirmaciones heredadas de los casos de uso, estado vacío guiado y protección contra doble ejecución, según los principios de UX transversal.

## Risks / Trade-offs

- **Un conserje omite pasos opcionales y no los encuentra después** → reapertura desde ajustes con el estado de cada paso y enlaces desde las pantallas vacías (que ya guían).
- **Los marcadores de omisión se restauran con una copia antigua** → efecto inocuo, porque el estado real sale de los datos y una omisión solo afecta a cómo se muestra un paso pendiente.
- **Crear una base nueva por error sobre una ruta equivocada** → la primera ejecución muestra la carpeta, pregunta si ya existe una base en ella y explica la consecuencia antes de crear.
- **El asistente ofrece crear un curso cuando el usuario solo quería consultar** → se puede cerrar sin descartar y la aplicación sigue en modo de consulta.
- **Orden fijo poco flexible** → cualquier paso se puede abrir en cualquier orden y solo se avisa de las dependencias.

## Migration Plan

Una migración de EF Core añade la tabla de marcadores (pasos omitidos y descarte). Las instalaciones existentes no tienen omisiones ni descarte, y el asistente calcula sobre sus datos: con lo obligatorio hecho no se abre solo.

## Open Questions

- Propuesta exacta de fechas del curso (inicio en septiembre, fin en junio) y del año académico según la fecha actual: detalle de implementación sin efecto en los specs.
- Si conviene enlazar desde el asistente con una guía de ayuda: se decide en `ui-shell`.
