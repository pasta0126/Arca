## 1. Base común de pantallas

- [ ] 1.1 Modelo de vista de lista (consulta, filtros, orden, selección por identidad) y de detalle (elemento, acciones, historial) reutilizables (D1)
- [ ] 1.2 Modelo de formulario con errores por campo a partir de códigos estables, que conserva lo escrito y usa el comando de ejecución única (D3)
- [ ] 1.3 Enlace de acciones del registro a botones, menús y atajos con no disponibles y su motivo desde Application (D2)
- [ ] 1.4 Registro de las pantallas en las secciones Curso, Taquillas, Alumnos y Cobros (D7)
- [ ] 1.5 Prueba de arquitectura: los modelos de vista no dependen de Domain ni de Infrastructure
- [ ] 1.6 Claves de recurso en catalán para todos los textos, con los términos del glosario (D8)

## 2. Consultas de lectura

- [ ] 2.1 Revisar las consultas existentes frente a las listas del cambio y añadir las que falten, sin reglas nuevas (D6)
- [ ] 2.2 Objetos de transferencia de listas sin correo ni identificador, y consulta aparte para el formulario de edición del alumno
- [ ] 2.3 Pruebas de cada consulta con datos de ejemplo y con privacidad

## 3. Sección Curso

- [ ] 3.1 Lista de cursos con estado y curso activo destacado, y estado vacío
- [ ] 3.2 Formulario de nuevo curso con nombre derivado y validación
- [ ] 3.3 Activar y eliminar un curso con confirmación y con no disponibles y su motivo
- [ ] 3.4 Detalle de solo lectura para cursos no activos
- [ ] 3.5 Formulario de importes con propuesta heredada, validación y aviso de efecto, e historial de importes
- [ ] 3.6 Aviso de curso activo sin importes con acceso directo
- [ ] 3.7 Pruebas: crear, activar, eliminar, importes válidos y no válidos, coma decimal, curso sin importes y doble clic

## 4. Sección Taquillas

- [ ] 4.1 Vista de taquillas con lista virtualizada, filtros, contadores, incluir bajas y estados vacíos
- [ ] 4.2 Detalle de taquilla con acciones, historial y no disponibles con su motivo
- [ ] 4.3 Alta individual con número siguiente propuesto y alta por rangos con vista previa, conflictos y progreso
- [ ] 4.4 Editar número y zona, reservar y quitar reserva
- [ ] 4.5 Fuera de servicio con diálogo de decisión si está ocupada, y reparada
- [ ] 4.6 Baja con confirmación
- [ ] 4.7 Vista de zonas con crear, renombrar, desactivar, reactivar y eliminar
- [ ] 4.8 Pruebas: lista y filtros con 300 taquillas, altas, decisión al averiar una ocupada y cancelarla, baja, zonas con restricciones y privacidad

## 5. Sección Alumnos

- [ ] 5.1 Lista con búsqueda, filtros, incluir bajas, recuento y estados vacíos, y aviso sin curso activo
- [ ] 5.2 Formulario de alta manual con catálogo de nivel y grupo, valores nuevos y aviso de posible duplicado
- [ ] 5.3 Ficha con cabecera de estado y pestañas Datos, Taquilla, Cobros e Historial cargadas al abrirlas (D4)
- [ ] 5.4 Edición de datos, baja con motivo y reactivación
- [ ] 5.5 Selectores compartidos de zona y taquilla y de alumno sin taquilla (D5)
- [ ] 5.6 Asignar desde el alumno, desde la taquilla y arrastrando, con avisos e impedimentos, cambiar y liberar
- [ ] 5.7 Pruebas: búsqueda con acentos, alta y duplicado, baja que libera taquilla, asignar por las tres vías, aviso de deuda, sin taquillas libres, sin curso activo y doble clic

## 6. Sección Cobros

- [ ] 6.1 Consulta de pendientes con filtros, totales, desglose y estado vacío positivo
- [ ] 6.2 Cargos de un alumno con resumen de estado, y su pestaña en la ficha reutilizando el mismo modelo (D7)
- [ ] 6.3 Acciones de pagar, exento, condonar, anular, cambiar importe y revertir con formularios y motivos
- [ ] 6.4 Historial del cargo de solo lectura
- [ ] 6.5 Cobrar reposición de llave con confirmación
- [ ] 6.6 Diálogo de aviso de deuda al asignar
- [ ] 6.7 Pruebas: pagar con fecha, fecha futura, motivo obligatorio, revertir, anulado sin acciones, sin cargos, alumno de baja con deuda, privacidad y doble clic

## 7. Feedback y verificación

- [ ] 7.1 Resultados y errores de todas las operaciones mediante las notificaciones comunes y el indicador de trabajo
- [ ] 7.2 Protección contra doble ejecución en todos los guardados y operaciones
- [ ] 7.3 Recorrido manual de las cuatro secciones con los datos de ejemplo en los tres sistemas y anotar los ajustes en los specs (D9)
