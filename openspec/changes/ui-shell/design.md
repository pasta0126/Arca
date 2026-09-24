## Context

Decimocuarto y último cambio de la fase de especificación. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Compone lo ya definido: casos de uso y consultas de los cambios de dominio, y los componentes, atajos, adaptabilidad y arrastrar y soltar de `ux-fonaments`. La pantalla principal está pendiente de validar con los conserjes (decisión abierta en `openspec/config.yaml`); el usuario ha elegido proponer provisionalmente el mapa de taquillas por zona.

Restricciones: Avalonia con MVVM, ratón y teclado, catalán, tres sistemas operativos, datos de menores visibles solo en pantalla, un solo PC.

## Goals / Non-Goals

**Goals:**
- Un marco de navegación estable que no cambie cuando cambie la pantalla de inicio.
- Búsqueda global rápida, sin datos de identificación de más.
- Identidad y tema con contraste garantizado por construcción.

**Non-Goals:**
- Diseño detallado de cada pantalla de dominio y validación de la pantalla principal.

## Decisions

### D1. Marco con secciones registradas
Cada sección se declara como una definición (identificador, clave de título, icono, orden, pantalla raíz y proveedor de indicador de atención) en un registro. La barra lateral, la cabecera y los atajos de navegación se construyen del registro. Añadir o mover una pantalla es editar una definición. El reparto de secciones está en el spec; una prueba comprueba que ninguna pantalla se registra en dos secciones. *Alternativa descartada*: navegación por pestañas codificada en la vista; obligaría a tocar la ventana principal al añadir una sección.

### D2. Inicio sustituible
`IHomeScreen` es la interfaz de la pantalla de inicio y se registra como la raíz de la sección Inicio. El mapa de taquillas es la implementación por defecto. Cambiarla es registrar otra sin tocar navegación, cabecera ni búsqueda. Como las alternativas descartadas (buscador con resumen, solo buscador) ya son casos de uso de este mismo marco, la validación con los conserjes puede cambiar de propuesta con poco coste.

### D3. Indicadores y avisos del estado global
Un servicio de estado global compone en una sola consulta el curso activo o en cierre, la versión nueva disponible, el estado del asistente de configuración y los recuentos de atención (cargos pendientes, llaves sin resolver, incidencias abiertas y pasos de cierre pendientes), y publica cambios cuando una operación los altera. Cabecera, avisos e indicadores de sección lo consumen. Reutiliza las consultas de los cambios de dominio y no calcula reglas propias. Los recuentos se refrescan tras cada operación de escritura y al arrancar, no por sondeo.

### D4. Búsqueda global
Caso de uso de Application que recibe un texto y devuelve grupos de resultados (alumnos, taquillas y grupos) limitados por tipo, con el total. Usa la comparación de texto centralizada de `arquitectura-base` (sin mayúsculas ni acentos) y las consultas por lotes de alumnos, taquillas y pagos, con objetos de transferencia que no llevan correo ni identificador. En la UI, una espera corta tras la última pulsación y cancelación de la búsqueda anterior. El ámbito por defecto son los alumnos activos del curso activo (o del curso en cierre si no hay activo) y las taquillas activas.

### D5. Mapa por consulta agregada
Una consulta devuelve, para todas las taquillas activas, zona, número, estado visible derivado, alumno asignado, estado de la llave y marca de deuda, en lotes y sin carga perezosa. La actualización tras un cambio usa el resultado estructurado de la operación para refrescar solo esa taquilla y los contadores. Las taquillas se pintan con un control virtualizado por zona. El estado se representa por color semántico e icono, nunca solo por color.

### D6. Panel de detalle y de alumnos sin taquilla
El detalle reutiliza los casos de uso de asignación, liberación, reserva e incidencias por los mismos comandos que las demás pantallas y las acciones del registro de `ux-fonaments`, con no disponibles deshabilitadas y su motivo. El panel de alumnos sin taquilla es la lista de `ux-fonaments` y la fuente del arrastre, con alternativa de menú y teclado.

### D7. Identidad guardada en la base de datos
Nombre, logo (bytes, tipo y tamaño) y color de acento son una fila única de configuración del centro en la base de datos, no en los ajustes locales, para que viajen con las copias. El logo se valida por firma de fichero (PNG o JPEG), se limita a 1 MB y se decodifica al cargarlo para rechazar imágenes dañadas. El arranque muestra la identidad de ARCA porque el splash ocurre antes de abrir la base de datos.

### D8. Contraste por construcción
El acento se convierte en un conjunto de recursos para cada tema: el tono se ajusta hasta cumplir el contraste con el texto que se muestra encima y con la superficie, con un umbral definido como constante (4,5:1 para texto). Una función pura lo calcula y se prueba con colores extremos. El tema (claro, oscuro, del sistema) es una preferencia local del equipo, porque depende del PC y no del centro, y por eso no viaja con la copia.

### D9. Tema como recursos con nombre
Este cambio define los valores finales (por defecto, un tema claro de colores neutros y pastel, decidido por la persona responsable el 2026-09-24) de los recursos que `ux-fonaments` declaró como contrato: superficie, texto, éxito, aviso, error, foco, acento, tipografías y espaciados, para claro y oscuro. Los componentes no cambian.

### D10. Feedback
Resultados estructurados y notificaciones de `ux-fonaments`, estados vacíos con guía en cada sección, vista previa de la identidad antes de aplicar y protección contra doble ejecución en guardados.

## Risks / Trade-offs

- **La pantalla principal puede no gustar a los conserjes** → es una pieza registrada e intercambiable; la búsqueda y la navegación no dependen de ella.
- **El mapa con 300 o más taquillas puede ser lento o denso** → consulta agregada única, control virtualizado por zona, zonas colapsables y vista compacta.
- **La búsqueda muestra nombres y estado de pago de menores en pantalla** → es inherente al trabajo del conserje; no se exporta ni se registra y los resultados no llevan correo ni identificador.
- **Un logo grande o malformado podría estropear la cabecera o la base** → validación de formato, tamaño y decodificación, y límite de 1 MB.
- **Un acento arbitrario puede dar contraste ilegible** → ajuste automático con prueba de casos extremos.
- **El estado global depende de muchas consultas** → un servicio único con actualización por eventos; se prueba con datos de ejemplo de 300 taquillas.
- **Reparto de pantallas discutible (por ejemplo, importes en Curso)** → es una definición del registro; moverla es editar una línea.

## Migration Plan

Una migración de EF Core añade la fila de configuración del centro (nombre, logo y acento), sin datos previos. La preferencia de tema es un ajuste local opcional. Las instalaciones existentes muestran el nombre de la aplicación hasta que se defina el del centro.

## Open Questions

- Validación de la pantalla principal con los conserjes: el mapa es provisional y no altera los specs de navegación, búsqueda ni identidad; solo cambiaría la implementación registrada de Inicio.
- Valores exactos de los recursos de tema para el modo oscuro: detalle de diseño visual sin efecto en los specs. El tema claro pastel por defecto ya está definido en `Arca.UI/Theme/ArcaPalette.cs`.
