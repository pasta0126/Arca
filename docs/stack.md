# Stack técnico

Decisiones de tecnología y versiones de ARCA. Punto 2 de `docs/preparacion-desarrollo.md`.

Última comprobación de versiones: **24 de septiembre de 2026**, con las fuentes indicadas. Lo marcado como *a confirmar* se verifica en el spike técnico y al crear el esqueleto, y las versiones exactas se fijan entonces en `Directory.Packages.props`.

## Principios

- **Coste cero, ahora y en el futuro** (decisión del proyecto): solo librerías, componentes, herramientas y servicios gratuitos con licencia permisiva (MIT, Apache 2.0, BSD), sin nada que haya que pagar ni que pueda pasar a serlo. Cada paquete nuevo se revisa antes de añadirlo y se registra en `THIRD-PARTY-NOTICES.md`.
- **Multiplataforma real**: todo paquete debe funcionar en Windows, Linux y macOS (x64 y arm64) y se verifica con los scripts de `build/` en macOS, Linux (contenedor) y Windows (ver «Verificación multiplataforma»).
- **Pocas dependencias**: lo que se puede resolver con la biblioteca estándar de .NET no añade paquete.
- **Versiones fijadas**: gestión central de paquetes y SDK fijado, para que cualquiera compile lo mismo.
- **Software libre bajo GPL-3.0** con la marca «ARCA» reservada (`LICENSE`, `TRADEMARK.md`). Toda dependencia debe ser **compatible con la GPL-3.0**: MIT, Apache 2.0, BSD e ISC sí; MS-PL y licencias con cláusulas incompatibles, no.

## Coste cero: comprobación y costes ocultos

Repaso de todo lo que puede costar dinero, ahora o más adelante.

| Elemento | ¿Gratis? | Riesgo futuro | Decisión |
|----------|----------|---------------|----------|
| .NET, EF Core, CommunityToolkit.Mvvm | Sí (MIT) | Ninguno | Usar |
| Avalonia (framework) | Sí (MIT, sin restricciones de uso comercial) | La empresa vende herramientas y componentes aparte | Usar solo el framework y paquetes MIT. **No** usar componentes premium (TreeDataGrid, reproductor multimedia, teclado virtual, gráficos), ni Accelerate ni XPF. No depender de las herramientas de desarrollo de Avalonia, que son opcionales y su licencia gratuita tiene condiciones (particulares y organizaciones pequeñas). |
| SQLite3 Multiple Ciphers, NSec.Cryptography | Sí (MIT) | Ninguno | Usar |
| Serilog, xUnit, NSubstitute, AwesomeAssertions, CsvHelper, NetArchTest | Sí (Apache, BSD, MIT), *a confirmar cada licencia al añadirlos* | Bibliotecas que cambian de licencia (ya pasó con FluentAssertions) | Fijar versiones y comprobar la licencia en CI (ver abajo) |
| Inno Setup (instalador) | Sí, también para uso comercial | Ninguno relevante | Usar |
| GitHub | **Sí**: los repositorios, privados o públicos, son gratuitos. Con el repositorio **público**, GitHub Actions también es gratuito e ilimitado | Los minutos de GitHub Actions en privado son limitados (2.000 al mes en Linux, Windows a doble coste y macOS a diez veces) y GitHub anunció en diciembre de 2025 cobrar también los runners propios (lo pospuso, sin descartarlo) | **No dependemos de GitHub Actions**: la verificación va en scripts locales. GitHub es solo el alojamiento del código. |
| Certificado de firma de código de Windows | **De pago** (anual) | Los instaladores sin firmar muestran avisos de SmartScreen | No se firma en v1. Se documenta el aviso y cómo autorizar la instalación. |
| Apple Developer Program (firma y notarización de macOS) | **De pago** (anual) | Sin firmar, macOS pide autorizar la aplicación | No se firma ni notariza (ya decidido en `arquitectura-base`). |
| Servidor de registro y avisos de versión | Fuera de este repositorio | Alojamiento, dominio y envío de correos son un coste del otro proyecto | Anotado como dependencia externa. El registro es opcional y la aplicación funciona igual sin él. |
| Editor y herramientas de diseño | Depende de la herramienta | Licencias no comerciales que cambian | Editor gratuito a elección de cada persona. Para maquetas, papel o herramientas de código abierto (como Penpot). |
| Iconos y tipografías | Depende | Licencias que exigen atribución o pago | Solo de licencia abierta (Fluent UI System Icons o Material Symbols; Inter o Noto con licencia OFL) y registrados en `THIRD-PARTY-NOTICES.md`. |

**Control automático en CI:** un paso que lista las licencias de todos los paquetes NuGet y falla si alguno no está en la lista permitida (MIT, Apache 2.0, BSD, ISC), es decir, compatible con la GPL-3.0. Así una dependencia nueva o un cambio de licencia en una actualización no entra sin que nos enteremos.

## Verificación multiplataforma (sin CI remota)

Decisión del proyecto: **por ahora no hay CI remota**. La lógica de comprobación vive en scripts del repositorio y se ejecuta donde haga falta.

**Decisión de 2026-09-24 (alcance de la verificación):** el desarrollo y las pruebas diarias son solo en **macOS**. Las pruebas en **Linux quedan descartadas por ahora** (el producto sigue diseñándose para Linux, pero no se verifica hasta que haya motivo). Las pruebas en **Windows se hacen solo antes de salir a producción**, en las fases avanzadas de la v1, y no al terminar cada cambio. Las filas de la tabla siguiente describen el objetivo final; la cadencia vigente es esta. El riesgo de descubrir tarde un fallo propio de Windows o Linux se acepta de forma consciente (T7 en `docs/riesgos.md`).

| Dónde | Cómo | Cuándo |
|-------|------|--------|
| **macOS** (equipo de desarrollo) | `build/test.sh`: restaura, compila con advertencias como errores, ejecuta todas las pruebas, las de arquitectura y el control de licencias de dependencias | Antes de cada commit de grupo de tareas |
| **Linux** | El mismo `build/test.sh` dentro de un contenedor con el SDK de .NET, con Colima o Podman (`build/test-linux.sh`) | Antes de cada commit de grupo de tareas |
| **Windows** | `build/test.ps1` en un equipo Windows con el SDK de .NET instalado (gratuito) y el paquete portable `win-x64` generado con `build/publish.sh win-x64` desde macOS | **Al terminar cada cambio** y en cada hito, más la prueba manual del guion |

Notas:
- Un **paquete portable de Windows se genera desde macOS** (`dotnet publish -r win-x64`); los binarios nativos de SQLite3 Multiple Ciphers y de libsodium ya incluyen `win-x64`. El **instalador de Inno Setup sí exige Windows**, y queda diferido al hito 2.
- El riesgo de esta decisión es que un fallo que solo ocurre en Windows se detecta cuando alguien lo prueba, no en cada commit (riesgo T7 en `docs/riesgos.md`). Se acota con la cadencia anterior y con scripts idénticos en las tres máquinas.
- **Servidor propio** (un equipo Linux arm64 con Docker, compartido con otros proyectos): no puede ejecutar Windows ni macOS y ya tiene carga de producción, así que **no se usa como CI**. Sí puede servir, si se quisiera, para alojar los paquetes de prueba o para una verificación adicional de Linux arm64 con límites de recursos. Se valora cuando haga falta.
- **Sin secretos de cifrado en el código**: la llave de la base sale de la contraseña del centro y de una clave de recuperación (`acces-i-xifrat`). No hay ninguna llave que inyectar al compilar y cualquier compilación abre una base con la contraseña correcta.
- Si se quisiera una CI remota, se crea un flujo que solo llame a los scripts, sin lógica propia.

## Plataforma y UI

| Elemento | Decisión | Versión | Notas |
|----------|----------|---------|-------|
| Plataforma | .NET LTS | **.NET 10** (LTS, publicada en noviembre de 2025, soporte hasta noviembre de 2028) | Se actualizará a la siguiente LTS (noviembre de 2027) antes de que acabe el soporte. No se usa .NET 11 (ciclo corto). |
| UI | Avalonia | **12.1.x** (12.0 estable desde abril de 2026; 12.1.2 publicada el 2 de septiembre de 2026) | Avalonia 12 es un salto mayor respecto a la 11: el spike comprueba en la 12 el renderizado sin ventana, arrastrar y soltar y la lista virtualizada. |
| MVVM | CommunityToolkit.Mvvm | Última estable, *a confirmar* | Generadores de código para propiedades y comandos. |
| Tablas | `Avalonia.Controls.DataGrid` (MIT) o lista virtualizada propia | *A confirmar* en Avalonia 12 | **No** TreeDataGrid, que es un componente de pago. El spike comprueba que la tabla con orden y selección múltiple se cubre con paquetes gratuitos. |
| Inyección de dependencias | Microsoft.Extensions.DependencyInjection | Alineada con .NET 10 | Sin contenedor de terceros. |

## Datos y cifrado

| Elemento | Decisión | Versión | Notas |
|----------|----------|---------|-------|
| Acceso a datos | Entity Framework Core con `Microsoft.EntityFrameworkCore.Sqlite.Core` | Alineada con .NET 10 | **Nunca** `Microsoft.EntityFrameworkCore.Sqlite` (trae su propio SQLite sin cifrado). Solo en `Infrastructure`. |
| Cifrado de la base | **SQLite3 Multiple Ciphers**: `SQLite3MC.PCLRaw.bundle` | 2.4.0 (28 de julio de 2026, SQLite 3.53.4) | Licencia MIT. Un solo paquete de enlace SQLitePCLRaw en el proyecto. |
| Formato de cifrado | **Compatible con SQLCipher 4** (`cipher=sqlcipher`, `legacy=4`) | — | Formato estándar y documentado: los datos se pueden recuperar con herramientas SQLCipher aunque ARCA dejara de existir. Los parámetros exactos y la derivación de la clave se confirman en el spike. |
| Ficheros de exportación | CSV con la biblioteca **CsvHelper** tras `ICsvReader` | *A confirmar* (licencia y versión; si es MS-PL o Apache 2.0, acogerse a Apache 2.0 por compatibilidad con la GPL) | Si no cumpliera los principios, se sustituye tras el puerto. |

**Por qué no el SQLCipher que asumían las primeras specs**
- `SQLitePCLRaw.bundle_e_sqlcipher` está **obsoleto** (sin mantenimiento) y contiene binarios de SQLCipher antiguos.
- El SQLCipher oficial para .NET (Zetetic) solo se ofrece con **licencia comercial**.
- Compilar SQLCipher Community por nuestra cuenta exige mantener binarios nativos para tres sistemas y dos arquitecturas.

SQLite3 Multiple Ciphers escribe en formato compatible con SQLCipher, es gratuito y está mantenido, por lo que las specs de `arquitectura-base` siguen valiendo tal cual.

## Criptografía de contraseña y firma de avisos

| Elemento | Decisión | Versión | Notas |
|----------|----------|---------|-------|
| Contraseña, llaves y avisos | **NSec.Cryptography** (Ed25519, basada en libsodium) | 26.4.0 | Licencia MIT. **Ed25519 no está integrado en .NET 10**: la propuesta de la biblioteca estándar sigue abierta. Si NSec diera problemas de binarios nativos, la alternativa de reserva es BouncyCastle (código gestionado). |
| Cliente HTTP | `HttpClient` de la biblioteca estándar | — | Sin paquete adicional. |

## Registro, recursos y pruebas

| Elemento | Decisión | Versión | Notas |
|----------|----------|---------|-------|
| Registro técnico | Serilog con salida a fichero con rotación | *A confirmar* | Sin datos de alumnos (ver convenciones). |
| Recursos e i18n | Ficheros `.resx` con `ILocalizer` | — | Ya decidido en `arquitectura-base`. |
| Marco de pruebas | xUnit | Última estable, *a confirmar* si la v3 | |
| Aserciones | Aserciones de xUnit o **AwesomeAssertions** | *A confirmar* | **No FluentAssertions**: desde su versión 8 tiene licencia comercial. |
| Dobles de prueba | **NSubstitute** | *A confirmar* | |
| Pruebas de arquitectura | NetArchTest o ArchUnitNET | *A confirmar* | Referencias entre capas, comando de ejecución única, clasificación de licencia. |
| Pruebas de vista | Avalonia.Headless con xUnit | La de Avalonia 12 | Solo enlaces y foco; el comportamiento vive en modelos de vista. |

## Herramientas de proyecto

| Elemento | Decisión | Notas |
|----------|----------|-------|
| Gestión de paquetes | Central Package Management (`Directory.Packages.props`) | Una sola versión por paquete en toda la solución. |
| SDK | `global.json` con la versión fijada | |
| Calidad de código | Nullable activado, analizadores de .NET y `.editorconfig`, advertencias como errores en CI | |
| Verificación multiplataforma | **Scripts en `build/`** (`test.sh`, `test.ps1`, `publish.sh`), sin CI remota por ahora | Ver la sección siguiente. Si más adelante se quisiera CI remota, solo tendría que llamar a estos scripts. |
| Contenedores para Linux | **Colima** o **Podman** (gratuitos sin condiciones) | Docker Desktop es gratuito solo para uso personal o empresas pequeñas, y esas condiciones pueden cambiar. |
| Instalador de Windows | Inno Setup | Ya decidido en `arquitectura-base`. |
| Paquetes de Linux y macOS | `tar.gz` y `.app` comprimido | Ya decidido en `arquitectura-base`. |
| Avisos de terceros | `THIRD-PARTY-NOTICES.md` | Atribución exigida por MIT, Apache y BSD. |

## Alternativas descartadas

| Descartado | Motivo |
|------------|--------|
| SQLCipher oficial (Zetetic) | Licencia comercial |
| `SQLitePCLRaw.bundle_e_sqlcipher` | Obsoleto y con binarios antiguos |
| Compilar SQLCipher Community | Mantener binarios nativos de tres sistemas |
| RSA para la firma de claves | Claves y firmas mucho mayores, incómodas de pegar |
| .NET integrado para Ed25519 | No disponible en .NET 10 |
| FluentAssertions | Licencia comercial desde la versión 8 |
| WPF, MAUI, Tauri, Electron | Ver `arquitectura-base` (D2) |
| .NET 11 | Ciclo de soporte corto |

## Pendiente de confirmar en el spike

1. Parámetros de `SQLite3MC.PCLRaw.bundle` para el formato SQLCipher 4 con una llave de 256 bits entregada directamente, y calibrar Argon2id (memoria y pasadas) y XChaCha20-Poly1305 de NSec en equipos de gama baja (menos de 2 segundos).
2. Que la copia en línea funciona sobre una base cifrada con ese paquete.
3. Que NSec carga sin problemas en Windows, Linux y macOS (x64 y arm64).
4. Que Avalonia 12.1 cubre renderizado sin ventana, arrastrar y soltar y la lista virtualizada.
5. Versiones exactas del resto de paquetes marcados *a confirmar*, y su licencia.
6. Que la tabla de `ux-fonaments` (orden, filtro y selección múltiple) se resuelve con paquetes gratuitos.

## Fuentes

- Política de soporte de .NET: <https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core>
- Avalonia 12: <https://avaloniaui.net/blog/avalonia-12>
- SQLite3 Multiple Ciphers para .NET: <https://www.nuget.org/packages/SQLite3MC.PCLRaw.bundle>
- Estado obsoleto de `bundle_e_sqlcipher`: <https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlcipher>
- SQLCipher de Zetetic para .NET (licencia comercial): <https://www.zetetic.net/sqlcipher/sqlcipher-for-dotnet/>
- Opciones de cifrado de SQLite en .NET: <https://www.bricelam.net/2023/11/10/more-sqlite-encryption.html>
- Licencia y herramientas de Avalonia: <https://avaloniaui.net/blog/building-a-sustainable-future-for-avalonia>
- Minutos gratuitos de GitHub Actions: <https://docs.github.com/billing/managing-billing-for-github-actions/about-billing-for-github-actions>
- Cambios de precio de GitHub Actions (aplazados): <https://github.blog/changelog/2025-12-16-coming-soon-simpler-pricing-and-a-better-experience-for-github-actions/>
- Propuesta de Ed25519 en .NET (abierta): <https://github.com/dotnet/runtime/issues/63174>
- NSec.Cryptography: <https://www.nuget.org/packages/NSec.Cryptography/>
