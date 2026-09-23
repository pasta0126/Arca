# Preparación del desarrollo

Pasos previos para que, al empezar a implementar, todo esté claro y se pueda ir directo. Se valoran uno a uno: lo que es **definir** se hace ahora (documentos y decisiones, sin código de producto) y lo que es **implementar** queda para cuando se empiece a desarrollar.

Estado de cada punto: `por valorar` · `aceptado` · `descartado` · `hecho`.

## Resumen

| # | Punto | Tipo | Cuándo | Estado |
|---|-------|------|--------|--------|
| 1 | Spike técnico | Implementar (código desechable) | Antes de `arquitectura-base` | por valorar |
| 2 | Stack concreto | Definir | Ahora | hecho (`docs/stack.md`) |
| 3 | Esqueleto de solución y CI | Implementar | Primer paso de `arquitectura-base` | por valorar |
| 4 | Convenciones de código | Definir | Ahora | hecho (`docs/convenciones.md`) |
| 5 | Glosario castellano, catalán e inglés | Definir | Ahora | hecho (`docs/glosario.md`); términos por validar con los conserjes |
| 6 | Catálogo de códigos de error y claves | Definir (convención) e implementar (contenido) | Convención ahora | convención hecha (`docs/convenciones.md`, sección 3); contenido al implementar |
| 7 | Mapa de dependencias y primer hito vertical | Definir | Ahora | por valorar |
| 8 | Reglas de trabajo con `/opsx:apply` | Definir | Ahora | por valorar |
| 9 | Definición de terminado | Definir | Ahora | por valorar |
| 10 | Datos de ejemplo | Definir (contenido) e implementar (generador) | Contenido ahora | por valorar |
| 11 | Maquetas de pantallas clave | Definir | Ahora, con los conserjes | por valorar |
| 12 | Registro de riesgos y pendientes externos | Definir | Ahora | por valorar |

Orden recomendado: 2 → 4 → 5 → 6 (convención) → 9 → 8 → 7 → 12 → 10 (contenido) → 11, y después el spike (1) y el esqueleto (3) al arrancar la implementación.

---

## 1. Spike técnico

**Tipo:** implementar, con código desechable en una rama que no se fusiona.
**Por qué:** las specs no pueden validar supuestos técnicos. Si alguno falla, cambia el diseño y conviene saberlo antes de construir encima.

**Supuestos a comprobar** (cada uno con criterio de éxito):

| Supuesto | Viene de | Criterio de éxito |
|----------|----------|-------------------|
| EF Core con SQLite3 Multiple Ciphers en formato SQLCipher 4, migraciones envueltas y `PRAGMA integrity_check` | `arquitectura-base` | Crear, migrar y reabrir una base cifrada en los tres sistemas |
| Copia en línea de la base cifrada con la misma clave | `copies-de-seguretat` | Copia consistente mientras la aplicación escribe, verificada y abrible |
| Bloqueo de instancia única asociado al fichero | `arquitectura-base` | La segunda instancia no abre la base en los tres sistemas |
| Firma y verificación Ed25519 con NSec en .NET | `llicencies-client` | Verificar una clave generada fuera |
| Huella de equipo estable | `llicencies-client` | Mismo valor en reinicios, distinto entre equipos, en los tres sistemas |
| Arrastrar y soltar en Avalonia | `ux-fonaments` | Arrastrar un elemento a otro con resalte del destino y cancelación con Escape en los tres sistemas |
| Lista virtualizada y mapa de 300 taquillas | `ui-shell` | Desplazamiento fluido y actualización de una sola taquilla |
| Renderizado sin ventana para pruebas de vista | `ux-fonaments` | Una prueba de enlace y foco ejecutable en CI |
| Comparación de texto catalana (`ç`, `l·l`, `ny`) | `arquitectura-base` | Orden y búsqueda sin acentos correctos en los tres sistemas |

**A decidir:** dónde se hace (rama descartable o repositorio aparte), quién prueba en Windows y cuánto tiempo se le da (propuesta: una tarde por bloque, sin pulir).

---

## 2. Stack concreto

**Tipo:** definir. Documento `docs/stack.md`.
**Por qué:** cada cambio da por hecho versiones y paquetes; sin fijarlos, cada uno decide por su cuenta.

**A decidir:**

| Elemento | Opciones o criterio |
|----------|---------------------|
| Versión de .NET | LTS vigente al empezar |
| Versión de Avalonia | Última estable compatible con el renderizado sin ventana |
| SQLite cifrado | SQLite3 Multiple Ciphers en formato SQLCipher 4 (decidido; ver `docs/stack.md`) |
| Lectura de CSV | Biblioteca consolidada tras `ICsvReader` (`taquilles-i-zones`) |
| Firma de licencias | Ed25519 con la biblioteca criptográfica de .NET o una externa |
| Pruebas | Marco de pruebas, biblioteca de aserciones, dobles y pruebas de arquitectura |
| Inyección de dependencias y MVVM | Microsoft.Extensions.* y biblioteca MVVM |
| Registro técnico | Biblioteca de registro con rotación |
| Instalador | Inno Setup (ya decidido en `arquitectura-base`) |
| CI | Servicio con matriz Windows, Linux y macOS |

Cada fila con versión, motivo y alternativa descartada.

---

## 3. Esqueleto de solución y CI

**Tipo:** implementar. Primer paso real de `arquitectura-base`.
**Por qué:** todo lo demás se apoya en ello y es lo que permite verificar en los tres sistemas desde el primer día.

**Contenido:**
- Proyectos: `Arca.Domain`, `Arca.Application`, `Arca.Infrastructure`, `Arca.Desktop` y un proyecto de pruebas por capa, más `Arca.UI` para los componentes de `ux-fonaments`.
- Pruebas de arquitectura de referencias entre capas.
- Canalización de CI con la matriz de tres sistemas, que compila y ejecuta las pruebas.
- "Hola mundo" de Avalonia que arranca en los tres sistemas.

**A decidir:** nombre exacto de los proyectos y carpetas (ligado al punto 4).

---

## 4. Convenciones de código

**Tipo:** definir. Sección en `AGENTS.md` o `docs/convenciones.md`.
**Por qué:** muchas specs dicen "como en `taquilles-i-zones`"; conviene que eso sea un ejemplo concreto y no una interpretación.

**A definir, cada una con un ejemplo corto:**

1. Estructura de carpetas por capa y por capacidad.
2. Forma del **resultado estructurado**: éxito con datos y recuentos, avisos y error con código estable.
3. Convención de **códigos de error** (por ejemplo `LOCKER_NUMBER_IN_USE`).
4. Convención de **claves de recurso** (por ejemplo `Lockers.Errors.NumberInUse`).
5. Patrón de **caso de uso**: entrada, resultado, atributo de licencia (`RequiresWriteLicense` o `AlwaysAvailable`).
6. Patrón de **operación en dos fases** (análisis, plan inmutable, confirmación con revalidación, transacción única).
7. Patrón de **historial de solo añadir**: evento con código de tipo y valores estructurados, sin texto traducido.
8. Patrón de **estado derivado** como función pura.
9. Reglas de **privacidad en el código**: objetos de transferencia sin correo ni identificador, nada de datos de alumnos en el registro.
10. Estilo de **pruebas**: nombres, dobles, datos de ejemplo y pruebas de integración con SQLite cifrado temporal.

---

## 5. Glosario castellano, catalán e inglés

**Tipo:** definir. Documento `docs/glosario.md`.
**Por qué:** las specs están en castellano, la interfaz en catalán y el código en inglés; sin glosario, cada cambio traduce a su manera.

**Formato:** término de spec (castellano) · texto de interfaz (catalán) · identificador de código (inglés) · nota.

**Términos que ya piden decisión:**

| Castellano | Catalán | Inglés | Nota |
|------------|---------|--------|------|
| taquilla | taquilla | `Locker` | |
| curso en cierre / cerrado | curs en tancament / tancat | `Closing` / `Closed` | Estados de `SchoolYear` |
| fianza | fiança | `Deposit` | Única por estancia |
| cuota | quota | `Fee` | Anual |
| cargo | càrrec | `Charge` | |
| condonado / exento | condonat / exempt | `Waived` / `Exempt` | Distinguir de anulado |
| llave | clau | `Key` | No confundir con clave de licencia ni de cifrado |
| clave de licencia | clau de llicència | `LicenseKey` | |
| avería / en mantenimiento | avaria / en manteniment | `Broken` / `UnderMaintenance` | Tipos de fuera de servicio |
| asignación | assignació | `Assignment` | |
| matrícula | matrícula | `Enrollment` | Por alumno y curso |
| baja / de baja | baixa / de baixa | `Retired` / `Withdrawn` | Taquilla frente a alumno |
| gracia / solo lectura | gràcia / només lectura | `Grace` / `ReadOnly` | Licencia frente a curso |
| revisión previa / plan | revisió prèvia / pla | `Preview` / `Plan` | Operaciones en dos fases |

**A decidir:** el término catalán definitivo de "cierre de curso", "de baja" y "condonado", que deberían validar los conserjes.

---

## 6. Catálogo de códigos de error y claves de recurso

**Tipo:** definir la convención ahora; el contenido se rellena al implementar cada cambio.
**Por qué:** las pruebas de "todas las claves existen en catalán" y la traducción de códigos de error necesitan una estructura acordada.

**A definir:**
- Formato del código de error y de la clave de recurso (ligado al punto 4).
- Fichero o carpeta donde vive el catálogo y cómo se genera o comprueba.
- Cómo se enlaza un código con su clave, sus parámetros y su gravedad (error, aviso).
- Plantilla para que cada cambio añada las suyas.

**Se rellena al implementar:** los códigos que ya citan las specs (número en uso, zona no disponible, curso no activo, llave no disponible, motivo obligatorio...).

---

## 7. Mapa de dependencias y primer hito vertical

**Tipo:** definir. Sección en `docs/roadmap.md`.
**Por qué:** hoy el orden es cambio a cambio, pero conviene un primer resultado usable de extremo a extremo para enseñarlo pronto a los conserjes.

**Hito 1 propuesto:** crear el curso y los importes, dar de alta zonas y taquillas, importar alumnos, asignar una taquilla y ver el mapa.

**A definir:** qué tareas de cada cambio entran en el hito 1 y cuáles quedan para después. Se deduce de las dependencias:

| Cambio | Lo que entra en el hito 1 | Lo que queda para después |
|--------|---------------------------|---------------------------|
| `arquitectura-base` | Capas, base cifrada, migrador, i18n, resultado estructurado, CI | Distribución e instalador |
| `taquilles-i-zones` | Zonas, taquillas, estado derivado, alta por rangos | Importación de taquillas, historial completo |
| `alumnes-i-assignacions` | Curso, alumnos, matrícula, asignación | Conciliación completa de la importación |
| `pagaments` | Importes y cargos generados al asignar | Devolución de fianza, bloque |
| `ux-fonaments` | Notificaciones, confirmación, comando de ejecución única | Adaptabilidad, atajos completos |
| `ui-shell` | Marco, mapa de taquillas | Identidad, búsqueda global completa |

**A decidir:** si el hito 1 incluye la importación de alumnos (depende del fichero de secretaría) o solo el alta manual.

---

## 8. Reglas de trabajo con `/opsx:apply`

**Tipo:** definir. Sección en `AGENTS.md`.
**Por qué:** escribirlo evita improvisar y mantiene las specs como fuente de verdad.

**A definir:**
- Una rama por cambio (o por hito), con commits por grupo de tareas.
- Tamaño máximo de una sesión de implementación y qué se comprueba al terminar.
- Qué se hace si una spec choca con la realidad: se actualiza la spec primero y se anota en el cambio; no se resuelve solo en el código.
- Cuándo se archiva un cambio (`openspec archive`) y qué implica sincronizar las specs con `openspec/specs/`.
- Cómo se marcan las tareas y qué hacer con las que se descartan o se añaden.
- Revisión antes de fusionar: quién y con qué lista de comprobación.

---

## 9. Definición de terminado

**Tipo:** definir. Lista de comprobación en `AGENTS.md`.
**Por qué:** que "terminado" signifique lo mismo en los 14 cambios.

**Propuesta de lista, por cambio:**
1. Todas las tareas del `tasks.md` marcadas.
2. Pruebas en verde en Windows, Linux y macOS.
3. Pruebas de arquitectura en verde (capas, comando de ejecución única, clasificación de licencia).
4. Todas las claves de recurso nuevas existen en catalán.
5. Ningún dato de alumnos en el registro técnico (prueba de privacidad).
6. `openspec validate` pasa y las specs reflejan lo implementado.
7. Cambio archivado.

---

## 10. Datos de ejemplo

**Tipo:** definir el contenido ahora; el generador se implementa después.
**Por qué:** sirve para pruebas, para demostraciones y para el primer contacto con los conserjes, sin datos reales de menores.

**Contenido a definir:**
- Un centro ficticio con 300 a 1000 taquillas repartidas en zonas.
- Unos 2000 alumnos con nombres catalanes (`ç`, `l·l`, `ny`), homónimos, niveles y grupos.
- Situaciones: deudas de cursos anteriores, fianzas pagadas, exentas y por devolver, llaves pendientes y perdidas, taquillas averiadas, un curso en cierre y otro activo.
- Varios ficheros CSV de secretaría de prueba con distintos formatos de cabecera, separador y codificación.

**A decidir:** si es un generador reproducible con semilla (recomendado) o ficheros fijos, y dónde vive.

---

## 11. Maquetas de pantallas clave

**Tipo:** definir, con los conserjes.
**Por qué:** es lo que valida la pantalla principal, que sigue abierta, y evita construir interfaz que luego no gusta.

**Pantallas a maquetar** (papel o herramienta de diseño):
1. Mapa de taquillas por zona (pantalla principal provisional) con panel de detalle.
2. Asignar una taquilla a un alumno, con aviso de deuda y entrega de llave.
3. Asistente de cierre de curso.
4. Consulta de morosos y sus filtros.

**A decidir:** herramienta, quién las enseña a los conserjes y qué se les pregunta (¿qué hacen primero al abrir la aplicación?, ¿cómo buscan hoy a un alumno?).

---

## 12. Registro de riesgos y pendientes externos

**Tipo:** definir. Sección en `docs/roadmap.md` o documento propio.
**Por qué:** hoy está disperso en el roadmap y en los diseños de cada cambio.

| Pendiente o riesgo | Bloquea | Responsable | Fecha objetivo | Estado |
|--------------------|---------|-------------|----------------|--------|
| Fichero de muestra de secretaría | Cierre de la importación de alumnos (`alumnes-i-assignacions`) | por definir | por definir | pendiente |
| Valoración RGPD con la dirección (identificador, correo, conservación) | Política de datos de `alumnes-i-assignacions` y `cursos-i-historial` | por definir | por definir | pendiente |
| Datos del proyecto de licencias | Documento de requisitos del servidor, contrato de `llicencies-client` | por definir | por definir | pendiente |
| Validar la pantalla principal con los conserjes | Implementación definitiva de Inicio (`ui-shell`) | por definir | por definir | pendiente |
| Política de conservación de datos con los conserjes | Automatización futura de `cursos-i-historial` | por definir | por definir | pendiente |
| Supuestos técnicos sin probar | Fiabilidad de `arquitectura-base`, `copies-de-seguretat`, `ux-fonaments` | por definir | tras el spike (punto 1) | pendiente |
| Informes adicionales que pidan los conserjes | Ampliar `informes-csv` | por definir | por definir | pendiente |

**A decidir:** quién es responsable de cada uno y con qué fecha se reclama.
