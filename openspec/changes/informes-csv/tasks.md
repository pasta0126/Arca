## 1. Escritor de CSV

- [ ] 1.1 Servicio puro que produce el CSV a partir de columnas y filas: cabeceras por recurso, separador `;`, comillas y escape (D2)
- [ ] 1.2 Formato de cultura catalana para fechas, importes sin símbolo y sí/no por recurso
- [ ] 1.3 Neutralización de fórmulas en columnas de texto (D3)
- [ ] 1.4 Pruebas: acentos y "l·l", separador, comillas y saltos de línea, valores que empiezan por `=`, `+`, `-`, `@`, tabulador y retorno de carro, importe negativo sin protección, columnas vacías

## 2. Escritura del fichero

- [ ] 2.1 Escritura atómica con fichero temporal y movimiento final, con BOM UTF-8 (D4)
- [ ] 2.2 Comprobación previa del destino: carpeta existente, permisos y confirmación de sobrescritura
- [ ] 2.3 Nombre propuesto con informe y fecha, con el reloj inyectado
- [ ] 2.4 Pruebas de integración: error a mitad sin fichero parcial, cancelación, destino existente intacto tras un fallo y carpeta sin permisos en Windows, Linux y macOS

## 3. Catálogo de informes

- [ ] 3.1 Definir `ReportDefinition` con filtros, columnas tipadas y bandera de contenido de alumnos (D1, D5)
- [ ] 3.2 Registro del catálogo y caso de uso genérico de exportación con recuento previo y filas vacías (D7)
- [ ] 3.3 Orden total determinista con desempate por identidad interna
- [ ] 3.4 Pruebas: informe nuevo declarado sin cambiar el mecanismo, exportaciones repetidas idénticas, sin filas sin fichero

## 4. Informes de v1

- [ ] 4.1 Morosos: una fila por cargo, alumno de baja, sin motivos
- [ ] 4.2 Taquillas libres y averiadas con filtros por zona y estado, sin notas
- [ ] 4.3 Asignaciones con filtros por curso, zona, nivel y grupo, y curso anonimizado con datos de alumno vacíos
- [ ] 4.4 Consulta y informe de resumen de cobros por curso y concepto (D6)
- [ ] 4.5 Fianzas por devolver y llaves pendientes reutilizando sus consultas
- [ ] 4.6 Pruebas de cada informe con datos de ejemplo, filtros, ausencia de correo, identificador, notas y motivos, y resumen de un curso antes y después de anonimizarlo

## 5. Feedback y guía al usuario

- [ ] 5.1 Resultado estructurado con número de filas y ruta del fichero
- [ ] 5.2 Aviso de datos de menores con confirmación, solo en informes con alumnos
- [ ] 5.3 Progreso con recuentos, cancelación y protección contra doble ejecución (D8)
- [ ] 5.4 Estados vacíos con guía: sin datos que exportar, sin cargos, sin morosos
- [ ] 5.5 Claves de recurso en catalán de títulos, descripciones, cabeceras y mensajes
- [ ] 5.6 Pruebas de mensajes de resultado, aviso, estados vacíos y doble ejecución

## 6. Verificación transversal

- [ ] 6.1 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core ni el sistema de ficheros
- [ ] 6.2 Prueba de privacidad: un error provocado durante la exportación no deja nombres ni valores en el registro técnico
- [ ] 6.3 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 6.4 Prueba de extremo a extremo: exportar cada informe con datos de ejemplo y comprobar el fichero
- [ ] 6.5 Documentar cómo añadir un informe nuevo
