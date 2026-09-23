## Why

Los conserjes necesitan llevarse listados fuera de la aplicación (reclamar pagos, repartir llaves, avisar de taquillas averiadas) y ARCA ya calcula todos esos datos, pero no hay una forma común y segura de sacarlos. Como se manejan datos de menores, la exportación debe ser mínima, uniforme y siempre disponible, y estar preparada para añadir informes cuando se hable con los conserjes.

## What Changes

- Mecanismo común de exportación a CSV: UTF-8 con BOM, separador `;`, cabeceras por clave de recurso, formatos de la cultura catalana, protección contra fórmulas y escritura atómica del fichero.
- Catálogo de informes de v1: morosos, taquillas libres y averiadas, asignaciones por zona o grupo, resumen de cobros, fianzas por devolver y llaves pendientes.
- Cada informe se exporta con los filtros que el usuario ha elegido, con nombre y apellidos y sin correo, identificador ni notas o motivos libres.
- Consulta nueva de resumen de cobros por curso y concepto, sin datos personales.
- Catálogo ampliable: añadir un informe es declarar sus columnas y su consulta, sin tocar el mecanismo de exportación.
- Exportar está siempre disponible, incluso sin licencia.

## Capabilities

### New Capabilities
- `exportacio-csv`: formato del fichero, elección de destino, protección de datos, progreso y errores de la exportación.
- `informes`: catálogo de informes de v1, sus columnas, filtros y resumen de cobros.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Reutiliza las consultas de `pagaments`, `claus`, `taquilles-i-zones` y `alumnes-i-assignacions`. -->

## Fuera de alcance

- PDF, impresión, recibos y Excel: solo CSV, para siempre.
- Informes que aún no se han pedido: se añadirán cuando se hable con los conserjes.
- Elegir columnas o incluir correo e identificador.
- Exportar historiales o incidencias detalladas, y programar o automatizar exportaciones.
- Exportar la base de datos completa o las copias de seguridad (`copies-de-seguretat`).
- Pantallas de informes (`ui-shell`, `ux-fonaments`).

## Impacto

- **Código**: escritor de CSV y definiciones de informes en Application, escritura de ficheros en Infrastructure, consulta nueva de resumen de cobros.
- **Datos personales (RGPD)**: los informes con alumnos llevan nombre y apellidos y estado de pago de menores fuera de la base cifrada. Se limita al mínimo (sin correo, identificador, notas ni motivos), se avisa al exportar y el contenido nunca se registra. La custodia del fichero exportado es responsabilidad del centro.
- **Depende de**: `arquitectura-base`, `taquilles-i-zones`, `alumnes-i-assignacions`, `pagaments`, `claus`, `incidencies` y `cursos-i-historial`.
