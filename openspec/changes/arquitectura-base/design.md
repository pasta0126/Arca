## Context

Proyecto nuevo, sin código. Motivación y alcance en `proposal.md`; requisitos de comportamiento en `specs/`. Restricciones que dan forma al diseño:

- Desarrollo en macOS; destino principal Windows; Linux y macOS deben funcionar. Las pruebas en Windows serán puntuales, así que casi todo debe poder verificarse fuera de Windows.
- Un solo PC, sin concurrencia. Usuarios finales no técnicos: los fallos deben ser comprensibles y nunca dejar datos a medias.
- La lógica de negocio debe poder reutilizarse en una futura web, por lo que no puede depender de la UI ni de la base de datos concreta.
- Datos de menores: el cifrado en reposo es requisito, pero sin contraseña (decisión de producto: sin login).
- La clave de cifrado no puede depender del servidor de licencias.

## Goals / Non-Goals

**Goals:**
- Fijar estructura de solución, reglas de dependencia entre capas y tecnologías base.
- Fijar el modelo de almacenamiento, cifrado, migraciones y ubicación de datos.
- Fijar el mecanismo de i18n y de formato por cultura.
- Que el dominio, la aplicación y la persistencia se puedan probar íntegramente en macOS y Linux.

**Non-Goals:**
- Modelo de datos de taquillas, alumnos, pagos, llaves, incidencias y mantenimiento (cambios posteriores; aquí solo el mecanismo de migraciones).
- Diseño visual, navegación y branding (`ui-shell`).
- Copia y restauración manuales para el usuario, licencias, informes.
- Empaquetado de Linux y macOS más allá de un archivo portable.

## Decisions

### D1. Estructura de solución en cuatro capas con dependencias hacia dentro
```
src/Arca.Domain          entidades, valores, reglas, errores con código (sin dependencias)
src/Arca.Application     casos de uso, puertos (interfaces de repositorio, IClock, ILocalizer...)
src/Arca.Infrastructure  SQLite/SQLCipher, migraciones, ajustes, implementación de puertos
src/Arca.Desktop         Avalonia (MVVM), composición e inyección de dependencias
tests/*                  un proyecto de pruebas por capa
```
Reglas: `Domain` no referencia nada; `Application` solo `Domain`; `Infrastructure` y `Desktop` referencian `Application`; `Desktop` no accede a `Infrastructure` salvo en la raíz de composición. Una prueba de arquitectura verifica las referencias.
*Alternativa descartada*: proyecto único con carpetas. Es más rápido al inicio pero no impide que la UI toque la BBDD, y el requisito de reutilizar la lógica en una web lo hace arriesgado.

### D2. Plataforma: .NET LTS y Avalonia
Un único código C# para Windows, Linux y macOS. Avalonia permite estilos y temas ricos (encaja con la identidad configurable del centro). Las capas Domain, Application e Infrastructure apuntan a una versión LTS de .NET y no usan APIs específicas de Windows.
*Alternativas descartadas*: WPF (solo Windows), MAUI (sin soporte de escritorio Linux), Tauri/Electron (sacan el proyecto de .NET, sin ventaja aquí).

### D3. SQLite con SQLCipher mediante Entity Framework Core
Fichero único, sin servidor, con binarios nativos disponibles para los tres sistemas. El acceso a datos usa EF Core con el proveedor SQLite y SQLCipher (`Microsoft.EntityFrameworkCore.Sqlite.Core` más el paquete de SQLitePCLRaw con SQLCipher). El `DbContext`, las configuraciones y las migraciones viven solo en `Infrastructure`; `Domain` y `Application` no referencian EF Core. `Application` define interfaces de repositorio y `Infrastructure` las implementa, de modo que una futura web pueda usar otro almacén.
*Alternativa descartada*: SQL explícito con Microsoft.Data.Sqlite. Da más control fino, pero EF Core aporta modelo, migraciones y consultas tipadas con menos código a mantener. El control necesario sobre la copia previa y el rechazo de versiones nuevas se obtiene envolviendo el migrador (ver D5).

### D4. Clave de cifrado interna, igual en todas las instalaciones
La clave se deriva de un secreto incluido en la aplicación, idéntico en todas las instalaciones.
- **Por qué no una clave por equipo** (almacén del sistema: DPAPI, Keychain, libsecret): un fichero de datos o una copia de seguridad no podría abrirse en otro PC, y sustituir el ordenador de conserjería perdería los datos. El spec exige transportabilidad.
- **Por qué no una clave junto a la base de datos**: no aportaría protección alguna.
- **Límite asumido**: protege frente a la copia casual del fichero (por ejemplo, un USB perdido), no frente a quien examine el programa. Debe constar así en la documentación para la dirección del centro.
- Independiente de la licencia y de la red, por requisito.

### D5. Migraciones de EF Core envueltas en un migrador propio
Se usan las migraciones de EF Core, pero nunca `Migrate()` directo al arrancar: un servicio propio de `Infrastructure` controla el proceso. Al arrancar:
1. Comparar el historial de migraciones aplicadas de la base de datos con las que conoce el ensamblado. Si el fichero contiene alguna migración desconocida (es de una versión más nueva): rechazar sin tocar el fichero.
2. Si hay migraciones pendientes: copiar el fichero junto al original, ejecutar la comprobación de integridad sobre la copia y solo entonces migrar.
3. Migrar dentro de una transacción; si falla, revertir y no continuar.
4. Tras migrar con éxito, conservar solo las 3 últimas copias previas a migración y borrar las más antiguas.
Una base nueva se crea aplicando todas las migraciones desde cero, con el mismo camino que una actualización. Las migraciones generadas se revisan y se versionan en el repositorio; no se aplican cambios de modelo sin migración (una prueba comprueba que no hay cambios de modelo pendientes).

### D6. Ubicación de datos y modo portable
Ruta por defecto según el sistema (carpeta de datos de aplicación del usuario). Modo portable si existe un fichero marcador junto al ejecutable: en ese caso datos y ajustes viven junto al ejecutable. La ruta puede cambiarse desde los ajustes y se guarda en un fichero de configuración local.
Instancia única mediante un bloqueo asociado al fichero de base de datos (no al proceso), para que también funcione entre versión instalada y portable apuntando al mismo fichero.

### D7. Tiempo y dinero
- Importes como decimales de precisión exacta en dominio y como enteros en céntimos en el almacén; nunca coma flotante.
- Fechas de calendario como tipo de fecha sin hora (`DateOnly`); instantes en UTC. El acceso al reloj va detrás de una interfaz (`IClock`) para poder probar cierres de curso y vencimientos.

### D8. i18n con recursos `.resx` y localizador en Application
Los textos viven en recursos con clave; `Application` define `ILocalizer` y la UI lo consume. Dominio y aplicación devuelven **códigos de error estables**, nunca texto. El idioma base es el catalán; los idiomas nuevos son ficheros de recursos adicionales con retroceso al catalán por clave.
La cultura de formato se fija en catalán de España en v1, ignorando la del sistema. Una prueba automática recorre las claves usadas y falla si alguna no existe en el idioma base. En ejecución, una clave inexistente muestra la propia clave.
*Alternativa descartada*: JSON propio. Es más cómodo de editar, pero `.resx` da tooling, satélites por cultura y detección en compilación sin código propio. Reversible si se prefiere más adelante gracias a `ILocalizer`.

### D9. Comparación de texto
Ordenación y búsqueda con comparaciones sensibles a la cultura catalana e insensibles a mayúsculas y acentos. Se centraliza en un único componente para que todos los listados se comporten igual.

### D10. Verificación multiplataforma
Integración continua con matriz Windows, Linux y macOS que compila y ejecuta las pruebas. Esto cubre la limitación de probar en Windows solo de forma puntual. Las pruebas de persistencia usan ficheros temporales reales, no simulaciones.

### D11. Distribución
- **Windows**: instalador creado con Inno Setup, la herramienta estándar y gratuita. Admite instalación por usuario (sin administrador) y por equipo, y permite que la desinstalación conserve los datos por defecto.
  *Alternativas descartadas*: WiX/MSI (más potente para despliegue corporativo, pero más complejo de mantener) y MSIX (exige firma y no encaja bien con el modo portable).
- **Linux**: archivo `tar.gz` portable con el fichero marcador en v1. AppImage o paquetes nativos, más adelante si hay demanda.
- **macOS**: paquete `.app` comprimido en `.zip` en v1, sin firmar ni notarizar (el usuario tendrá que autorizarlo la primera vez). Firma y notarización, más adelante si se distribuye fuera del desarrollo.
- Versión de aplicación y de esquema visibles en la pantalla de información.

### D12. Contrato de resultado, progreso y cancelación en Application
Todo caso de uso devuelve un resultado estructurado, no lanza excepciones para reglas de negocio: éxito con datos, lista de avisos, o error con código estable (D8 y spec de i18n). Los datos incluyen los recuentos necesarios para el mensaje ("40 taquillas creadas"). Las operaciones largas reciben un informador de progreso con actual y total, y un token de cancelación. La cancelación solo se atiende antes de la fase de guardado indivisible; a partir de ahí el caso de uso indica que ya no es cancelable.
`Domain` y `Application` no saben nada de ventanas ni de notificaciones: la UI traduce cada resultado en mensaje, notificación o diálogo.
*Alternativa descartada*: excepciones para errores de negocio. Mezclan flujo normal y fallo real, y hacen más difícil mostrar avisos junto a un éxito parcial.

### D13. Arranque por etapas
El arranque es una secuencia de etapas con nombre (ajustes, ruta de datos, apertura, migración...) que informan de su avance a la pantalla de arranque. La pantalla se muestra desde el primer instante y no es una ventana de aplicación completa: es ligera para aparecer antes de que las etapas costosas empiecen. Un fallo en una etapa detiene la secuencia y entrega su código de error a la pantalla.

### D14. Indicadores de trabajo y protección frente a doble ejecución
Los comandos de la UI se ejecutan de forma asíncrona sobre un ejecutor común que: marca el comando como en curso (lo que deshabilita el control y evita el doble clic), muestra el indicador solo pasados 300 ms y recoge el resultado estructurado para mostrarlo. Ningún acceso a datos se hace en el hilo de la interfaz.

### D15. Carga bajo demanda, sin carga perezosa implícita
Las listas largas usan virtualización o paginación en la UI, y el detalle se pide al abrirlo. En persistencia se desactivan los proxies de carga perezosa de EF Core: las relaciones se cargan de forma explícita en cada consulta. Así no aparecen consultas repetidas ocultas y el coste de cada pantalla es visible en el código.
*Alternativa descartada*: carga perezosa de EF Core. Es cómoda, pero esconde consultas por elemento que degradan justo las listas de cientos de filas.

### D16. Registro técnico local
Registro en fichero con rotación y tamaño máximo, en la carpeta de datos (o junto al ejecutable en modo portable). Cada error inesperado recibe una referencia corta que se muestra al usuario. Política de privacidad: se registran tipos de error, pilas y códigos, nunca valores de datos de alumnos; las consultas no se registran con sus parámetros.
*Alternativa descartada*: sin registro. Hace imposible atender un fallo en un equipo ajeno.

## Risks / Trade-offs

- **Resultado estructurado en lugar de excepciones exige disciplina** → una prueba de arquitectura verifica que los casos de uso públicos devuelven el tipo de resultado y una revisión de código lo comprueba en cada cambio.
- **Umbral de 300 ms con indicador tardío puede dar sensación de congelación en equipos lentos** → el umbral es configurable en un único lugar y se valida con equipos reales.
- **Filtrar datos personales del registro es fácil de romper con un mensaje de excepción** → los tipos de error propios no incluyen valores de datos y una prueba comprueba que un error provocado con datos de alumno no deja rastro.
- **La clave interna es ofuscación fuerte, no seguridad frente a un atacante con el programa** → documentarlo con claridad; mantener la opción de ampliarlo a una clave por centro en el futuro sin cambiar el resto.
- **SQLCipher y binarios nativos en tres sistemas** → validar el arranque en los tres desde el primer hito y cubrirlo en la matriz de CI.
- **Rechazar bases de versión más nueva bloquea al usuario** → el mensaje debe indicar exactamente qué hacer (actualizar la aplicación); nunca degradar ni abrir en modo parcial.
- **Bloqueo de instancia única sobre carpetas de red o sincronizadas (OneDrive)** → documentar que la base de datos debe estar en disco local; el bloqueo fallido se trata como "ya en uso".
- **Copia previa a la migración ocupa espacio** → la base es pequeña; se conservan las 3 últimas copias previas y no se acumulan indefinidamente.
- **EF Core y SQLCipher requieren configuración específica del proveedor** → validar el arranque cifrado en los tres sistemas desde el primer hito y cubrirlo en la matriz de CI.
- **Las migraciones generadas de EF Core pueden ocultar cambios de esquema no deseados** → revisarlas en cada cambio y probar que el modelo no tiene cambios pendientes.
- **Un `.app` de macOS sin firmar muestra avisos del sistema** → aceptable en v1 (uso de desarrollo y pruebas); documentar el paso de autorización.
- **`.resx` menos cómodo de editar para traductores** → aceptable en v1 con un solo idioma; el localizador permite cambiar el formato después.
- **Fijar la cultura catalana ignora la configuración del sistema** → es una decisión de producto de v1; se revisará al añadir el selector de idioma.

## Migration Plan

No aplica: proyecto nuevo. La primera versión crea la base de datos con el esquema inicial. La política de migraciones (D5) queda establecida para todos los cambios posteriores, que aportarán sus propios scripts numerados.

## Open Questions

Ninguna pendiente. El instalador, el empaquetado de Linux y macOS y la retención de copias previas quedaron decididos en D5 y D11.
