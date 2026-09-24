# Registro de riesgos y pendientes

Punto 12 de `docs/preparacion-desarrollo.md`. Reúne en un solo sitio lo que puede frenar o torcer el proyecto, quién lo resuelve y cuándo hace falta. Se revisa al terminar cada etapa del hito y cada cambio.

**Cómo leerlo**
- **Necesario antes de**: el momento a partir del cual el punto bloquea o pone en riesgo el trabajo. Se expresa por etapa o cambio, no por fecha, porque el calendario aún no está fijado.
- **Impacto** y **Probabilidad**: alto, medio o bajo.
- **Responsable**: la persona que debe moverlo. Donde pone *persona responsable* es quien lleva el proyecto; los datos externos los aporta el centro.

## 1. Pendientes externos (datos o decisiones que llegan de fuera)

| # | Pendiente | Bloquea | Necesario antes de | Responsable | Estado |
|---|-----------|---------|--------------------|-------------|--------|
| E1 | **Fichero de muestra de secretaría** (columnas, si trae todo el centro o solo nuevos, identificador) | Importación de alumnos (`alumnes-i-assignacions`, grupo 6) y su tarea 9.5 | Hito 2 | Persona responsable, pidiéndolo a secretaría | Pendiente |
| E2 | **Valoración RGPD** con la dirección: identificador, correo, conservación de datos, custodia de copias y exportaciones | Política del identificador; conservación de `cursos-i-historial`; aviso de las exportaciones | Hito 2 (identificador) y antes de instalar en un centro real | Persona responsable con la dirección del centro | Pendiente |
| E3 | **Servidor de registro y avisos**: dónde se aloja, dominio de descargas y desde dónde se envían los correos | Puesta en marcha del registro y los avisos de `registre-i-actualitzacions` | Cuando se publique la primera versión | Persona responsable | Pendiente |
| E4 | **Validar la pantalla principal** (mapa de taquillas) con los conserjes | Implementación definitiva de Inicio (`ui-shell`) | Etapa 3 del hito 1 | Persona responsable con los conserjes | Pendiente |
| E5 | **Validar los términos del glosario** con los conserjes (dipòsit, pendents de pagament, reposada...) | Textos visibles | Etapa 4 del hito 1 | Persona responsable con los conserjes | Pendiente |
| E6 | **Política de conservación de datos** de cursos cerrados (cuántos, si se automatiza) | Automatización futura; hoy es manual | Después del hito 2 | Persona responsable con los conserjes | Pendiente |
| E7 | **Informes adicionales** que pidan los conserjes | Ampliar `informes-csv` | Después del hito 2 | Persona responsable con los conserjes | Pendiente |
| E8 | **Recordatorio o fecha de la última copia**: ¿lo echan en falta? | Ampliar `copies-de-seguretat` | Después del hito 2 | Persona responsable con los conserjes | Pendiente |
| E9 | **Un equipo Windows** para pruebas puntuales y para la demostración | Comprobaciones de las etapas 3 y 4 | Etapa 3 | Persona responsable | Pendiente |
| E10 | **Volumen y estructura reales del centro**: taquillas, alumnos, proporción con taquilla, niveles, grupos, importes | Que los perfiles de datos de ejemplo se parezcan a un centro real | Etapa 4 del hito 1 | Persona responsable con los conserjes | Pendiente |
| E11 | **Revisión legal**: titularidad, GPL-3.0-o-posterior, política de marca (y posible conflicto del nombre «ARCA»), aviso de privacidad del registro opcional y responsabilidad. **Dossier preparado** en `docs/legal/dossier-revision-legal.md`; falta la consulta profesional. Contribuciones de terceros: no se aceptan por ahora (`CONTRIBUTING.md`) | Distribuir binarios y recibir centros | Antes del primer centro piloto y antes de activar el servidor de registro | Persona responsable con asesoría legal | Dossier listo; pendiente consulta |

## 2. Riesgos técnicos

| # | Riesgo | Prob. | Impacto | Mitigación | Necesario antes de |
|---|--------|-------|---------|------------|--------------------|
| T1 | El cifrado con **SQLite3 Multiple Ciphers en formato SQLCipher 4** no se comporta como se supone (parámetros, copia en línea, migraciones) en los tres sistemas | Media | Alto | Spike técnico (punto 1) con criterios de éxito por sistema; CI en tres sistemas desde el principio | Etapa 1 |
| T2 | **Avalonia 12** es reciente (abril de 2026): arrastrar y soltar, renderizado sin ventana y listas virtualizadas pueden no cubrir lo que exige `ux-fonaments` | Media | Alto | Spike con criterios por sistema; alternativa de teclado y menú ya prevista para todo arrastre | Etapa 3 |
| T3 | **NSec** (Ed25519) puede tener problemas de binarios nativos en algún sistema; solo se usa para verificar la firma de los avisos de versión | Baja | Bajo | Spike; BouncyCastle como reserva; si fallara, los avisos se pueden desactivar sin afectar al resto | `registre-i-actualitzacions` |
| T4 | **La tabla con orden y selección múltiple** no se cubre con componentes gratuitos (TreeDataGrid es de pago) | Media | Medio | Spike con `Avalonia.Controls.DataGrid` o lista propia | Etapa 3 |
| T5 | Una **dependencia cambia de licencia** a una de pago (ya ocurrió con FluentAssertions) | Media | Alto (viola coste cero) | Control de licencias en CI, versiones fijadas, revisar el registro de terceros al actualizar | Etapa 1 (9.1b) |
| T6 | **Rendimiento** del mapa con 300 a 1000 taquillas y de la conciliación de 5000 filas | Baja | Medio | Consulta agregada por lotes, control virtualizado, pruebas de volumen en las tareas | Etapa 3 |
| T7 | Un fallo que **solo ocurre en Windows** (rutas, bloqueos de fichero, cultura) y se descubre tarde | Media | Medio | Sin CI remota: scripts idénticos en macOS, Linux (contenedor) y Windows, verificación en Windows **al terminar cada cambio** con paquete `win-x64` generado en macOS, y equipo Windows disponible (E9) | Etapa 1 |
| T8 | ~~La clave interna de cifrado deja de ser un secreto con código abierto~~ **Resuelto**: la llave sale de la contraseña del centro (`acces-i-xifrat`) y no hay secretos en el código | — | — | — | — |
| T10 | **Fallo de la verificación manual en Windows**: nadie la hace a tiempo y un error llega a `main` | Media | Medio | Cadencia fija (fin de cada cambio y de cada hito), lista de comprobación en el PR y script único | Etapa 1 |
| T9 | **Solapamientos entre cambios** que las specs no detectan (ya se corrigieron varios en la lectura cruzada) | Media | Medio | Regla de parar y actualizar la spec; lectura cruzada tras cambios grandes | Continuo |

## 3. Riesgos de producto y de proyecto

| # | Riesgo | Prob. | Impacto | Mitigación | Necesario antes de |
|---|--------|-------|---------|------------|--------------------|
| P1 | **Las pantallas de dominio no tenían dueño** (`docs/hito-1.md`). Decidido: cambio nuevo `pantalles-de-domini`, que se redacta sin maquetas (D3) | Cierta | Alto | Cambio 15 redactado (2026-09-24); falta implementarlo en la etapa 3; las etapas 1 y 2 avanzan sin él | Etapa 3 |
| P2 | La **pantalla principal no gusta** a los conserjes | Media | Medio | Es una pieza sustituible (`IHomeScreen`); demostración temprana con la aplicación funcionando | Etapa 3 |
| P3 | **Demasiada especificación** antes de tener nada delante: parte de lo escrito cambiará | Alta | Medio | Specs como documentos vivos; hito 1 pequeño; regla de actualizar la spec si choca | Continuo |
| P4 | Los conserjes **no tienen tiempo** o disponibilidad para validar | Media | Alto | Sesiones cortas con la aplicación funcionando y el guion de demostración; validar términos en una hoja | Etapa 3 |
| P5 | **Una sola persona** lleva el proyecto: si falta, se para | Media | Alto | Todo está por escrito (`AGENTS.md`, specs, convenciones, flujo); formato agnóstico de herramientas | Continuo |
| P6 | La **alta de datos de menores** sin una política RGPD cerrada | Media | Alto | Mínimo de datos, cifrado, nada sale del equipo; cerrar E2 antes de instalar en un centro | Antes de instalar en un centro |
| P13 | **La sigla «ARCA» ya está en uso por otras entidades** (por ejemplo, una agencia tributaria de otro país) y podría haber marcas registradas o confusión al buscarlo | Media | Medio | Búsqueda de anterioridades y consulta legal (E11, pregunta 9) antes de invertir en el nombre; el nombre es fácil de cambiar mientras no haya distribución. Decisión actual: **se mantiene el nombre por ahora** | Antes del primer centro piloto |
| P11 | **Forks y copias** que usan el nombre «ARCA» o confunden a los centros | Media | Medio | Política de marca (`TRADEMARK.md`), canal oficial de descargas, registro de la marca (opcional) y avisos firmados solo desde el servidor oficial | Antes de abrir el repositorio |
| P12 | **Un centro pierde la contraseña y la clave de recuperación** y con ellas los datos y las copias. **Decisión: sin custodia por el mantenedor; el centro lo pierde todo** | Media | Alto | Clave de recuperación obligatoria y confirmada, avisos claros, guía para la dirección (sobre cerrado, dos copias) y una contraseña de varias palabras sin relación, fácil de recordar para el centro | Antes de instalar en un centro |
| P7 | **Coste cero incumplido** sin darnos cuenta: GitHub cambia las condiciones, se añade un paquete de pago, firma de código | Baja | Alto | Regla en `AGENTS.md`, control de licencias en los scripts de verificación, repositorio privado sin depender de GitHub Actions, sin firma de código | Continuo |
| P8 | El **instalador sin firmar** genera avisos de Windows que asustan a los conserjes | Alta | Bajo | Documentar cómo autorizar; explicarlo en la primera instalación | Hito 2 |
| P9 | **Los términos catalanes** (dipòsit, reposada...) no los entienden las familias o los conserjes | Media | Bajo | Glosario con estado "validar"; cambiar es editar recursos | Etapa 4 |
| P10 | **El servidor de registro** (proyecto aparte) se retrasa o cae | Media | Bajo | La aplicación funciona igual sin él; solo se pierden los avisos y el conocimiento de instalaciones | Fuera de este repositorio |

## 4. Decisiones abiertas

| # | Decisión | Opciones | Quién decide | Antes de |
|---|----------|----------|--------------|----------|
| D1 | ~~Quién es dueño de las pantallas de dominio~~ **Decidido**: cambio nuevo `pantalles-de-domini`, redactado sin maquetas (D3) | — | — | — |
| D5 | ~~Clave de cifrado con el repositorio abierto~~ **Decidido**: contraseña compartida del centro con clave de recuperación obligatoria (`acces-i-xifrat`) | — | — | — |
| D2 | ~~Dónde se hace el spike~~ **Decidido (2026-09-24)**: rama descartable (`spike/tecnico`), que no se fusiona; lo aprendido se anota en `docs/` | — | — | — |
| D3 | ~~Herramienta y método para las maquetas~~ **Decidido (2026-09-24)**: no habrá maquetas; las decisiones de interfaz las propone Claude directamente en las specs de `pantalles-de-domini` y se iteran con la aplicación funcionando | — | — | — |
| D4 | Alcance del hito 2 | Propuesta en `docs/hito-1.md` | Persona responsable, tras la demostración | Fin del hito 1 |

## Revisión

Revisar este registro al terminar cada etapa del hito 1 y al cerrar cada cambio. Un punto se cierra cuando se resuelve o se descarta, anotando cómo y cuándo. Los riesgos nuevos se añaden con su número siguiente.
