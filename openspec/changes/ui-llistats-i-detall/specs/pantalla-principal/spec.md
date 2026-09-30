## MODIFIED Requirements

### Requirement: Inicio como pantalla registrable
El sistema SHALL registrar la pantalla de la sección Inicio como una pieza sustituible con una interfaz definida, de modo que cambiarla no exija modificar la navegación, la búsqueda ni las demás secciones. Hasta que exista el panel de tarjetas, Inicio SHALL mostrar un resumen mínimo con el curso activo, el recuento de taquillas por estado y el de alumnos sin taquilla y con pendientes, cada uno como acceso que abre la sección correspondiente, y nunca importes.

#### Scenario: Cambiar el inicio
- **WHEN** se registra otra pantalla de inicio
- **THEN** la aplicación la muestra en Inicio sin cambios en el resto del marco

#### Scenario: Resumen mínimo
- **WHEN** el usuario abre Inicio
- **THEN** ve el curso activo y los recuentos como accesos, y al pulsar uno se abre Taquillas o Alumnos

#### Scenario: Sin curso activo
- **WHEN** no hay curso activo
- **THEN** Inicio lo indica y ofrece ir a Curso

### Requirement: Pantalla provisional a validar
El sistema SHALL tratar la pantalla de Inicio como una propuesta inicial y SHALL mantenerse en el registro de decisiones abiertas hasta validarla con los conserjes.

#### Scenario: Decisión abierta
- **WHEN** se revisan las decisiones abiertas del proyecto
- **THEN** la pantalla de Inicio consta como propuesta pendiente de validar con los conserjes

## REMOVED Requirements

### Requirement: Mapa de taquillas por zona
**Reason**: El mapa pasa a ser una vista de la sección Taquillas, al mismo nivel que la lista de alumnos es una vista de Alumnos.
**Migration**: `pantalles-taquilles-i-zones`, Vista de mapa de taquillas.

### Requirement: Filtros del mapa
**Reason**: El mapa comparte los filtros de la lista de taquillas en su nueva sección.
**Migration**: `pantalles-taquilles-i-zones`, Vista de mapa de taquillas y Lista de taquillas con filtros y contadores.

### Requirement: Detalle de la taquilla seleccionada
**Reason**: El detalle de la taquilla ya existe en la sección Taquillas y lo usan el mapa y la lista.
**Migration**: `pantalles-taquilles-i-zones`, Detalle de la taquilla.

### Requirement: Alumnos sin taquilla
**Reason**: El panel de origen para asignar arrastrando acompaña al mapa, que cambia de sección.
**Migration**: `pantalles-taquilles-i-zones`, Panel de alumnos sin taquilla en el mapa.

### Requirement: Carga y rendimiento del mapa
**Reason**: Es un requisito del mapa, que cambia de sección.
**Migration**: `pantalles-taquilles-i-zones`, Vista de mapa de taquillas.
