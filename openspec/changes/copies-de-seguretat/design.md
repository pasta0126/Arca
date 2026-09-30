## Context

Décimo cambio. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. `arquitectura-base` ya fija la base como un único fichero SQLite cifrado con SQLCipher y una llave protegida por la contraseña del centro (`acces-i-xifrat`), con un fichero de claves que viaja con cada copia (transportable entre equipos y sistemas), un migrador propio que hace copia previa verificada, rechaza versiones más nuevas y conserva las 3 últimas copias previas, y la instancia única sobre el fichero. Este cambio construye sobre eso, sin modificar el modelo de datos.

Restricciones: solo manual; sin red; datos de menores; un solo PC y una sola instancia.

## Goals / Non-Goals

**Goals:**
- Copia consistente y verificada sin parar la aplicación.
- Restauración sin riesgo de perder los datos actuales: verificación, vista previa, copia previa y recuperación automática.
- Reutilizar migrador, integridad y retención existentes.

**Non-Goals:**
- Automatismos, nube, contraseña propia de la copia, restauración parcial o ajustes locales.

## Decisions

### D1. La copia es un fichero de base de datos cifrado, sin formato propio
El fichero de copia es un contenedor sencillo, sin compresión, con la base SQLCipher completa y el fichero de claves vigente (`acces-i-xifrat`), y una extensión propia de ARCA. No hay metadatos externos: la versión de esquema sale del historial de migraciones de la base y los recuentos, de consultas sobre ella. *Alternativa descartada*: copiar solo la base; sin su fichero de claves no se podría abrir.

### D2. Copia consistente con la API de copia en línea de SQLite
Se usa la API de copia en línea (o equivalente del paquete de cifrado, ver `docs/stack.md`) sobre una conexión propia, que da una instantánea coherente sin bloquear la aplicación ni copiar el fichero a ciegas con la base abierta (que podría dejar el diario a medias). Se escribe en un temporal junto al destino, se verifica y se mueve al nombre final. Como hay una sola instancia y un solo PC, no hay escritores concurrentes salvo la propia aplicación, y la API los tolera.

### D3. Verificación en dos comprobaciones
Sobre la copia recién hecha, y sobre cualquier fichero elegido para restaurar: (1) se abre con la llave desenvuelta con la contraseña o la clave de recuperación de esa copia, (2) `PRAGMA integrity_check`, y (3) se lee su historial de migraciones para clasificarla: igual, anterior (migrable) o desconocida (más nueva, rechazada). Es la misma lógica que el migrador; se extrae en un servicio compartido en vez de duplicarla.

### D4. Vista previa con recuentos
Se abre la copia en solo lectura y se cuentan cursos, alumnos, taquillas y asignaciones. Nunca se leen nombres. La fecha que se muestra es la del fichero. No se compara ninguna marca de "última copia" porque se decidió no guardarla.

### D5. Restauración como secuencia con punto de retorno
1. Verificar y mostrar la vista previa (sin cambios).
2. Tras confirmar, cerrar la base actual (liberando el bloqueo de instancia única).
3. Crear y verificar la copia previa de los datos actuales junto a la base, con nombre de restauración y fecha. Si la base actual está dañada, el fichero se conserva tal cual, sin verificar, como copia previa.
4. Copiar la copia elegida a un temporal junto a la base y, si es anterior, migrarla allí con el migrador (que a su vez hace su copia previa de migración).
5. Mover el temporal sobre la base; reabrir.
6. Ante un fallo en cualquier punto: restaurar la copia previa sobre la base y reabrir. Si esto también fallara, se informa de dónde están ambos ficheros sin borrar ninguno.
La migración ocurre sobre un temporal, de modo que la copia original nunca se modifica y la base actual no se toca hasta el paso 5.

### D6. Retención de copias previas a restauración
Se conservan las 3 más recientes, con el mismo mecanismo y criterio que las previas a migración pero en un conjunto separado, para que las migraciones no expulsen las de restauración. La limpieza solo se hace tras una restauración correcta.

### D7. Estado tras restaurar
La aplicación reabre la base y recarga su estado en memoria desde ella (cursos, curso activo, configuración guardada en la base). Nada de lo anterior se conserva en caches. Los ajustes locales (ruta de la base, modo portable) no forman parte de la copia y no cambian.

### D8. Disponible siempre
La restauración es la vía de recuperación tras un desastre y no depende de ninguna condición externa. Se ofrece también desde el mensaje de fichero dañado de `arquitectura-base`.

### D9. Validación del destino
Antes de empezar: el destino no es el fichero de la base, la carpeta existe y admite escritura, y hay espacio libre suficiente (al menos el tamaño actual de la base). Así los errores salen antes de generar nada.

### D10. Feedback
Resultado estructurado con ubicación y tamaño, progreso con etapas reales (verificando, copiando, migrando, recargando), confirmaciones con su consecuencia y protección contra doble ejecución, según los principios de UX transversal.

### D11. Reutiliza lo que ya hay de `acces-i-xifrat`
El contenedor (`.arcabackup`: base cifrada más fichero de claves), la copia en línea, la verificación, la sesión de restauración con su desbloqueo por contraseña o por clave de recuperación y la copia previa ya existen en `Arca.Infrastructure/Backup` con sus pruebas. Este cambio **no los reescribe**: los completa (validación del destino, nombre propuesto, vista previa y clasificación de versión, migración en un temporal, retención de las 3 copias previas, recuentos) y los expone a la interfaz detrás de un servicio de Application (la interfaz no conoce Infrastructure).

### D12. Migrar al restaurar, sobre el temporal
La restauración copia la base elegida a un temporal junto a la base actual y, si es de una versión anterior, la migra allí con el migrador existente antes del intercambio atómico. La base actual no se toca hasta ese momento y el fichero de la copia nunca se modifica. Si la migración falla, se descarta el temporal y se informa sin cambiar nada.

### D13. Reinicio tras restaurar
La aplicación construye todos sus servicios sobre la sesión de base de datos abierta; recargar «en caliente» dejaría referencias a los datos anteriores. Tras una restauración correcta se guarda lo pendiente, se cierra la base y la aplicación **se lanza de nuevo a sí misma y termina**. Así no hay caché de los datos anteriores y el arranque normal pide la contraseña (que ahora es la de la copia). Si no se puede lanzar de nuevo, se informa de que los datos ya están restaurados y hay que abrir ARCA otra vez.

### D14. Pantalla de Ajustes
Un bloque plegable «Còpia de seguretat» en Ajustes con dos acciones: Fer una còpia y Restaurar una còpia. Usa el selector de ficheros del sistema (guardar y abrir), el `IConfirmationService` para el aviso de datos de menores, la sobrescritura y la confirmación final con el resumen de la vista previa como detalle, y los formularios de contraseña existentes (con la opción de la clave de recuperación). El bloque no depende del curso ni de los datos.

## Risks / Trade-offs

- **Restaurar una copia hecha con otra contraseña cambia la del centro** → aviso antes de confirmar y copia previa conservada; las copias antiguas siguen abriéndose con la contraseña que tenían.
- **Nadie hace copias porque no hay recordatorios** → decisión de producto: sin recordatorios ni fecha de última copia. Queda como pregunta para los conserjes por si echan en falta un aviso.
- **Restaurar una copia antigua pierde el trabajo posterior** → vista previa con comparación de recuentos y advertencia, y copia previa automática de los datos actuales.
- **Una restauración interrumpida (corte de luz) deja la base a medias** → el temporal se mueve al final de forma atómica y la copia previa existe desde antes; si al arrancar hay un temporal huérfano, se ignora y se borra.
- **Los ajustes locales no viajan con la copia** → tras restaurar en un equipo nuevo hay que configurar la ruta, lo que ya es parte de la instalación.
- **Tamaño de las copias previas en disco** → la base es pequeña y se conservan solo 3 de cada tipo.

## Migration Plan

Sin migración de base de datos. Solo se añade el servicio compartido de verificación, y la salida del migrador se refactoriza para reutilizarlo sin cambiar su comportamiento.

## Open Questions

- Si los conserjes echan en falta un recordatorio o la fecha de la última copia: se preguntará al hablar con ellos; añadirlo no cambiaría el modelo.
- Extensión y nombre exactos del fichero de copia: detalle de implementación sin efecto en los specs.
