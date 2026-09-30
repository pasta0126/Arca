## ADDED Requirements

### Requirement: Guardar el filtro como tarjeta
El sistema SHALL ofrecer en las pantallas de lista y de mapa con filtros la acción común Desa com a targeta, en los mismos botones, menús y atajos que las demás acciones de la pantalla, disponible solo con algún filtro activo y deshabilitada con su motivo en otro caso.

#### Scenario: Acción en el mapa y en la lista
- **WHEN** el usuario tiene un filtro activo en el mapa o en la lista de Taquillas
- **THEN** ve la acción Desa com a targeta con los criterios del filtro actual, que son los mismos en ambas vistas

#### Scenario: Acción en Alumnos
- **WHEN** el usuario tiene un filtro activo en Alumnos
- **THEN** ve la acción Desa com a targeta y su formulario resume los criterios

#### Scenario: Reiniciar no borra tarjetas
- **WHEN** el usuario pulsa Reiniciar
- **THEN** se quitan los filtros de la pantalla y ninguna tarjeta cambia
