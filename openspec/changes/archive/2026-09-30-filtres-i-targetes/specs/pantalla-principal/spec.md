## ADDED Requirements

### Requirement: Pieza sustituible de Inicio
El sistema SHALL registrar la pantalla de la sección Inicio como una pieza sustituible con una interfaz definida, de modo que cambiarla no exija modificar la navegación, la búsqueda ni las demás secciones.

#### Scenario: Cambiar el inicio
- **WHEN** se registra otra pantalla de inicio
- **THEN** la aplicación la muestra en Inicio sin cambios en el resto del marco

### Requirement: Inicio como panel de tarjetas
El sistema SHALL mostrar en Inicio el curso activo y el panel de tarjetas del centro en su orden, cada una con su título y su recuento y todas abiertas con un clic o con Intro, y las acciones Nova targeta y Restaurar les de sèrie. Inicio nunca SHALL mostrar importes, saldos ni nombres de alumnos.

#### Scenario: Panel de tarjetas
- **WHEN** el usuario abre Inicio con las tarjetas de serie
- **THEN** ve el curso activo y siete tarjetas con su recuento, en su orden

#### Scenario: Abrir una tarjeta
- **WHEN** el usuario pulsa una tarjeta
- **THEN** se abre su pantalla con su filtro puesto, sin la búsqueda ni los demás filtros, y el filtro se ve como etiqueta que se puede quitar

#### Scenario: Sin curso activo
- **WHEN** no hay curso activo
- **THEN** Inicio lo indica y ofrece ir a Curso, y las tarjetas de Taquillas siguen visibles

#### Scenario: Centro sin configurar
- **WHEN** hay curso activo pero ninguna taquilla ni ningún alumno
- **THEN** Inicio explica qué hacer primero y ofrece abrir las zonas y abrir los alumnos, además de las tarjetas con recuento cero

#### Scenario: Sin tarjetas
- **WHEN** el usuario ha borrado todas las tarjetas
- **THEN** Inicio lo explica y ofrece Nova targeta y Restaurar les de sèrie

#### Scenario: Carga
- **WHEN** Inicio lee los recuentos
- **THEN** muestra un indicador de carga en lugar de las tarjetas, nunca un panel vacío, y no se bloquea

## REMOVED Requirements

### Requirement: Inicio como pantalla registrable
**Reason**: Se separa en la pieza sustituible y en el panel de tarjetas, que sustituye al resumen mínimo provisional.
**Migration**: `Pieza sustituible de Inicio` e `Inicio como panel de tarjetas`; el resumen fijo de recuentos pasa a ser las tarjetas de serie (`targetes-d-inici`, Tarjetas de serie).
