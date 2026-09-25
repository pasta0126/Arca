## 1. Modelo de pasos

- [ ] 1.1 Definir `SetupStep` con obligatoriedad, requisitos previos y función pura de estado sobre un resumen de datos, y el catálogo ordenado de seis pasos (D1)
- [ ] 1.2 Consulta única de resumen: curso activo, importes, zonas, taquillas, alumnos, sin carga perezosa
- [ ] 1.3 Reglas de dependencia entre pasos (D6)
- [ ] 1.4 Pruebas de las funciones de estado: cada combinación de datos, curso en cierre sin activo, importes incompletos y taquillas sin zonas

## 2. Marcadores y reanudación

- [ ] 2.1 Marcadores de pasos omitidos y de descarte del asistente (D2)
- [ ] 2.2 Regla de omisión y de descarte con lo obligatorio pendiente rechazado (D3)
- [ ] 2.3 Servicio de decisión de apertura al arrancar y primer paso pendiente (D4)
- [ ] 2.4 Olvidar la omisión cuando los datos cumplen el paso
- [ ] 2.5 Pruebas: retomar tras reiniciar, no reabrir tras descarte, reabrir al aparecer un paso obligatorio nuevo y omisión de un paso opcional

## 3. Primera ejecución

- [ ] 3.1 Detección de ausencia de base de datos en la ruta configurada, distinguiéndola de un fichero ilegible (D5)
- [ ] 3.2 Elección de carpeta con comprobación de escritura y detección de una base existente en ella
- [ ] 3.3 Empezar de cero: crear la contraseña del centro y confirmar la clave de recuperación (`acces-i-xifrat`) y después crear la base con el migrador, sin dejar ficheros a medias ante un fallo
- [ ] 3.4 Restaurar una copia desde la primera ejecución con el asistente de `copies-de-seguretat`, pidiendo la contraseña de la copia
- [ ] 3.5 Paso de registro opcional en la primera ejecución, con las casillas de `registre-i-actualitzacions` desactivadas por defecto
- [ ] 3.6 Pruebas de integración: instalación nueva, carpeta sin permisos, carpeta con base existente, fichero dañado, fallo al crear y restauración en equipo nuevo

## 4. Pasos del asistente

- [ ] 4.1 Curso escolar: propuesta del año académico y fechas, creación y activación con los casos de uso existentes (D7, D8)
- [ ] 4.2 Importes: definición de los tres conceptos con los importes del curso anterior como propuesta
- [ ] 4.3 Zonas: alta reutilizando los casos de uso de zonas
- [ ] 4.4 Taquillas: alta por rangos con vista previa, o individual, exigiendo una zona
- [ ] 4.5 Alumnos: importación con revisión previa y omisión sin fichero
- [ ] 4.6 Pruebas de cada paso reutilizando los errores de sus casos de uso, y de guardado inmediato con cancelación a mitad

## 5. Reapertura y navegación

- [ ] 5.1 Consulta del estado de los pasos para la entrada de ajustes y para el progreso "N de M"
- [ ] 5.2 Navegar entre pasos en cualquier orden, sugerencia del siguiente y estado de terminado
- [ ] 5.3 Pruebas de reapertura, paso omitido que se completa y progreso

## 6. Feedback y guía al usuario

- [ ] 6.1 Devolver resultado estructurado con recuentos en cada paso
- [ ] 6.2 Explicar los bloqueos por dependencia, por obligatoriedad y por descarte
- [ ] 6.3 Protección contra doble ejecución en empezar de cero y en los pasos
- [ ] 6.4 Claves de recurso en catalán para títulos, explicaciones y mensajes
- [ ] 6.5 Pruebas de mensajes, estados de bloqueo y doble ejecución

## 7. Persistencia y verificación transversal

- [ ] 7.1 Entidad, configuración EF Core y migración de los marcadores, verificando que el modelo no tiene cambios sin migrar
- [ ] 7.2 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core ni el sistema de ficheros
- [ ] 7.3 Prueba de privacidad: un fallo durante la importación de alumnos en el asistente no deja datos de alumnos en el registro técnico
- [ ] 7.4 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 7.5 Prueba de extremo a extremo: primera ejecución, empezar de cero, curso, importes, zonas, taquillas, salir, reanudar y alumnos
- [ ] 7.6 Documentar los puntos de enganche con `ui-shell` (dónde se ofrece el asistente y la entrada de ajustes)
