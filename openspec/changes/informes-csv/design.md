## Context

Noveno cambio. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. Reutiliza las consultas ya definidas: morosos y fianzas por devolver (`pagaments`), llaves pendientes (`claus`), taquillas y su estado (`taquilles-i-zones`, `incidencies`) y asignaciones (`alumnes-i-assignacions`). Este cambio añade solo el resumen de cobros, que ninguno definía.

Restricciones: solo CSV para siempre; cabeceras por clave de recurso; datos de menores; un solo PC.

## Goals / Non-Goals

**Goals:**
- Un único mecanismo de exportación para todos los informes, verificable sin base de datos ni sistema de ficheros.
- Añadir un informe sin tocar el mecanismo.
- Ficheros seguros de abrir en una hoja de cálculo y sin datos de más.

**Non-Goals:**
- Informes no pedidos todavía, columnas configurables, PDF, Excel o automatización.

## Decisions

### D1. Informe como definición declarativa
`ReportDefinition`: identificador, clave de recurso del título y de la descripción, filtros admitidos, columnas (clave de recurso de cabecera, extractor y tipo: texto, fecha, importe, entero, sí/no) y una consulta que devuelve filas de objetos de transferencia. El catálogo es una lista registrada en el arranque. *Alternativa descartada*: un método de exportación por informe; duplicaría el formato y las protecciones seis veces y haría costosa cada ampliación futura.

### D2. Escritor de CSV puro
Servicio de dominio sin acceso a ficheros que recibe columnas y filas y produce el texto: cabeceras por recurso, formato de cultura catalana (fecha corta, importe con coma y sin símbolo, sí/no por recurso), escape de comillas y separadores y neutralización de fórmulas solo en columnas de texto (D3). El BOM y el fin de línea los añade la capa de escritura. Es la pieza que concentra las pruebas.

### D3. Protección contra inyección de fórmulas
Los valores de texto que empiezan por `=`, `+`, `-`, `@`, tabulador o retorno de carro llevan una comilla simple delante. Los importes y fechas se escriben con su tipo y no se tocan, porque se generan en la aplicación y no son texto libre. Los nombres de alumnos vienen de un fichero externo y son la vía realista de ataque.

### D4. Escritura atómica
Se escribe en un fichero temporal junto al destino, se cierra y se mueve sobre el destino. Un error o una cancelación borran el temporal; el destino existente no se toca hasta el movimiento final. Se comprueba el destino antes de empezar (existe la carpeta, hay permisos) para dar el error antes de leer datos.

### D5. Datos mínimos por construcción
Los objetos de transferencia de los informes no tienen campos de correo, identificador, notas ni motivos, igual que los listados de `alumnes-i-assignacions` y `pagaments`, de modo que no se pueden filtrar por descuido. El aviso de datos de menores depende de una propiedad de la definición (contiene alumnos), no de una lista aparte.

### D6. Resumen de cobros
Consulta agregada sobre los cargos: por curso y concepto, recuento e importe en cada estado. Se calcula sobre los cargos, así que sobrevive al anonimizado de `cursos-i-historial`, que solo desvincula al alumno. No incluye datos personales.

### D7. Filas vacías y orden
Sin filas no se crea el fichero. Todas las consultas terminan en un orden total (apellidos, nombre e identidad interna; o zona, número e identidad) para que la misma entrada dé el mismo fichero; el desempate por identidad interna evita variaciones entre homónimos.

### D8. Progreso y cancelación
Las filas se generan y escriben en flujo por bloques, con recuento de filas para el progreso y comprobación de cancelación entre bloques; el volumen (cientos de filas) hace que en la práctica sea rápido, pero se respetan los principios de UX transversal.

### D10. Feedback
Resultado estructurado con número de filas y ruta, confirmación de sobrescritura, aviso de menores y estados vacíos con guía.

## Risks / Trade-offs

- **El fichero exportado sale de la base cifrada y puede acabar en un USB** → mínimo de datos, aviso explícito en cada exportación con datos de alumnos y documentación para la dirección del centro; la custodia es del centro.
- **Una hoja de cálculo puede mostrar mal caracteres o fechas** → BOM, `;` y formatos catalanes; la fecha corta evita ambigüedades de mes y día.
- **Sin columnas configurables, un conserje puede querer otras** → el catálogo declarativo permite añadir informes o columnas sin cambiar el mecanismo; se decidirá tras hablar con ellos.
- **Nombres con comillas o saltos de línea rompen el fichero** → escape estándar y pruebas con valores hostiles.
- **Datos de un curso anonimizado quedan con campos vacíos** → es correcto y esperado; el informe lo trata sin error.

## Migration Plan

Sin migración de base de datos: no hay datos nuevos. El resumen de cobros es una consulta sobre tablas existentes.

## Open Questions

- Qué informes adicionales necesitan los conserjes y con qué columnas: se decide al hablar con ellos; solo exige añadir definiciones al catálogo.
- Carpeta por defecto de las exportaciones (última usada o documentos del usuario): detalle de interfaz sin efecto en los specs.
