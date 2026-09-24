# Hito 1: el trabajo diario del conserje

Punto 7 de `docs/preparacion-desarrollo.md`. Primer resultado usable de extremo a extremo, pensado para **enseñarlo pronto a los conserjes** y validar la pantalla principal antes de construir el resto.

## Objetivo

Que un conserje pueda, sobre datos de ejemplo, hacer **el trabajo de un día normal**: configurar el curso y los importes, ver el mapa de taquillas, asignar taquillas a alumnos (también arrastrando), cobrar y buscar. Sin importar ficheros, sin llaves, sin cierre de curso.

## Guion de demostración (criterio de éxito)

El hito está terminado cuando este guion se completa sin ayuda técnica, con el teclado y con el ratón, en macOS y en Windows:

1. **Arrancar** la aplicación portable con la base de datos creada y cargada con el perfil `demo` de `docs/datos-de-ejemplo.md` (600 taquillas en 6 zonas, 900 alumnos, un curso activo con importes y deuda de un curso anterior).
2. **Ver el mapa** de taquillas por zona con su estado (libre, ocupada, reservada, averiada, en mantenimiento), contadores y marca de deuda.
3. **Buscar** "garcia" y ver a los alumnos con su taquilla y su estado de pago.
4. **Asignar** un alumno sin taquilla arrastrándolo a una taquilla libre. Comprobar el aviso si tiene deuda de un curso anterior y que se generan la cuota y el dipòsit.
5. **Asignar por menú y teclado** a otro alumno, con el mismo resultado.
6. **Cobrar**: marcar la cuota como pagada y ver desaparecer la marca de deuda en el mapa. Marcar otro cargo como exento y otro como condonado, con motivo.
7. **Liberar** una taquilla y **cambiar** a un alumno de taquilla.
8. **Marcar una taquilla como averiada** estando ocupada y elegir mantener, reasignar o liberar.
9. **Dar de alta** una zona y 40 taquillas por rango, con vista previa y confirmación.
10. **Dar de alta un alumno** manualmente.
11. **Definir los importes** de un curso nuevo (proponiendo los del anterior).
12. **Cerrar y reabrir** la aplicación: todo se conserva. Tras un error inesperado se ve un mensaje comprensible con referencia.

## Alcance

### Dentro

- Cimientos: capas, base cifrada con la contraseña del centro y su clave de recuperación (`acces-i-xifrat`), migraciones, i18n, resultado estructurado, arranque, registro y CI en tres sistemas.
- Zonas y taquillas (estado derivado, avería, reserva, baja, historial, alta por rangos).
- Curso escolar y alumnos con **alta manual**, matrícula, niveles y grupos, baja y reactivación.
- Asignación, cambio y liberación, reserva, y decisiones al averiar una taquilla ocupada.
- **Cobros básicos**: importes por curso, cuota y dipòsit generados al asignar, marcar pagado, exento o condonado, deuda de cursos anteriores con aviso, estado de pago.
- Componentes de UI mínimos (feedback, atajos, arrastrar y soltar, lista virtualizada).
- Marco de la aplicación, **búsqueda global básica** (alumno y taquilla) y **mapa de taquillas** como pantalla de inicio.
- Paquete **portable** para ejecutarlo en los equipos de los conserjes.
- Herramienta de desarrollo con **datos ficticios** reproducibles (`docs/datos-de-ejemplo.md`).

### Fuera (hito 2 en adelante)

Importación de alumnos y de taquillas (dependen del fichero de secretaría), llaves, devolución de dipòsit y operaciones en bloque, incidencias y mantenimiento en bloque, informes, cierre de curso y conservación, copias de seguridad, registro y avisos de versión, configuración guiada, identidad del centro y tema, instalador de Windows.

## Reparto por cambio

Cada cambio de OpenSpec **fusiona solo los grupos del hito 1** y **no se archiva** hasta terminarse del todo (ver "Cómo se fusiona").

| Cambio | Entra en el hito 1 | Se difiere |
|--------|--------------------|------------|
| `acces-i-xifrat` | **Entero**: contraseña del centro, clave de recuperación, fichero de claves, apertura, copias y restauración | Nada |
| `arquitectura-base` | Grupos 1 a 8, más 9.1 (CI), 9.1b (control de licencias) y 9.2 (paquete portable) | 9.3 a 9.5 (instalador, actualización, documento del cifrado) |
| `taquilles-i-zones` | Grupos 1 y 2, 3 (sin `ICsvReader`), 4.1 a 4.3 (alta por rangos), 5.1 a 5.4, 5.6 y 5.7, 6 (sin la parte de importación) y 7 | 4.4 a 4.9 (importación CSV), 5.5 (`ICsvReader`) y las partes de CSV de 3.1, 6.2, 6.3 y 6.6 |
| `alumnes-i-assignacions` | Grupos 1 a 5 (curso, alumnos, asignaciones, ocupación real), 7, 8 y 9.1 a 9.4 | Grupo 6 (importación de alumnos) y 9.5 |
| `pagaments` | Grupos 1 y 2, 3.1 a 3.3 y 3.5 a 3.7, 4.1 a 4.5, 6.1, 6.2, 6.5, 6.6, 7 y 8 y 9.1 a 9.4 (sin devolución) | 3.4 (reposición de llave), 4.6 a 4.8 (devolución), grupo 5 (bloque), 6.3 y 6.4 (listas) |
| `ux-fonaments` | Grupos 1 y 2, 3.1, 3.2 y 3.4, grupos 4 y 5 y 7, y 6.1 | 3.3, 3.5 y 6.2 a 6.5 (selección múltiple, planes y pasos) |
| `ui-shell` | Grupos 1, 2.1 a 2.4 (solo curso y cobros, sin asistente), 3 (alumno y taquilla), 5, 6 y 7 | Grupo 4 (identidad y tema), y búsqueda por grupo |
| `pantalles-de-domini` (cambio 15, redactado, sin maquetas) | Pantallas de curso e importes, zonas, taquillas, alumnos y cargos | Pantallas de llaves, incidencias, informes, cierre y copias |
| No incluidos | `claus`, `incidencies`, `manteniment`, `cursos-i-historial`, `informes-csv`, `copies-de-seguretat`, `registre-i-actualitzacions`, `configuracio-inicial` | Todo su contenido |

## Orden de las etapas

```
Etapa 0   Preparación técnica   spike (1) + esqueleto y CI (3)
Etapa 1   Cimientos             arquitectura-base + acces-i-xifrat
Etapa 2   Núcleo sin pantallas  taquilles-i-zones → alumnes-i-assignacions → pagaments
Etapa 3   Interfaz              ux-fonaments → ui-shell + pantallas de dominio
Etapa 4   Demostración          datos de ejemplo + guion + prueba con conserjes
```

- Las **etapas 1 y 2 no necesitan pantallas**: se prueban con pruebas automáticas, así que se pueden implementar mientras se decide lo de la interfaz.
- La **etapa 3 necesita** que estén definidas las pantallas de dominio (ver más abajo) (sin maquetas: punto 11 descartado).
- Dentro de cada etapa se sigue `docs/flujo-de-trabajo.md`: un grupo de tareas por sesión.

## Cómo se fusiona

Como el hito reparte tareas de varios cambios (decisión tomada):

- Cada cambio se fusiona con **Pull Request parcial**: solo los grupos del hito 1, con sus tareas restantes sin marcar en `tasks.md`.
- Un cambio **no se archiva hasta completarse** (`openspec archive`), en un hito posterior.
- Cada PR indica en su descripción qué tareas del cambio entran y cuáles quedan, con la lista anterior.

## Desviaciones temporales respecto a las specs

El hito 1 no implementa todo lo que las specs dicen. Estas diferencias son **intencionadas y desaparecen** al llegar los cambios que faltan:

| Comportamiento del hito 1 | Comportamiento final | Lo resuelve |
|---------------------------|----------------------|-------------|
| La primera ejecución solo pide la contraseña, muestra la clave de recuperación y crea la base en la carpeta por defecto | Primera ejecución completa: elegir carpeta, restaurar una copia y registro opcional | `configuracio-inicial` |
| Sin registro ni avisos de versión | Registro opcional y aviso de versión nueva | `registre-i-actualitzacions` |
| Cabecera con el nombre de la aplicación, sin identidad del centro | Nombre, logo y color del centro | `ui-shell` grupo 4 |
| Tema por defecto del sistema, sin selector | Claro, oscuro o del sistema con acento | `ui-shell` grupo 4 |
| Datos cargados por una herramienta de desarrollo | Importación desde el fichero de secretaría | `alumnes-i-assignacions` grupo 6 |
| Los ganchos de llaves no tienen implementación (una taquilla siempre tiene "llave disponible") | Estado de la llave | `claus` |
| Dipòsit sin devolución desde la interfaz | Lista y devolución individual y en bloque | `pagaments` grupos 4 y 5 |

## Hallazgo: las pantallas de dominio no tienen dueño

Al repartir las tareas se ve que **nadie especifica ni implementa las pantallas concretas** de dominio: formulario y lista de zonas, taquillas, alumnos, cargos, importes y curso. Cada cambio de dominio las deja fuera ("Pantallas: `ui-shell`, `ux-fonaments`"), y `ui-shell` solo cubre el marco, la búsqueda, el tema y el mapa de inicio, con la tarea 1.4 "repartir las pantallas en las ocho secciones" pero sin crearlas.

Consecuencia: **la etapa 3 no se puede implementar sin decidirlo antes.** Las etapas 1 y 2 no se ven afectadas.

Opciones:
1. **Crear un cambio nuevo** `pantalles-de-domini` (cambio 15) con specs, diseño y tareas de esas pantallas, apoyado en los escenarios que ya existen en las specs de dominio.
2. **Ampliar `ui-shell`** con esas pantallas en lugar de un cambio nuevo, lo que lo hace mucho mayor.
3. **Especificarlas al implementar**, pantalla a pantalla. Es lo que más riesgo de improvisación tiene.

**Decidido:** opción 1. Se creará el cambio **`pantalles-de-domini`** (cambio 15), que se redacta **sin maquetas** (punto 11 descartado) y antes de la etapa 3. Mientras tanto avanzan las etapas 1 y 2. Registrado en `docs/riesgos.md` (P1 y D1) y en `docs/roadmap.md`.

## Dependencias externas del hito 1

Ninguna bloquea el hito 1. El fichero de secretaría, el RGPD y el servidor de registro afectan a cambios posteriores. **Sí** hace falta:

- Validar con los conserjes los términos del glosario antes de que se vean textos (etapa 4).
- Redactar `pantalles-de-domini` antes de la etapa 3 (sin maquetas).
- Un equipo Windows para las comprobaciones puntuales.

## Después del hito 1

Propuesta de hito 2 (a confirmar tras la demostración): importación de alumnos y de taquillas (con el fichero de secretaría), llaves, dipòsit completo, cierre de curso e informes. Después, copias de seguridad, registro y avisos de versión, configuración guiada, identidad e instalador.
