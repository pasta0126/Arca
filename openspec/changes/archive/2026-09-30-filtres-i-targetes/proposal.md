## Why

Inicio es hoy un resumen fijo con siete recuentos. Los conserjes necesitan que sea **su** panel: tarjetas que llevan, con un clic, a la información concreta que miran cada día («taquillas disponibles» por zona, «alumnos con pendientes de pago»…), y poder crear las suyas sin pedir cambios al programa. `ui-llistats-i-detall` dejó preparado el mecanismo (`ScreenFilterRouter`: abrir una sección con un filtro ya puesto); falta que el filtro sea un dato que se pueda guardar, mostrar y editar.

## What Changes

- **Inicio pasa a ser un panel de tarjetas.** Cada tarjeta es un filtro guardado: un título, la pantalla a la que lleva (mapa o lista de Taquillas, o Alumnos), los criterios y un recuento en vivo de cuántos elementos cumplen el filtro (siempre recuentos, nunca importes ni saldos).
- **Tarjetas de serie, siempre**: taquillas libres, ocupadas, reservadas, avariadas y en mantenimiento, alumnos sin taquilla y alumnos con pendientes de pago. Se crean al estrenar un centro (y en las bases de demostración), se pueden editar y borrar, y una acción «Restaurar las de serie» devuelve las que falten.
- **Crear una tarjeta es fácil**: desde cualquier pantalla ya filtrada (Taquillas en mapa o lista, Alumnos) con «Desa com a targeta», o desde Inicio con «Nova targeta», que pide la pantalla y los filtros con las mismas listas desplegables.
- **Personalizar**: editar título y filtros, cambiar el orden (botones con teclado, sin depender de arrastrar) y borrar con confirmación.
- **Una tarjeta cuyo filtro ya no existe** (zona, nivel o grupo borrados) lo avisa en la propia tarjeta y al abrirla, en lugar de romperse.
- **Filtros nuevos en las pantallas**: Alumnos entiende también nivel y grupo al abrirse desde una tarjeta (hoy solo taquilla y pago), y Taquillas zona y estado.
- Las tarjetas se guardan en la base del centro: entran en la copia de seguridad y la restauración, y no contienen datos de alumnos, solo criterios.

## Capabilities

### New Capabilities

- `targetes-d-inici`: qué es una tarjeta, sus filtros y recuento, las de serie, crear, editar, ordenar y borrar, los filtros obsoletos, la persistencia y la privacidad.

### Modified Capabilities

- `pantalla-principal`: Inicio como panel de tarjetas en lugar del resumen mínimo provisional; se mantiene la pieza sustituible y los estados sin curso activo y centro sin configurar.
- `navegacio-i-cerca`: la acción común «Desa com a targeta» en las pantallas con filtros.

## Impact

- Código: dominio (`HomeCard`), aplicación (casos de uso de tarjetas y su recuento con las mismas consultas que las listas), persistencia (tabla nueva y migración, marca de «de serie ya creadas»), interfaz (`Inicio`, formulario de tarjeta, acción en las pantallas filtradas, claves i18n en catalán) y datos de demostración. Sustituye `StartHomeModel`/`StartHomeScreen` y su resumen fijo.
- Base de datos: **migración** con dos tablas nuevas; no cambia nada existente.
- **RGPD**: no se guardan datos de alumnos, solo criterios (estado, zona, nivel, grupo, pago) y un título escrito por la persona. Se avisa en el formulario de no escribir nombres de alumnos en el título. Los recuentos se calculan en el equipo.
- Sin dependencias nuevas.

## Fuera de alcance

- Importes, saldos o totales de deuda en las tarjetas o en sus recuentos.
- Tarjetas de otras secciones (llaves, incidencias, informes, curso): llegarán con esos cambios, el modelo no lo impide.
- Tarjetas distintas por usuario o por equipo: hay un solo panel por centro.
- Gráficos, tendencias o históricos de los recuentos.
- Arrastrar tarjetas para ordenarlas (el orden se cambia con botones; arrastrar podrá añadirse después).
- Importar o exportar tarjetas entre centros.
