# Copias de seguridad y restauración

Cómo funciona (`copies-de-seguretat`, sobre lo de `acces-i-xifrat`) y dónde engancharse desde otros cambios.

## Piezas

- **Application** (`Arca.Application/Backup`): `IBackupService` (nombre propuesto, hacer copia, abrir una copia), `IRestoreSession` (contraseña o clave de recuperación de la copia, vista previa, restaurar), `IApplicationRestarter`, y los modelos (`ContentCounts`, `RestorePreview`, `BackupResult`, `RestoreOutcome`, `BackupErrors`). No conoce ficheros de base de datos.
- **Infrastructure** (`Arca.Infrastructure/Backup`): `BackupMaker` (copia en línea, verificada y atómica), `BackupDestination` (nombre y validación del destino), `DatabaseInspector` (versión igual/anterior/más nueva y recuentos, sin leer datos personales), `BackupRestorer.CompleteAsync` (copia previa verificada, migración sobre el temporal, sustitución atómica, recuperación y retención de 3 copias previas) y `BackupService` (lo expone a la interfaz).
- **UI** (`Arca.UI/Backup`): `BackupViewModel` y `BackupView` (bloque «Còpia de seguretat» de Ajustes), `IBackupFilePicker`. La carpeta de la última copia se guarda en los ajustes locales, no en la base.
- **Desktop**: `ApplicationRestarter` cierra la ventana (eso libera la base y su cerrojo) y después lanza de nuevo el proceso.

## Reglas que conviene no romper

- La copia y la restauración no usan la red y están siempre disponibles (hay prueba de arquitectura).
- Nada se sustituye hasta el último paso; si algo falla se vuelven a poner los datos actuales. El fichero de la copia nunca se modifica (la migración se hace sobre un temporal).
- Restaurar una copia hecha con otra contraseña cambia la del centro: se avisa antes de confirmar.
- Las copias previas a una restauración (`*.prerestore-*.bak`, máximo 3) son independientes de las copias previas a una migración.

## Punto de enganche con `cursos-i-historial`

Antes de **anonimizar o borrar** los datos de un curso, el flujo de confirmación ofrece hacer una copia sin obligar a ello. Para hacerlo no hay que tocar nada de este cambio:

1. Inyectar `BackupViewModel` (o, si se quiere un flujo propio, `IBackupService` y `IBackupFilePicker`).
2. Desde el diálogo de confirmación del borrado, un botón «Fer una còpia abans» llama a `BackupViewModel.MakeAsync()`; al terminar se vuelve al diálogo con el resultado ya notificado.
3. La acción de borrar no depende de que la copia se haya hecho: la copia y la exportación están siempre disponibles, pero nunca son un requisito.
