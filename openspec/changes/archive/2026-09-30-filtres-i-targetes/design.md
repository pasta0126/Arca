## Context

Ver `proposal.md` (Why). Punto de partida, tras `ui-llistats-i-detall`:

- `ScreenFilterRouter` abre una sección con un `ScreenFilterRequest` (sección, pantalla y filtros por nombre y valor). Hoy entienden filtros `Status` y `Zone` (Taquillas) y `Locker` y `Payment` (Alumnos).
- Los filtros de las listas se aplican **en memoria sobre filas ya cargadas** (las filas de `ListLockerRows` y de `ListStudentRows` llevan estado, zona, nivel, grupo, taquilla y marca de deuda). No existe una definición compartida de «qué filas cumplen un filtro» fuera de los modelos de interfaz.
- Inicio es `StartHomeModel`/`StartHomeScreen` con un resumen fijo (`GetHomeSummaryHandler`) y siete enlaces escritos a mano.
- El patrón de persistencia es el de `CentreIdentity`: entidad de dominio, repositorio en Application, configuración y repositorio EF en Infrastructure, migración en `Storage/Migrations`. Los textos de interfaz salen del localizador, no de las migraciones.

## Goals / Non-Goals

**Goals:**
- Que el filtro de una tarjeta sea el mismo objeto para contar, para abrir la pantalla y para guardarse, sin que la regla «qué cumple el filtro» se escriba dos veces.
- Crear tarjetas con pocos gestos desde donde ya se filtra.
- Recuentos baratos: una lectura por tipo de elemento, no una por tarjeta.

**Non-Goals:**
- Un lenguaje de consultas libre: los criterios son un conjunto cerrado y conocido.
- Mover toda la lógica de filtrado de las listas a Application; solo la parte que las tarjetas necesitan.

## Decisions

**D1. La tarjeta guarda criterios con nombre, los mismos del enrutador.** `HomeCard` = identificador, título, destino (`LockerMap`, `Lockers` o `Students`), criterios y posición. Los criterios son un diccionario nombre→valor (`Status`, `Zone`, `Locker`, `Payment`, `Level`, `Group`, `IncludeRetired`) que se guarda como texto JSON en una columna. *Alternativa*: una columna por criterio; se descarta porque cada criterio nuevo exigiría una migración y las tarjetas de otras secciones tendrán otros criterios.

**D2. Una regla única de «qué cumple el filtro», en Application.** Se crean `LockerCardFilter` y `StudentCardFilter` con un método `Matches(fila)` (puro, sin E/S), y los usan a la vez el recuento de las tarjetas y los modelos de las listas al aplicar una petición, de modo que el recuento de una tarjeta y lo que muestra la pantalla no pueden divergir (escenario «Recuento coherente con la pantalla»). *Alternativa*: dejar el filtrado en los modelos y que el recuento lo reimplemente; se descarta porque es justo la duplicación que rompe la coherencia.

**D3. Recuentos con dos lecturas.** `GetHomeCardsHandler` lee una vez las filas de taquillas y una vez las de alumnos (las mismas consultas por lotes de las listas) y evalúa todas las tarjetas en memoria. Sin curso activo no lee alumnos y esas tarjetas llevan «sin recuento». Sustituye a `GetHomeSummaryHandler`.

**D4. Persistencia: dos tablas nuevas y una migración.** `HomeCards` (una fila por tarjeta) y `HomeCardsState` (una fila con la fecha en que se crearon las de serie). La migración solo crea las tablas; **no inserta tarjetas**, porque sus títulos son texto en catalán que sale del localizador. Un caso de uso `EnsureDefaultCardsHandler` se ejecuta al abrir Inicio y crea las de serie si la marca no existe; es idempotente y la marca impide que reaparezcan tras borrarlas. El mismo caso de uso, con la marca ignorada, sirve a Restaurar las de serie, que añade solo las que no estén (se reconocen por una clave estable `SeedKey` guardada en la tarjeta). Las bases de demostración llaman al mismo caso de uso.

**D5. Límites en el dominio.** Título obligatorio y de hasta 60 caracteres; máximo 24 tarjetas; criterios válidos según el destino. `HomeCard.Create` devuelve un `Result` con errores de dominio como el resto, que la interfaz muestra en el campo.

**D6. Criterios obsoletos.** `ResolveHomeCardHandler` comprueba zona, nivel y grupo contra las zonas y el catálogo vigentes y devuelve la tarjeta con los criterios válidos y la lista de los ignorados. La tarjeta lo muestra como «filtre obsolet» y abrirla usa solo los válidos y avisa de los ignorados. No se modifica ni se borra nada automáticamente.

**D7. Crear desde una pantalla filtrada.** Las pantallas ya tienen el estado de filtros en sus modelos (`StatusFilter`, `ZoneFilter`, etc.). Se añade a cada modelo `CurrentCardCriteria` (lo inverso de `ApplyRequest`), y la acción común `Desa com a targeta` se construye con un `AppAction` disponible solo si hay criterios. Mapa y lista de Taquillas comparten modelo, así que dan los mismos criterios. El formulario usa el `FormViewModel` existente (título y nota con el resumen y el aviso de no poner nombres).

**D8. Crear desde Inicio.** El formulario de Nova targeta reutiliza el de la edición: destino y filtros como listas desplegables (opciones de estado, zonas, niveles y grupos que ya exponen los modelos), y una línea de vista previa del recuento calculada con las mismas reglas de D2 sobre las filas ya cargadas. Como ese formulario es de la sección Inicio y necesita las opciones de otras dos, las opciones las sirve un `GetCardOptionsHandler` de Application (zonas, niveles, grupos), no los modelos de las otras pantallas.

**D9. Orden sin arrastrar.** La posición es un entero denso; mover antes/después intercambia con la vecina en una sola transacción. Se hace con botones y atajos de teclado; arrastrar queda fuera de alcance.

**D10. Inicio como pieza sustituible.** `IHomeScreen` no cambia: `StartHomeScreen` se reescribe como panel de tarjetas y `StartHomeModel` pasa a leer las tarjetas y sus recuentos. `ScreenFilterRouter` se mantiene y crece con los criterios `Level`, `Group` e `IncludeRetired` (Alumnos) y `Screen` ya admitido.

**UX transversal:** cada operación confirma con notificación, los errores son comprensibles y persistentes, la doble ejecución se protege con `OneAtATime`/`RunOnceCommand`, el recuento muestra un indicador de carga sin bloquear, los estados vacíos (sin tarjetas, sin curso, sin configurar) tienen guía, todo se opera con teclado y el estado de una tarjeta se dice con texto.

## Risks / Trade-offs

- [La regla única obliga a tocar los modelos de las listas] → se hace detrás de pruebas existentes (los filtros de Alumnos y Taquillas ya tienen pruebas por escenario) y se añade una prueba de coherencia recuento-pantalla por cada tarjeta de serie.
- [Un título con el nombre de un alumno] → aviso en el formulario y nada se envía fuera del equipo; no se puede comprobar automáticamente y se acepta.
- [Las tarjetas de serie no se traducen si se cambia de idioma más adelante] → los títulos de serie se guardan en el idioma de la creación; al añadir idiomas se podrá regenerar con Restaurar. Se anota en `docs/riesgos.md`.
- [Migración sobre bases existentes] → solo crea tablas; prueba de migración desde el esquema anterior con datos.
- [Muchas tarjetas ralentizan Inicio] → el recuento es de dos lecturas y evaluación en memoria; el límite de 24 acota el coste.

## Migration Plan

1. Migración EF con `HomeCards` y `HomeCardsState`, aplicada por el migrador de esquema existente al abrir la base.
2. Al abrir Inicio, `EnsureDefaultCardsHandler` crea las de serie una sola vez por centro.
3. Retroceso: las tablas nuevas no las usa ningún otro código; una versión anterior las ignora.

## Open Questions

- ¿Conviene más adelante una tarjeta de «taquillas libres por zona» que agrupe por zona en Inicio (una tarjeta por zona)? Hoy se cubre con el filtro de zona en una tarjeta propia; se puede revisar con los conserjes sin cambiar el modelo.
