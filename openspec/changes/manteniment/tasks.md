## 1. Selección de taquillas

- [ ] 1.1 Implementar el resolvedor de selección por zona, rango y lista que devuelve identificadores sin duplicados, excluyendo taquillas de baja (D3)
- [ ] 1.2 Validaciones: selección vacía, máximo de 1000, rango invertido
- [ ] 1.3 Códigos de error y claves de recurso en catalán del mantenimiento en bloque
- [ ] 1.4 Pruebas: zona, rango, lista, combinación sin duplicados, taquillas de baja, vacía, excesiva y rango invertido

## 2. Puesta fuera de servicio en bloque

- [ ] 2.1 Validar los datos comunes: tipo, motivo activo obligatorio, nota opcional de hasta 500 caracteres y fecha de inicio no futura
- [ ] 2.2 Análisis previo que produce el plan inmutable con las taquillas a marcar, las omitidas por motivo (baja, ya fuera de servicio) y el recuento de ocupadas (D2)
- [ ] 2.3 Decisión única sobre ocupadas: mantener o liberar, sin reasignar; exigirla cuando hay ocupadas y no pedirla cuando no las hay (D4)
- [ ] 2.4 Confirmación que revalida y aplica en una transacción llamando a la apertura de `incidencies` con los datos comunes, y devolviendo el análisis actualizado si algo cambió
- [ ] 2.5 Liberación en bloque con el contexto de proceso automático para que las llaves entregadas queden pendientes de devolución
- [ ] 2.6 Progreso con recuentos y cancelación solo antes del guardado (D6)
- [ ] 2.7 Pruebas: aplicación correcta con incidencia individual por taquilla, omitidas por cada motivo, todas omitidas, decisión ausente, mantener, liberar con llaves pendientes, fallo a mitad con reversión completa, datos cambiados desde el análisis y cancelación sin efectos

## 3. Reparación en bloque

- [ ] 3.1 Consulta de incidencias abiertas con filtros por tipo, motivo, zona, rango de números y fecha de inicio
- [ ] 3.2 Análisis con selección editable, fecha y nota comunes y marca de incidencias no reparables con esa fecha (D5)
- [ ] 3.3 Confirmación que revalida y repara todas en una transacción llamando a la reparación de `incidencies`
- [ ] 3.4 Progreso con recuentos y cancelación solo antes del guardado
- [ ] 3.5 Pruebas: reparar un grupo, excluir algunas, fecha anterior al inicio, fecha futura, incidencia que cambia de estado, estado visible tras reparar y reparación individual posterior

## 4. Feedback y guía al usuario

- [ ] 4.1 Devolver el resultado estructurado con recuentos: marcadas, omitidas por motivo, alumnos liberados y reparadas (D7)
- [ ] 4.2 Preparar la confirmación con su consecuencia, incluidos los alumnos afectados si se liberan
- [ ] 4.3 Preparar los estados vacíos: selección sin taquillas, nada que marcar, sin incidencias abiertas, filtros sin resultados
- [ ] 4.4 Preparar el aviso de que la nota no debe incluir datos de alumnos
- [ ] 4.5 Claves de recurso en catalán para todos los mensajes
- [ ] 4.6 Pruebas de mensajes de resultado, de confirmaciones y de estados vacíos

## 5. Persistencia y verificación

- [ ] 5.1 Pruebas de integración sobre SQLite cifrado temporal con el máximo de 1000 taquillas: atomicidad al poner fuera de servicio y al reparar, y tiempo de respuesta fluido
- [ ] 5.2 Comprobar que las pruebas pasan en Windows, Linux y macOS con los scripts de verificación (`docs/stack.md`)
- [ ] 5.3 Prueba de arquitectura: `Domain` y `Application` no referencian EF Core, y ninguna operación en bloque usa un camino distinto de los casos de uso de `incidencies`
- [ ] 5.4 Prueba del invariante de `incidencies` tras cualquier secuencia de operaciones en bloque e individuales
- [ ] 5.5 Prueba de privacidad: un error provocado con una nota no deja rastro en el registro técnico
- [ ] 5.6 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 5.7 Prueba de extremo a extremo: poner en mantenimiento 40 taquillas mantener alumnos, reparar 35 en bloque, reparar 1 individualmente y comprobar las 4 restantes abiertas
