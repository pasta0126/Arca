## Why

ARCA no tiene todavía ninguna base técnica. Antes de especificar taquillas, alumnos o pagos hay que fijar las decisiones transversales que condicionan a todos los demás cambios: dónde y cómo se guardan los datos, cómo se evolucionan entre versiones, cómo se preparan los textos para varios idiomas y cómo se separan las capas para que la lógica de negocio sea la única fuente de verdad (y reutilizable en una futura web).

## What Changes

- Estructura de solución .NET multiplataforma (Windows, Linux y macOS) en capas: dominio y aplicación sin dependencias de UI, persistencia detrás de interfaces de repositorio y UI de escritorio con Avalonia (MVVM).
- Base de datos local SQLite cifrada con clave interna transparente, en un único fichero, sin concurrencia (un solo PC).
- Ruta de la base de datos configurable, para cubrir instalación clásica y versión portable.
- Migraciones de esquema versionadas, con copia de seguridad verificada antes de migrar y rechazo de bases creadas por una versión más nueva.
- Internacionalización por claves en ficheros de recursos: v1 solo catalán, sin textos literales en código ni vistas, y formato de fechas, números e importes según la cultura activa.
- Distribución: instalador (principal) y versión portable (para equipos sin permisos de administrador).
- Contrato de feedback para todos los casos de uso: resultado estructurado (éxito, avisos, errores con código), progreso con recuentos y cancelación segura.
- Pantalla de arranque con etapas reales, indicadores de trabajo, notificaciones no bloqueantes, confirmación de acciones irreversibles y estados vacíos y de carga.
- Carga bajo demanda de datos en la interfaz y persistencia sin carga perezosa implícita.
- Registro técnico local de errores inesperados, acotado y sin datos personales.
- Los tests de dominio y persistencia se pueden ejecutar en macOS sin necesidad de Windows.

## Capabilities

### New Capabilities
- `emmagatzematge-local`: base de datos local cifrada, ubicación configurable, migraciones de esquema y protección frente a versiones incompatibles.
- `internacionalitzacio`: textos por claves, catalán como único idioma de v1, formato por cultura y extensibilidad a nuevos idiomas sin tocar código.
- `distribucio-multiplataforma`: instalador y versión portable, y comportamiento equivalente en Windows, Linux y macOS.
- `feedback-operacions`: pantalla de arranque, resultado y progreso de las acciones, notificaciones, confirmaciones, estados vacíos y de carga, carga bajo demanda y registro técnico de errores.

### Modified Capabilities

<!-- Ninguna: es el primer cambio y no existen specs previas. -->

## Fuera de alcance

- Cualquier funcionalidad de dominio (taquillas, alumnos, pagos, llaves, incidencias, mantenimiento).
- Copias de seguridad y restauración manuales para el usuario (cambio `copies-de-seguretat`); aquí solo se define la copia previa a una migración.
- Licenciamiento (cambio `llicencies-client`) y servidor de licencias (otro proyecto).
- Pantallas, navegación y branding (cambio `ui-shell`), y el sistema de componentes visuales, adaptabilidad, colapsables y arrastrar y soltar (cambio `ux-fonaments`). Aquí solo se fija el contrato de feedback y la pantalla de arranque.
- Asistente de configuración inicial (cambio `configuracio-inicial`).
- Selector de idioma y traducciones a castellano o inglés (versiones posteriores).
- Firmado de código y canal de actualizaciones automáticas.

## Impacto

- **Código**: crea la estructura inicial de la solución; no existe código previo.
- **Datos personales (RGPD)**: la base de datos contendrá datos de menores en cambios posteriores. Este cambio fija el cifrado en reposo. La clave interna protege frente a la copia del fichero, no frente a quien tenga acceso al programa; esto debe constar en la documentación para la dirección del centro.
- **Dependencias**: .NET LTS, Avalonia, SQLite con cifrado (SQLCipher) y una herramienta de empaquetado para el instalador de Windows.
- **Decisión heredada**: la clave de cifrado no puede depender del servidor de licencias, para que un corte de licencia nunca deje los datos inaccesibles.
