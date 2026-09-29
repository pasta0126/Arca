## 1. Marco de navegación

- [x] 1.1 Registro de secciones con identificador, título, icono, orden, pantalla raíz e indicador de atención, y prueba de que ninguna pantalla está en dos secciones (D1)
- [x] 1.2 Ventana principal con barra lateral colapsable, cabecera y zona de contenido, con estado de la barra recordado en ajustes locales
- [x] 1.3 Navegación con teclado por la barra y patrón común de pantalla con título, acciones, lista, filtros y detalle
- [x] 1.4 Reparto de las pantallas de dominio en las ocho secciones según el spec
- [x] 1.5 Pruebas: cambio de sección, colapsar y recordar, teclado y estados vacíos por sección

## 2. Estado global y avisos

- [x] 2.1 Servicio de estado global con curso activo o en cierre, versión nueva disponible, asistente y recuentos de atención, y su publicación de cambios (D3) (hito 1: curso activo y cargos pendientes)
- [x] 2.2 Cabecera con nombre, logo y curso
- [x] 2.3 Avisos globales no bloqueantes con acción directa, descartables hasta el siguiente arranque salvo los que bloquean acciones (hito 1: solo «sin curso activo», cuya acción lleva a la sección Curso porque el asistente queda para el hito 2)
- [x] 2.4 Indicadores de sección con recuentos que se refrescan tras cada operación de escritura
- [x] 2.5 Pruebas: curso activo, en cierre, sin curso, aviso de versión nueva y recuentos con datos de ejemplo

## 3. Búsqueda global

- [x] 3.1 Caso de uso de búsqueda en Application con grupos limitados, totales, comparación centralizada y ámbito de curso y bajas (D4)
- [x] 3.2 Objetos de transferencia de resultados sin correo ni identificador, con estado de pago, taquilla y llave
- [x] 3.3 Cuadro de búsqueda siempre visible con atajo, espera corta, cancelación de la búsqueda anterior y navegación por teclado
- [x] 3.4 Abrir la ficha del resultado en su sección y resaltar la taquilla en el mapa (`SearchNavigator` abre la sección y deja la petición y la taquilla resaltada para que las pantallas las recojan; las pantallas de ficha y el mapa llegan con `pantalles-de-domini` y el grupo 5)
- [x] 3.5 Pruebas: acentos y mayúsculas, alumno, taquilla y grupo, sin resultados, muchos resultados, bajas, curso en cierre y escritura rápida

## 4. Identidad y tema

- [ ] 4.1 Configuración del centro con nombre, logo y color en la base de datos y su caso de uso de guardado (D7)
- [ ] 4.2 Validación del logo por firma de fichero, tamaño de hasta 1 MB y decodificación, con rechazo sin cambiar el actual
- [ ] 4.3 Función pura de contraste que ajusta el acento y el texto para claro y oscuro (D8)
- [ ] 4.4 Tema claro (pastel, por defecto), oscuro y del sistema como preferencia local aplicada sin reiniciar (el claro pastel por defecto ya existe desde `arquitectura-base`)
- [ ] 4.5 Valores finales de los recursos con nombre de `ux-fonaments` para ambos temas, con estado por icono o texto además del color (D9)
- [ ] 4.6 Vista previa de la identidad antes de aplicar y pantalla de arranque con la identidad de ARCA
- [ ] 4.7 Pruebas: logo válido, formato y tamaño rechazados, imagen dañada, acentos extremos con contraste, tema del sistema que cambia y restauración de la copia con la identidad

## 5. Pantalla de inicio

- [x] 5.1 `IHomeScreen` y su registro como raíz de Inicio (D2)
- [x] 5.2 Consulta agregada del mapa con estado visible, alumno, llave y marca de deuda por lotes (D5)
- [x] 5.3 Mapa por zonas con secciones colapsables, contadores por estado, filtros y resalte desde la búsqueda
- [x] 5.4 Panel de detalle con las acciones del registro, no disponibles con su motivo y sin correo ni identificador (D6) (liberar, cambiar, asignar, reservar, quitar reserva, marcar como averiada y volver a poner en servicio; marcar como averiada una taquilla ocupada, con sus decisiones, queda para más adelante y se indica con su motivo)
- [x] 5.5 Panel de alumnos sin taquilla como origen del arrastre, con alternativa de menú y teclado, y estado sin curso activo
- [x] 5.6 Actualización puntual de una taquilla y de los contadores tras cada cambio (la consulta y el modelo están; se engancha a las operaciones de escritura cuando llegan las acciones del detalle, 5.4 y 5.5)
- [x] 5.7 Pruebas: mapa con 300 taquillas y zonas, estados y marca de deuda, filtros, asignar arrastrando y por menú, sin taquillas, sin curso activo y sustitución del inicio

## 6. Feedback y guía al usuario

- [x] 6.1 Estados vacíos con guía en cada sección y en el mapa
- [x] 6.2 Resultados y errores de identidad y búsqueda mediante las notificaciones comunes (la búsqueda y el mapa; la identidad llega con el grupo 4)
- [ ] 6.3 Protección contra doble ejecución en los guardados de identidad (queda con la identidad, grupo 4, fuera del hito 1; la asignación y las acciones del detalle ya la tienen)
- [x] 6.4 Claves de recurso en catalán para todos los textos
- [x] 6.5 Pruebas de mensajes, estados vacíos y doble ejecución

## 7. Verificación transversal

- [x] 7.1 Prueba de arquitectura: los modelos de vista no referencian Domain ni Infrastructure salvo por Application
- [x] 7.2 Prueba de privacidad: búsqueda, mapa y notificaciones no dejan nombres, correos ni identificadores en el registro técnico
- [x] 7.3 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [x] 7.4 Prueba de extremo a extremo: arranque, búsqueda, abrir una ficha, asignar desde el mapa, cambiar el tema y el acento y restaurar una copia (`StartScreenEndToEndTests`, sobre la base cifrada real: búsqueda, arrastrar, contadores y deuda, ficha y liberar; cambiar el tema y restaurar una copia quedan fuera del hito 1)
- [x] 7.5 Registrar la pantalla principal como decisión abierta pendiente de validar con los conserjes y documentar cómo sustituir el inicio
