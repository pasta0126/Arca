# Convenciones de código

Puntos 4 y 6 (convención) de `docs/preparacion-desarrollo.md`. Aplican a todo el código de ARCA. Las specs dicen *qué* debe hacer el sistema; este documento dice *cómo se escribe* para que los 14 cambios sean coherentes entre sí.

Regla general: **si una spec dice "como en `taquilles-i-zones`", el patrón de referencia es el de este documento.** Si el código necesita romper una convención, se cambia aquí primero y se explica el motivo.

Los ejemplos son ilustrativos: fijan la forma, no la implementación final. Las versiones y paquetes están en `docs/stack.md`; la regla de coste cero también aplica a todo lo que se añada.

## Decisiones tomadas

| Tema | Decisión | Alternativa descartada |
|------|----------|------------------------|
| Carpetas dentro de cada capa | **Por capacidad** (`Lockers`, `Students`...) | Por tipo técnico (`Entities`, `UseCases`...): dispersa una misma capacidad |
| Resultado de un caso de uso | **Tipo propio `Result<T>`**, sin librerías | Librerías de resultados: dependencia extra y contrato de avisos y recuentos a adaptar |
| Códigos de error y claves | **Código por capacidad** (`Lockers.NumberInUse`) con **clave derivada por regla** | Códigos numéricos: ilegibles en el código y con catálogo aparte |
| Nombre de las pruebas | **Descriptivo, un test por escenario del spec**, con etiqueta de trazabilidad | Libre: no se puede comprobar la cobertura de las specs |

## 1. Estructura de solución y carpetas

Proyectos (ya fijados en `arquitectura-base`, D1):

```
src/Arca.Domain
src/Arca.Application
src/Arca.Infrastructure
src/Arca.Desktop            composición e inyección de dependencias
src/Arca.UI                 componentes de ux-fonaments y pantallas (ver ui-shell)
tests/Arca.Domain.Tests
tests/Arca.Application.Tests
tests/Arca.Infrastructure.Tests
tests/Arca.UI.Tests
tests/Arca.Architecture.Tests
tests/Arca.Testing          constructores de datos de ejemplo, reloj falso y utilidades comunes
```

Dentro de cada capa, **una carpeta por capacidad**, con el mismo nombre que su capacidad de OpenSpec, en inglés y en singular o plural natural (`Lockers`, `Zones`, `Students`, `Assignments`, `Charges`, `Keys`, `Incidents`, `SchoolYears`, `Licensing`, `Backups`, `Reports`).

```
src/Arca.Domain/Lockers/
    Locker.cs
    LockerStatus.cs               tipo del estado visible
    LockerStatusCalculator.cs     función pura de estado derivado
    LockerErrors.cs               códigos de error de la capacidad
src/Arca.Application/Lockers/
    CreateLockerRange/
        CreateLockerRangeRequest.cs
        CreateLockerRangePlan.cs
        CreateLockerRangeHandler.cs
    ILockerRepository.cs          puerto de persistencia
src/Arca.Infrastructure/Lockers/
    LockerRepository.cs
    LockerConfiguration.cs        configuración de EF Core
tests/Arca.Application.Tests/Lockers/CreateLockerRangeTests.cs
```

Reglas:
- **Un caso de uso = una carpeta** con su petición, su respuesta o plan y su manejador.
- Lo compartido entre capacidades vive en carpetas `Common` de cada capa y se reduce al mínimo. Antes de mover algo a `Common`, que lo usen al menos tres capacidades.
- Los tipos son `sealed` salvo que exista un motivo para heredar. Los objetos de valor, peticiones, planes y DTOs son `record`.
- Un fichero por tipo público, con el nombre del tipo.

## 2. Estilo general

- **Licencia en cada fichero**: todo fichero de código lleva al inicio `// SPDX-License-Identifier: GPL-3.0-or-later` y `// Copyright (c) 2026 Guillermo Garcia Carballo`, para que la licencia conste sin ambigüedad. Los ficheros de documentación y de especificación siguen cubiertos por el `LICENSE` del repositorio.
- **Idioma**: identificadores, comentarios de código, pruebas y commits en inglés. Specs y documentación en castellano. Textos de interfaz en catalán, siempre por clave.
- **Nombres**: `PascalCase` para tipos y miembros, `camelCase` para parámetros y variables locales, `_camelCase` para campos privados. Los métodos asíncronos terminan en `Async`. Las interfaces empiezan por `I`.
- **Asincronía**: todo el acceso a datos y a ficheros es asíncrono, con `CancellationToken` como último parámetro. Nunca `.Result` ni `.Wait()`. Ningún acceso a datos en el hilo de la interfaz.
- **Nulos**: contexto de nulabilidad activado en todos los proyectos. Un valor ausente es `null` explícito, no una cadena vacía ni un valor centinela.
- **Fechas y dinero** (ya fijado en `arquitectura-base`, D7):
  - Fecha de calendario: `DateOnly`. Instante: `DateTimeOffset` en UTC. Nunca `DateTime`.
  - El reloj se obtiene siempre de `IClock`; nunca `DateTime.Now` ni `DateTimeOffset.UtcNow` en el código de negocio.
  - Importe: el tipo `Money` del dominio, con `decimal` y dos decimales; se guarda como entero en céntimos. Nunca `double` ni `float`.
- **Texto**: toda comparación, orden y búsqueda de texto pasa por `TextComparer`, el componente único de la cultura catalana (sin mayúsculas ni acentos). Nunca `ToLower()` ni `==` sobre texto de usuario.
- **Excepciones**: las reglas de negocio **no lanzan excepciones**; devuelven `Result<T>` con un error. Las excepciones son solo para fallos inesperados (disco, corrupción) y se capturan en un único punto que las registra sin datos y las convierte en un error inesperado.
- **Persistencia**: sin carga perezosa. Toda relación se carga de forma explícita en cada consulta. `Domain` y `Application` no referencian EF Core.
- **Dependencias**: hacia dentro. `Domain` no referencia nada; `Application` solo `Domain`; `Infrastructure` y `Desktop` referencian `Application`. Lo comprueba una prueba de arquitectura.

## 3. Resultado estructurado y errores

Contrato de `arquitectura-base` (D12): todo caso de uso devuelve un resultado con **datos y recuentos**, **avisos** o un **error con código estable**.

```csharp
public sealed record Result<T>(T? Value, IReadOnlyList<Notice> Notices, Error? Error)
{
    public bool IsSuccess => Error is null;
    public static Result<T> Success(T value, params Notice[] notices) => new(value, notices, null);
    // Result<T>, Error, Notice, Money, TextComparer and Cultures live in Arca.Domain/Common; IClock in Arca.Application/Common.
    public static Result<T> Failure(Error error) => new(default, [], error);
}

// Un aviso o un error lleva un código estable y parámetros, nunca texto traducido.
public sealed record Error(string Code, Severity Severity = Severity.Error, IReadOnlyList<object>? Args = null);
public sealed record Notice(string Code, IReadOnlyList<object>? Args = null);
```

El código de error de una capacidad se declara una sola vez:

```csharp
public static class LockerErrors
{
    public static Error NumberInUse(int number) => new("Lockers.NumberInUse", Args: [number]);
    public static readonly Error Retired = new("Lockers.Retired");
}
```

### Códigos y claves de recurso

- **Código**: `Capacidad.NombreEnPascalCase`, en inglés y estable para siempre (`Lockers.NumberInUse`, `SchoolYears.NotActive`, `Keys.NotAvailable`). Una vez publicado no se renombra: se añade uno nuevo.
- **Clave de recurso**: se **deriva por regla**, sin tabla de correspondencia. Para un código `Capacidad.Nombre`, la clave del mensaje es `Capacidad.Error.Nombre`.

| Tipo de texto | Patrón de clave | Ejemplo |
|---------------|-----------------|---------|
| Error de negocio | `Capacidad.Error.Nombre` | `Lockers.Error.NumberInUse` |
| Aviso | `Capacidad.Warning.Nombre` | `Assignments.Warning.PriorDebt` |
| Resultado con recuentos | `Capacidad.Result.Nombre` | `Lockers.Result.RangeCreated` |
| Etiqueta o título de interfaz | `Capacidad.Label.Nombre` | `Lockers.Label.Number` |
| Mensaje de estado vacío | `Capacidad.Empty.Nombre` | `Lockers.Empty.NoLockers` |
| Evento de historial | `History.Capacidad.Tipo` | `History.Locker.NumberChanged` |
| Común a varias capacidades | `Common.Tipo.Nombre` | `Common.Error.Unexpected` |

- Los parámetros del mensaje usan marcadores posicionales (`{0}`, `{1}`) y el orden queda documentado junto a la definición del error.
- Los mensajes dicen **la causa y qué hacer**, sin texto técnico: "El número 15 ya lo usa otra taquilla activa. Elige otro número."

### Catálogo (punto 6)

- El catálogo **es el propio código**: cada capacidad tiene su `XxxErrors.cs`. No hay un documento aparte que mantener.
- Los recursos viven en `src/Arca.Application/Resources/`, un fichero `.resx` por capacidad (`Lockers.resx`). El neutro es el **catalán**; los idiomas nuevos se añaden como `Lockers.es.resx`, `Lockers.en.resx`.
- **Prueba automática obligatoria**: recorre por reflexión todos los códigos declarados y comprueba que cada uno tiene su clave en catalán, y que ninguna clave usada en el código o en las vistas falta (`arquitectura-base`, spec de i18n).
- Plantilla: al implementar un cambio se añade su `XxxErrors.cs` y su `.resx` con los códigos que citan sus specs.
- **Implementación** (`arquitectura-base`, grupo 6): `ILocalizer` y `ResxLocalizer` en `Arca.Application/Localization`. La capacidad (primer segmento de la clave) elige el fichero, y dentro se usa la clave completa como nombre de la entrada (`Storage.Error.Unreadable` en `Storage.resx`). Una clave que no existe muestra la propia clave. La prueba `ResourceCoverageTests` falla si un código declarado en un `XxxErrors` no tiene texto en catalán o si una clave escrita literalmente en `.Get("...")` o `new Notice("...")` no existe.

## 4. Casos de uso

Un manejador por caso de uso, con una sola operación pública:

```csharp
public sealed class ReleaseLockerHandler(ILockerRepository lockers, IClock clock)
{
    public async Task<Result<ReleaseLockerResult>> HandleAsync(
        ReleaseLockerRequest request, CancellationToken ct)
    {
        // 1. Validar y devolver Result.Failure(...) sin excepciones.
        // 2. Ejecutar dentro de una transacción única, con los ganchos invocados dentro.
        // 3. Devolver Result.Success(datos con recuentos, avisos...).
    }
}
```

- **Transacción**: una por caso de uso, abierta en la capa de aplicación mediante un puerto de unidad de trabajo. Los ganchos de otras capacidades (`IAssignmentOpenedHandler`...) se ejecutan **dentro** de esa transacción y solo pueden fallar revirtiéndolo todo.
- **Progreso y cancelación**: las operaciones largas reciben `IProgress<Progress>` (actual, total, cancelable) y el `CancellationToken`, y solo atienden la cancelación **antes** de la fase de guardado indivisible.
- **Sin lógica de interfaz**: los manejadores no saben nada de ventanas, notificaciones ni diálogos. Devuelven datos; la interfaz los traduce.

## 5. Operaciones en dos fases

Patrón para toda operación masiva o con revisión previa (alta por rangos, importaciones, liberación masiva, devolución de llaves y fianzas, condonación, mantenimiento en bloque, anonimizado y borrado).

```csharp
// Fase 1: análisis. No escribe nada.
Result<ReleaseLockersPlan> plan = await handler.AnalyzeAsync(request, progress, ct);

// Fase 2: confirmación. Revalida contra el estado actual y aplica todo en una transacción.
Result<ReleaseLockersResult> result = await handler.ApplyAsync(plan.Value!, ct);
// Si el estado cambió: Failure(PlanOutdated) devolviendo el plan actualizado; no se aplica nada.
```

- El **plan** es un `record` inmutable con los elementos, los bloqueos con su motivo y los recuentos.
- **Análisis y aplicación comparten el mismo código de validación**, para que la vista previa y el resultado real no puedan discrepar.
- La aplicación es **indivisible**: todo o nada. Si un elemento dejó de ser válido, no se aplica ninguno y se devuelve el plan actualizado.
- Se pide confirmación explícita con los recuentos antes de llamar a `ApplyAsync`; lo gestiona la interfaz con `IConfirmationService` (`ux-fonaments`).

## 6. Historial de solo añadir

Cada capacidad con historial (taquillas, alumnos, cargos, llaves, importes) escribe eventos con el mismo esquema:

```csharp
public sealed record HistoryEvent(
    Guid EntityId,                // identidad interna, nunca el número visible
    string Type,                  // código estable, p. ej. "Locker.NumberChanged"
    DateTimeOffset OccurredAtUtc,
    string? BeforeJson,           // valores anterior y nuevo estructurados
    string? AfterJson,
    string? Reason);
```

- **Nunca se guarda texto ya traducido.** El texto se compone al mostrarlo con la clave `History.<Tipo>` y los valores.
- No existen operaciones de edición ni de borrado en el modelo.
- El evento se escribe **en la misma transacción** que el cambio que lo origina.
- El tipo es un código extensible: cada cambio posterior añade los suyos.
- Los motivos y notas libres se tratan como datos sensibles (ver la sección de privacidad).

## 7. Estado derivado como función pura

Cuando un estado se puede deducir de hechos (estado visible de una taquilla, disponibilidad de una llave, estado al corriente de pago, estado de los pasos de un asistente), **no se almacena**: se calcula.

```csharp
public static class LockerStatusCalculator
{
    public static LockerStatus Calculate(LockerFacts facts) =>
        facts.IsRetired ? LockerStatus.Retired
      : facts.IsOutOfService ? LockerStatus.OutOfService
      : facts.HasAssignment ? LockerStatus.Occupied
      : facts.IsReserved ? LockerStatus.Reserved
      : LockerStatus.Free;
}
```

- Función **pura**: sin acceso a datos, reloj ni red. Recibe los hechos y, si hace falta, la fecha.
- Es la **única** implementación: ninguna otra parte del código recalcula el estado.
- Se prueba con una **tabla de casos** que cubre cada combinación relevante.

## 8. Privacidad en el código

Los datos de alumnos son datos de menores. Reglas de obligado cumplimiento:

1. **Objetos de transferencia sin correo ni identificador.** Los listados, búsquedas, informes y exportaciones usan DTO que no tienen esos campos. Solo el DTO de la revisión de dudosos de la importación puede llevarlos. Una prueba de arquitectura busca propiedades llamadas `Email` o `Identifier` en los DTO.
2. **Nada de datos de alumnos en el registro técnico** (implementado en `FileErrorLog`: solo se escribe el tipo de la excepción y de sus causas, el contexto y la pila, **nunca el mensaje ni `Data`**, porque el mensaje puede llevar el dato que se estaba procesando): ni nombres, ni valores de campos, ni consultas con parámetros. Se registran tipos de error, códigos y referencias. Las excepciones propias no incluyen valores de datos en su mensaje.
3. **Los textos libres del usuario** (motivos, notas) no aparecen en listados generales, exportaciones ni registro técnico. Solo en la ficha del elemento.
4. **Ningún dato de alumnos sale del equipo.** La única comunicación de red posible es el registro opcional y el aviso de versión de `registre-i-actualitzacions`, desactivados por defecto y con un contenido cerrado y documentado (`docs/registro-de-instalaciones.md`).
5. Una **prueba de privacidad por cambio**: provoca un error con datos de un alumno y comprueba que el registro no deja rastro.

## 9. Pruebas

- **Nombre**: `Metodo_Cuando_Resultado`, en inglés, y **un test por cada escenario relevante del spec**.

  ```csharp
  [Fact]
  [Trait("spec", "taquilles-i-zones/taquilles: Número único entre taquillas activas")]
  public async Task CreateLocker_WhenNumberUsedByActiveLocker_ReturnsNumberInUse() { ... }
  ```

  La etiqueta `spec` con `capacidad: requisito` permite listar qué requisitos tienen prueba y cuáles no.
- **Estructura**: Preparar, Actuar, Comprobar. Un solo motivo de fallo por test.
- **Datos de ejemplo** con constructores en `Arca.Testing` (`A.Locker().InZone("Planta 1").Build()`), no con datos repetidos en cada prueba.
- **Dominio y aplicación**: sin base de datos; el reloj es un `FakeClock`; los puertos se sustituyen por dobles solo donde hace falta.
- **Persistencia**: pruebas de integración con **SQLite cifrado en un fichero temporal real**, nunca simulado. Verifican migraciones, índices únicos y atomicidad.
- **Interfaz**: se prueba el comportamiento en los **modelos de vista**; la batería de vistas sin ventana se limita a enlaces y foco.
- **Pruebas de arquitectura** obligatorias (proyecto `Arca.Architecture.Tests`):
  - referencias entre capas;
  - los comandos de escritura de la interfaz usan el comando de ejecución única;
  - los DTO no llevan correo ni identificador;
  - `Domain` y `Application` no referencian EF Core ni el sistema de ficheros.
- **Pruebas transversales**: todas las claves de recurso existen en catalán; cada cambio tiene su prueba de privacidad.
- Se ejecutan en **Windows, Linux y macOS** con los scripts de `build/` (ver `docs/stack.md`).

## 10. Commits, ramas y trabajo con OpenSpec

Se definen en el punto 8 de `docs/preparacion-desarrollo.md` (reglas de trabajo con `/opsx:apply`). Ya fijado: mensajes de commit en inglés, en imperativo y con la línea de coautoría cuando corresponda.

## Lista de comprobación rápida para revisar código

- [ ] ¿La carpeta y el nombre siguen la capacidad?
- [ ] ¿Devuelve `Result<T>` sin lanzar excepciones para reglas de negocio?
- [ ] ¿Los códigos de error tienen clave en catalán?
- [ ] ¿Hay algún texto, fecha, dinero o comparación de texto que se salte los componentes centrales?
- [ ] ¿Las operaciones masivas usan dos fases y son indivisibles?
- [ ] ¿Los eventos de historial guardan datos estructurados y no texto?
- [ ] ¿Algún DTO, log o exportación lleva correo, identificador o notas?
- [ ] ¿Hay una prueba por escenario del spec, con su etiqueta?
- [ ] ¿La dependencia nueva cumple la regla de coste cero?
