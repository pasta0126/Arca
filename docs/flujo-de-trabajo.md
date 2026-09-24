# Flujo de trabajo de implementación

Puntos 8 y 9 de `docs/preparacion-desarrollo.md`: cómo se implementa cada cambio de OpenSpec con `/opsx:apply` y cuándo se considera terminado. Aplica a cualquier persona o herramienta de IA que trabaje en el repositorio.

## Decisiones tomadas

| Tema | Decisión | Alternativa descartada |
|------|----------|------------------------|
| Ramas | **Una rama por cambio** y Pull Request a `main` | Trabajar directamente en `main` |
| Commits | **Uno por grupo de tareas** (cada `## N.` de `tasks.md`) | Uno por tarea (ruidoso) o uno por cambio (ilegible) |
| Ritmo de la sesión | **Un grupo de tareas y parar a revisar** | Un cambio entero seguido |
| Choques con las specs | **Parar, actualizar la spec y seguir** | Anotar y corregir las specs al final |

## 1. Ciclo de vida de un cambio

```
specs aprobadas → rama → implementar grupo a grupo → PR → revisión → archivar → fusionar
```

| Fase | Qué ocurre | Quién |
|------|------------|-------|
| Specs aprobadas | El cambio está redactado y validado (`openspec validate`); sus dependencias ya están fusionadas | Persona responsable |
| Rama | Se crea `change/<nombre>` desde `main` actualizado | Quien implementa |
| Implementar | Se trabaja un grupo de tareas cada vez (sección 3) | Quien implementa |
| PR | Se abre el Pull Request con la lista de comprobación de la sección 6 | Quien implementa |
| Revisión | Autorrevisión con `/code-review` y visto bueno de la persona responsable | Ambos |
| Archivar | `openspec archive <nombre>` como **último commit de la rama** | Quien implementa |
| Fusionar | Con los scripts en verde en macOS (Linux descartado por ahora; Windows solo antes de producción) | Persona responsable |

**Orden de los cambios**: el del roadmap (`docs/roadmap.md`). Un cambio empieza cuando sus dependencias están fusionadas. La única excepción es el hito 1 (punto 7 de `docs/preparacion-desarrollo.md`), que reparte tareas de varios cambios y se definirá aparte.

## 2. Ramas y Pull Requests

- Nombre de rama: `change/<nombre-del-cambio>` (por ejemplo `change/arquitectura-base`).
- `main` siempre compila y pasa las pruebas. Nada se fusiona con los scripts en rojo. No hay CI remota por ahora (ver `docs/stack.md`, «Verificación multiplataforma»): la comprobación la hacen los scripts de `build/`.
- El Pull Request se fusiona con **commit de fusión** (no *squash*), para conservar un commit por grupo de tareas en el historial.
- Un Pull Request corresponde a **un cambio de OpenSpec**. Si un cambio es muy grande, se puede dividir en varias ramas por grupos, avisándolo en el PR.
- Los cambios de documentación (`docs/`, `openspec/config.yaml`) que no forman parte de un cambio pueden ir directamente a `main`.

## 3. Sesión de implementación con `/opsx:apply`

Una sesión implementa **un grupo de tareas** (una sección numerada de `tasks.md`) y se detiene.

**Antes de empezar**
1. Leer `AGENTS.md`, `docs/convenciones.md`, `docs/glosario.md`, `docs/stack.md` y los artefactos del cambio (propuesta, diseño, specs y tareas).
2. Comprobar que la rama está al día con `main` y que `build/test.sh` pasa sobre `main`.
3. Elegir el siguiente grupo de tareas sin marcar y confirmar que sus dependencias (grupos anteriores) están hechas.

**Durante**
4. Implementar solo lo que dicen las tareas del grupo, con sus pruebas (un test por escenario del spec, con etiqueta `spec`).
5. No añadir funcionalidad que no esté en las specs. Si parece necesaria, se trata como un choque (sección 4).
6. Marcar cada tarea (`- [x]`) en el mismo commit que la implementa.

**Al terminar el grupo**
7. Ejecutar todas las pruebas afectadas y las de arquitectura, y `openspec validate --all --strict`.
8. Hacer **un commit** con el formato de la sección 5.
9. Mostrar un resumen: qué se hizo, qué pruebas pasan, qué se desvió de las specs y qué grupo toca a continuación.
10. **Parar y esperar el visto bueno** antes de empezar el siguiente grupo.

## 4. Si una spec choca con la realidad

Ocurrirá (una API no se comporta como se supuso, dos requisitos se contradicen, falta un caso). Las specs son la fuente de verdad, así que **no se resuelve solo en el código**:

1. **Parar** la tarea y no escribir código que contradiga la spec.
2. **Describir el choque**: qué dice la spec, qué ocurre en realidad y qué opciones hay.
3. **Proponer el cambio** a la spec, al diseño o a las tareas (con `/opsx:update`), indicando las consecuencias en otros cambios.
4. **Validar con la persona responsable** antes de tocar nada.
5. **Actualizar los artefactos** y anotar la decisión en la sección *Cambios durante la implementación* al final de `design.md` del cambio, con fecha y motivo.
6. Hacer el cambio de especificación en un **commit propio** con prefijo `spec:` (ver sección 5) y continuar.
7. Si el choque afecta a **otro cambio ya fusionado o redactado**, se actualizan también sus artefactos y se avisa en el PR.

Tareas que se descartan o se añaden: se edita `tasks.md`, se indica el motivo en la misma sección de `design.md` y se sigue el mismo camino de validación.

## 5. Commits

- Idioma: **inglés**, en imperativo, primera línea de 72 caracteres como máximo.
- Formato: `<cambio>: <resumen> (grupo N)`.
- El cuerpo lista las tareas completadas y cualquier desviación.
- Si participa una herramienta de IA, el mensaje termina con la línea de coautoría que corresponda.

Ejemplos:

```
arquitectura-base: add layered solution and architecture tests (group 1)
taquilles-i-zones: add locker persistence and migration (group 7)
spec: allow releasing assignments of a closing course (cursos-i-historial)
docs: update glossary with validated terms
```

Prefijos: nombre del cambio para implementación, `spec:` para cambios de especificación, `docs:` para documentación y `build:` para los scripts de verificación y empaquetado.

## 6. Revisión antes de fusionar

**Autorrevisión** (quien implementa): ejecutar `/code-review` sobre el diff del PR y resolver lo que corresponda.

**Lista de comprobación del PR** (la de `docs/convenciones.md` más esta):
- [ ] Todas las tareas del cambio están marcadas y ninguna queda a medias.
- [ ] `build/test.sh` pasa en macOS (Linux y Windows: ver la decisión de alcance en `docs/stack.md`).
- [ ] No hay funcionalidad fuera de las specs.
- [ ] Las desviaciones están anotadas en `design.md` y las specs están actualizadas.
- [ ] No se ha añadido ninguna dependencia sin pasar el control de licencias (coste cero).
- [ ] El PR indica qué se probó manualmente y en qué sistema.

**Visto bueno** de la persona responsable, que puede pedir cambios. Con los scripts en verde y el visto bueno, se fusiona.

## 7. Definición de terminado

### Por grupo de tareas (cada commit)
1. Las tareas del grupo están marcadas.
2. Sus pruebas pasan, incluidas las de arquitectura.
3. `openspec validate --all --strict` pasa.
4. Las claves de recurso nuevas existen en catalán.
5. No hay funcionalidad fuera de las specs.

### Por cambio (antes de fusionar)
1. **Todas las tareas** de `tasks.md` están marcadas.
2. **Los scripts de verificación pasan**: `build/test.sh` en macOS, con las pruebas de dominio, aplicación, persistencia, interfaz y arquitectura. Linux queda descartado por ahora y la verificación de Windows se hace solo antes de salir a producción (`docs/stack.md`).
3. **Cada escenario de los specs del cambio tiene su prueba** con la etiqueta `spec` (cuando exista el script de cobertura, lo comprueba solo; hasta entonces, se revisa a mano).
4. **Pruebas de arquitectura en verde**: capas, comando de ejecución única, DTO sin correo ni identificador y sin EF Core en `Domain` ni `Application`.
5. **Todas las claves de recurso** usadas existen en catalán y no queda ningún texto literal en el código ni en las vistas.
6. **Prueba de privacidad**: un error con datos de un alumno no deja rastro en el registro técnico.
7. **Control de licencias de dependencias** en verde (coste cero).
8. **Specs y documentación sincronizadas**: `openspec validate --all --strict` pasa y `docs/convenciones.md` o `docs/glosario.md` reflejan cualquier convención o término nuevo.
9. **Prueba manual** del flujo principal del cambio en macOS. La prueba en Windows con el paquete portable `win-x64` se hace antes de salir a producción.
10. **Cambio archivado** con `openspec archive <nombre>` y specs sincronizadas con `openspec/specs/`.
11. **Pull Request fusionado** y rama borrada.

### Por hito (cuando exista interfaz que enseñar)
Además: instalador y versión portable generados, demostración con los datos de ejemplo y revisión con los conserjes de lo que sea visible.

## 8. Cuando la verificación falla en un solo sistema

- No se fusiona. Un fallo en un solo sistema es un fallo real (rutas, mayúsculas de ficheros, fin de línea, cultura, binarios nativos).
- Se reproduce en ese sistema con el script, se corrige y se añade una prueba que lo cubra.
- Si el fallo revela un supuesto erróneo de las specs, se sigue la sección 4.

## 9. Pendientes externos durante la implementación

Los pendientes de `docs/roadmap.md` (fichero de secretaría, valoración RGPD, datos del servidor de registro, validación de la pantalla principal) **no bloquean empezar** los cambios que no dependen de ellos.

- Cada tarea que dependa de un pendiente se marca en `tasks.md` con `(bloqueada: <motivo>)` y se salta sin dejar código a medias.
- Cuando llega el dato, se actualiza la spec afectada (sección 4) y se desbloquea la tarea.
- No se inventa un formato o una decisión para salir del paso: se deja documentado y se avanza con otra tarea.
