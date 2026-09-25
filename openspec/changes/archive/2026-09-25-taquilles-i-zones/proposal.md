## Why

El inventario de taquillas es la base de todo lo demás: las asignaciones, los pagos, las llaves, las incidencias y el mantenimiento cuelgan de ellas. Con más de 300 taquillas repartidas por zonas, hace falta poder darlas de alta con rapidez, mantenerlas ordenadas, conocer su estado de un vistazo y conservar su historial, sin depender aún de los alumnos.

## What Changes

- Zonas o pasillos: crear, renombrar, desactivar y reactivar, sin borrar las que hayan tenido taquillas.
- Taquillas con identidad interna propia y número visible, único solo entre las que no están de baja: una baja y una activa pueden compartir número.
- Estado visible derivado de hechos (baja, avería, ocupación, reserva), con las cinco situaciones: libre, ocupada, averiada, reservada y de baja.
- Alta individual, alta por rangos (con vista previa y sin efectos parciales) sin importación de ficheros: el inventario se carga una vez con el alta por rangos y el individual y después se mantiene a mano.
- Reserva con nota libre opcional y marca de avería. Marcar como averiada una taquilla ocupada exige antes una decisión explícita.
- Baja definitiva de una taquilla, conservando su historial.
- Historial de eventos por taquilla (alta, cambio de número o zona, reserva, avería, baja) de solo añadir.
- Consulta con filtros por zona, estado y número, y contadores por estado.

## Capabilities

### New Capabilities
- `zones`: gestión del catálogo de zonas o pasillos.
- `taquilles`: inventario, número, estados derivados, alta individual y por rangos, reserva, avería, baja, historial y consulta.

### Modified Capabilities

<!-- Ninguna: las specs de arquitectura-base aún no están archivadas y este cambio no altera sus requisitos. -->

## Fuera de alcance

- Alumnos, asignaciones y la ocupación real de una taquilla: cambio `alumnes-i-assignacions`. Aquí la ocupación es un dato que ese cambio aportará.
- Las decisiones de reasignar o liberar al averiarse una taquilla ocupada, que requieren asignaciones. Aquí solo se define la exigencia de decidir y la opción de mantener.
- Reserva asociada a un alumno concreto (en `alumnes-i-assignacions`); aquí la reserva lleva solo una nota opcional.
- Estado de la llave, cobros, incidencias formales y tareas de mantenimiento (cambios propios).
- Pantallas y navegación (`ui-shell`); este cambio entrega las reglas, casos de uso y persistencia, verificables con pruebas.
- Importación de taquillas desde un fichero, en cualquier formato. Decidido el 2026-09-25: la carga inicial la cubren el alta por rangos y el alta individual, y las cargas puntuales son una actuación de desarrollo o mantenimiento sobre la base de un centro. Si algún día hiciera falta, sería ODS con el lector de `alumnes-i-assignacions`, no CSV.
- Deshacer una baja: una taquilla de baja no se reactiva; si fue un error se da de alta otra.

## Impacto

- **Código**: nuevas entidades y casos de uso en Domain y Application; entidades EF Core, configuraciones y una migración en Infrastructure.
- **Datos personales (RGPD)**: ninguno; este cambio no almacena datos de alumnos.
- **Dependencias**: ninguna nueva.
- **Depende de**: `arquitectura-base` (persistencia, migraciones, i18n, comparación de texto y reloj).
