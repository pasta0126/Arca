## Purpose

Definir la pantalla de inicio provisional (mapa de taquillas por zona) y el mecanismo que permite sustituirla por otra cuando se valide con los conserjes.

## ADDED Requirements

### Requirement: Inicio como pantalla registrable
El sistema SHALL registrar la pantalla de la sección Inicio como una pieza sustituible con una interfaz definida, de modo que cambiarla no exija modificar la navegación, la búsqueda ni las demás secciones.

#### Scenario: Cambiar el inicio
- **WHEN** se registra otra pantalla de inicio
- **THEN** la aplicación la muestra en Inicio sin cambios en el resto del marco

### Requirement: Mapa de taquillas por zona
El sistema SHALL mostrar en Inicio las taquillas activas agrupadas por zona, cada una con su número y su estado visible, ordenadas por número, con las zonas como secciones colapsables.

#### Scenario: Mapa por zonas
- **WHEN** el usuario abre Inicio
- **THEN** ve cada zona con sus taquillas y el número de taquillas por estado

#### Scenario: Estado visible
- **WHEN** se muestra una taquilla
- **THEN** su estado (libre, ocupada, reservada, averiada o en mantenimiento) se distingue por color y por icono o texto

#### Scenario: Taquilla ocupada con deuda
- **WHEN** una taquilla ocupada tiene un alumno con cargos pendientes
- **THEN** muestra además la marca de deuda

#### Scenario: Zona desactivada
- **WHEN** una zona está desactivada
- **THEN** no aparece en el mapa

#### Scenario: Sin taquillas
- **WHEN** no hay taquillas activas
- **THEN** se muestra un estado vacío que guía a darlas de alta o a la configuración guiada

### Requirement: Filtros del mapa
El sistema SHALL permitir filtrar el mapa por estado y por zona y resaltar las taquillas que coinciden con la búsqueda global, con contadores por estado.

#### Scenario: Filtrar por estado
- **WHEN** el usuario filtra por averiada
- **THEN** el mapa muestra solo las taquillas averiadas de cada zona

#### Scenario: Contadores
- **WHEN** hay 120 taquillas libres y 180 ocupadas
- **THEN** los contadores muestran 120 y 180

#### Scenario: Resaltar una búsqueda
- **WHEN** el usuario elige una taquilla en los resultados de la búsqueda global
- **THEN** el mapa la resalta y se abre su detalle

### Requirement: Detalle de la taquilla seleccionada
El sistema SHALL mostrar al seleccionar una taquilla un panel de detalle con su estado, su alumno y su estado de pago y de llave, sus incidencias abiertas y las acciones aplicables, sin salir de Inicio.

#### Scenario: Taquilla ocupada
- **WHEN** el usuario selecciona una taquilla ocupada
- **THEN** el panel muestra el alumno, su estado de pago y de llave y las acciones de liberar y cambiar

#### Scenario: Taquilla libre
- **WHEN** el usuario selecciona una taquilla libre
- **THEN** el panel ofrece asignar, reservar y marcar como averiada

#### Scenario: Acciones no disponibles
- **WHEN** una acción no procede en el estado de la taquilla
- **THEN** aparece deshabilitada con su motivo

#### Scenario: Correo e identificador
- **WHEN** el panel muestra un alumno
- **THEN** no muestra su correo ni su identificador

### Requirement: Alumnos sin taquilla
El sistema SHALL ofrecer en Inicio un panel con los alumnos activos del curso activo sin taquilla, con búsqueda y recuento, que sirve de origen para asignar arrastrando y también permite asignar desde el menú y el teclado.

#### Scenario: Lista de alumnos
- **WHEN** hay 40 alumnos sin taquilla
- **THEN** el panel los lista con el recuento 40

#### Scenario: Asignar arrastrando
- **WHEN** el usuario arrastra un alumno del panel sobre una taquilla libre
- **THEN** se asigna con las validaciones y avisos habituales y el mapa y el panel se actualizan

#### Scenario: Todos con taquilla
- **WHEN** no queda ningún alumno sin taquilla
- **THEN** el panel lo indica y ofrece ver los alumnos

#### Scenario: Sin curso activo
- **WHEN** no hay curso activo
- **THEN** el panel indica que hay que activar un curso y no permite asignar

### Requirement: Carga y rendimiento del mapa
El sistema SHALL cargar el mapa con una consulta agregada que traiga estado, alumno y marca de deuda de todas las taquillas por lotes, sin consultar una a una, y SHALL mantener la interfaz receptiva con más de 300 taquillas.

#### Scenario: 300 taquillas
- **WHEN** el usuario abre Inicio con 300 taquillas
- **THEN** el mapa aparece con un indicador de carga que no bloquea la interfaz

#### Scenario: Actualización tras un cambio
- **WHEN** se asigna o libera una taquilla
- **THEN** solo se actualiza esa taquilla y los contadores, sin recargar todo el mapa

### Requirement: Pantalla provisional a validar
El sistema SHALL tratar el mapa como la propuesta inicial de pantalla de inicio y SHALL mantenerse en el registro de decisiones abiertas hasta validarla con los conserjes.

#### Scenario: Decisión abierta
- **WHEN** se revisan las decisiones abiertas del proyecto
- **THEN** la pantalla principal consta como propuesta pendiente de validar con los conserjes
