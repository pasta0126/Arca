# pantalla-principal Specification

## Purpose
Definir la pantalla de inicio provisional (mapa de taquillas por zona) y el mecanismo que permite sustituirla por otra cuando se valide con los conserjes.

## Requirements

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

#### Scenario: Centro sin configurar
- **WHEN** hay curso activo pero ninguna taquilla ni ningún alumno
- **THEN** Inicio explica qué hacer primero y ofrece abrir las zonas y abrir los alumnos

#### Scenario: Filtro ya puesto
- **WHEN** el usuario pulsa un recuento de Inicio
- **THEN** se abre la sección con ese filtro puesto, sin la búsqueda ni los demás filtros, y se ve como etiqueta que se puede quitar

### Requirement: Pantalla provisional a validar
El sistema SHALL tratar la pantalla de Inicio como una propuesta inicial y SHALL mantenerse en el registro de decisiones abiertas hasta validarla con los conserjes.

#### Scenario: Decisión abierta
- **WHEN** se revisan las decisiones abiertas del proyecto
- **THEN** la pantalla de Inicio consta como propuesta pendiente de validar con los conserjes
