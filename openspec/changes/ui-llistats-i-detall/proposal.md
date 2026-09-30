## Why

Las pantallas de lista y detalle son el patrón que más se usa en ARCA y hoy se sienten pesadas: la ficha del alumno esconde los cargos en una pestaña, hay una sección Cobros que duplica lo que ya se hace desde el alumno (ARCA gestiona taquillas, alumnos y su asignación, no contabilidad), los filtros no se pueden limpiar de un golpe, el mapa de taquillas se pega bloque con bloque y Ajustes no hace scroll, con lo que sus últimos bloques son inaccesibles.

## What Changes

- **Patrón común de lista y detalle** más cuidado: recuento visible, botón de reiniciar que limpia el cuadro de búsqueda y todos los filtros, etiqueta del filtro activo, `Esc` que quita la selección y el foco, y estados vacíos con guía.
- **Ficha del alumno sin pestañas**: cabecera con taquilla y estado de pago, los cargos pendientes con su acción de cobrar en la misma fila (o «al corriente»), y bloques colapsables para el historial de pagos, los datos y el historial de actividad.
- **Se elimina la sección Cobros**. Cobrar, marcar exento, condonar, anular, revertir, cobrar reposición y el aviso de deuda al asignar se mantienen, pero se hacen desde la ficha del alumno.
- **Filtro «amb pendents de pagament»** en la lista de Alumnos, con el recuento de alumnos y **sin importes globales ni saldos**.
- **El mapa de taquillas pasa de Inicio a la sección Taquillas** como una vista más (Mapa, Lista, Zonas), con el mismo detalle, los mismos filtros y el panel de alumnos sin taquilla para arrastrar. Se deja espacio entre las zonas del mapa.
- **Inicio queda provisional y mínimo** (resumen del estado y accesos a Taquillas y Alumnos) hasta el cambio `filtres-i-targetes`, que lo convertirá en un panel de tarjetas de filtros guardados.
- **Toda pantalla con contenido más alto que la ventana hace scroll** (corrige Ajustes).
- **BREAKING** (interfaz): desaparece la entrada Cobros de la barra lateral y su indicador de pendientes pasa a Alumnos.

## Capabilities

### New Capabilities

Ninguna.

### Modified Capabilities

- `navegacio-i-cerca`: barra lateral sin Cobros, reparto de pantallas, indicadores y patrón común de pantalla (reinicio, etiqueta de filtro, `Esc`, scroll).
- `pantalles-alumnes-i-assignacions`: filtro de pendientes en la lista y ficha sin pestañas con cargos e historial de pagos.
- `pantalles-cobraments`: se eliminan la organización de la sección y la consulta de morosos; el resto de operaciones sobre cargos queda como está y se hace desde la ficha.
- `pantalla-principal`: el mapa, sus filtros, su detalle, el panel de alumnos sin taquilla y su carga salen de Inicio; Inicio pasa a ser un resumen mínimo provisional.
- `pantalles-taquilles-i-zones`: la sección Taquillas pasa a tener las vistas Mapa, Lista y Zonas, con las reglas del mapa.

## Impact

- Código: `Arca.UI` (Shell, Students, Charges, Lockers, Map, Lists), composición en `Arca.Desktop` (`MainWindow`, `ChargesComposition`, `LockerHomeComposition`), claves i18n en catalán. Sin cambios de dominio ni de base de datos.
- Pruebas de interfaz existentes de Cobros, Inicio y la ficha por pestañas se reescriben.
- **RGPD**: no se añaden datos ni se sacan del equipo. Se mantiene que la lista y el mapa no muestran correo ni identificador, y que el correo solo aparece en los datos de la ficha.
- Specs y contrato: ninguno externo.

## Fuera de alcance

- Importes globales, saldos, totales de deuda o cualquier informe contable.
- El panel de tarjetas de filtros guardados y personalizables de Inicio (cambio `filtres-i-targetes`).
- Cambios en las reglas de cargos, estados o historial de pagos, o en los importes.
- Validar el mapa con conserjes (D7 de `docs/riesgos.md`).
- Rediseño visual de Curso, Ajustes (salvo el scroll) y pantallas aún no construidas.
