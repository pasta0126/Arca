## 1. Marco de navegación

- [ ] 1.1 Registro de secciones con identificador, título, icono, orden, pantalla raíz e indicador de atención, y prueba de que ninguna pantalla está en dos secciones (D1)
- [ ] 1.2 Ventana principal con barra lateral colapsable, cabecera y zona de contenido, con estado de la barra recordado en ajustes locales
- [ ] 1.3 Navegación con teclado por la barra y patrón común de pantalla con título, acciones, lista, filtros y detalle
- [ ] 1.4 Reparto de las pantallas de dominio en las ocho secciones según el spec
- [ ] 1.5 Pruebas: cambio de sección, colapsar y recordar, teclado y estados vacíos por sección

## 2. Estado global y avisos

- [ ] 2.1 Servicio de estado global con curso activo o en cierre, versión nueva disponible, asistente y recuentos de atención, y su publicación de cambios (D3)
- [ ] 2.2 Cabecera con nombre, logo y curso
- [ ] 2.3 Avisos globales no bloqueantes con acción directa, descartables hasta el siguiente arranque salvo los que bloquean acciones
- [ ] 2.4 Indicadores de sección con recuentos que se refrescan tras cada operación de escritura
- [ ] 2.5 Pruebas: curso activo, en cierre, sin curso, aviso de versión nueva y recuentos con datos de ejemplo

## 3. Búsqueda global

- [ ] 3.1 Caso de uso de búsqueda en Application con grupos limitados, totales, comparación centralizada y ámbito de curso y bajas (D4)
- [ ] 3.2 Objetos de transferencia de resultados sin correo ni identificador, con estado de pago, taquilla y llave
- [ ] 3.3 Cuadro de búsqueda siempre visible con atajo, espera corta, cancelación de la búsqueda anterior y navegación por teclado
- [ ] 3.4 Abrir la ficha del resultado en su sección y resaltar la taquilla en el mapa
- [ ] 3.5 Pruebas: acentos y mayúsculas, alumno, taquilla y grupo, sin resultados, muchos resultados, bajas, curso en cierre y escritura rápida

## 4. Identidad y tema

- [ ] 4.1 Configuración del centro con nombre, logo y color en la base de datos y su caso de uso de guardado (D7)
- [ ] 4.2 Validación del logo por firma de fichero, tamaño de hasta 1 MB y decodificación, con rechazo sin cambiar el actual
- [ ] 4.3 Función pura de contraste que ajusta el acento y el texto para claro y oscuro (D8)
- [ ] 4.4 Tema claro, oscuro y del sistema como preferencia local aplicada sin reiniciar
- [ ] 4.5 Valores finales de los recursos con nombre de `ux-fonaments` para ambos temas, con estado por icono o texto además del color (D9)
- [ ] 4.6 Vista previa de la identidad antes de aplicar y pantalla de arranque con la identidad de ARCA
- [ ] 4.7 Pruebas: logo válido, formato y tamaño rechazados, imagen dañada, acentos extremos con contraste, tema del sistema que cambia y restauración de la copia con la identidad

## 5. Pantalla de inicio

- [ ] 5.1 `IHomeScreen` y su registro como raíz de Inicio (D2)
- [ ] 5.2 Consulta agregada del mapa con estado visible, alumno, llave y marca de deuda por lotes (D5)
- [ ] 5.3 Mapa por zonas con secciones colapsables, contadores por estado, filtros y resalte desde la búsqueda
- [ ] 5.4 Panel de detalle con las acciones del registro, no disponibles con su motivo y sin correo ni identificador (D6)
- [ ] 5.5 Panel de alumnos sin taquilla como origen del arrastre, con alternativa de menú y teclado, y estado sin curso activo
- [ ] 5.6 Actualización puntual de una taquilla y de los contadores tras cada cambio
- [ ] 5.7 Pruebas: mapa con 300 taquillas y zonas, estados y marca de deuda, filtros, asignar arrastrando y por menú, sin taquillas, sin curso activo y sustitución del inicio

## 6. Feedback y guía al usuario

- [ ] 6.1 Estados vacíos con guía en cada sección y en el mapa
- [ ] 6.2 Resultados y errores de identidad y búsqueda mediante las notificaciones comunes
- [ ] 6.3 Protección contra doble ejecución en los guardados de identidad
- [ ] 6.4 Claves de recurso en catalán para todos los textos
- [ ] 6.5 Pruebas de mensajes, estados vacíos y doble ejecución

## 7. Verificación transversal

- [ ] 7.1 Prueba de arquitectura: los modelos de vista no referencian Domain ni Infrastructure salvo por Application
- [ ] 7.2 Prueba de privacidad: búsqueda, mapa y notificaciones no dejan nombres, correos ni identificadores en el registro técnico
- [ ] 7.3 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 7.4 Prueba de extremo a extremo: arranque, búsqueda, abrir una ficha, asignar desde el mapa, cambiar el tema y el acento y restaurar una copia
- [ ] 7.5 Registrar la pantalla principal como decisión abierta pendiente de validar con los conserjes y documentar cómo sustituir el inicio
