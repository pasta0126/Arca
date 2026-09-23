## Why

Los cambios anteriores fijan qué comportamiento debe tener la interfaz (feedback constante, confirmaciones con consecuencia, operaciones masivas en dos fases, asistentes de pasos, asignar arrastrando) pero no cómo se construye. Sin un conjunto común de componentes cada pantalla lo resolvería a su manera y la aplicación sería incoherente para conserjes que necesitan predecibilidad. Este cambio crea esos cimientos reutilizables antes de diseñar las pantallas.

## What Changes

- Componentes de feedback que materializan los requisitos de `arquitectura-base`: notificaciones no bloqueantes, diálogo de confirmación con consecuencia, indicador de trabajo y progreso con recuentos, estados vacíos y de carga, y presentación de errores con referencia.
- Protección común contra doble ejecución: un comando que se deshabilita mientras se ejecuta.
- Adaptabilidad: tamaño mínimo de ventana, escalado por DPI, disposición que se adapta al ancho y secciones colapsables cuyo estado se recuerda en el equipo.
- Teclado y menús: orden de foco y foco visible, un conjunto pequeño y fijo de atajos iguales en todas las pantallas y sistemas, atajos visibles junto a cada acción y menús contextuales con las mismas acciones que los botones.
- Arrastrar y soltar solo para asignar un alumno a una taquilla libre, con alternativa de teclado y de menú, mismas validaciones y avisos, y respuesta visual del destino.
- Lista con virtualización, orden, filtro y selección múltiple editable para las operaciones masivas en dos fases.
- Componente genérico de pasos con estado (pendiente, hecho, omitido), progreso "N de M" y navegación, reutilizable por el cierre de curso y la configuración guiada.
- Los componentes toman colores y tipografías de recursos de tema y todos sus textos de claves de recurso, para que `ui-shell` pueda definir el tema y la identidad del centro sin tocarlos.

## Capabilities

### New Capabilities
- `components-de-feedback`: notificaciones, confirmaciones, trabajo y progreso, estados vacíos y de carga, errores y protección contra doble ejecución.
- `adaptabilitat-i-disposicio`: tamaño de ventana, DPI, disposición adaptable, secciones colapsables y preferencias locales de interfaz.
- `teclat-i-menus`: foco, atajos, menús contextuales y equivalencia entre acciones.
- `arrossegar-i-deixar-anar`: asignación arrastrando un alumno sobre una taquilla, con alternativas.
- `llistes-i-passos`: listas con selección editable y componente de pasos.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Implementa como componentes los comportamientos que ya exige `arquitectura-base` (feedback-operacions). -->

## Fuera de alcance

- Tema claro y oscuro, identidad del centro (logo y color), navegación, pantalla principal y pantallas concretas (`ui-shell`).
- Accesibilidad más allá de la que ofrece Avalonia de serie: no hay requisitos propios de lector de pantalla en v1; sí operación completa con teclado y foco visible, que ya exige la config.
- Arrastrar y soltar en otros casos, atajos personalizables y diseño táctil.
- Idiomas distintos del catalán (los componentes ya usan claves de recurso).
- Reglas de negocio: los componentes no las contienen.

## Impacto

- **Código**: nueva biblioteca de componentes de UI (controles, modelos de vista y servicios de notificación y diálogo) que depende de Application solo a través de interfaces y de sus resultados estructurados; sin dependencias de Domain ni de Infrastructure.
- **Datos personales (RGPD)**: los componentes no guardan datos de alumnos. Las preferencias locales (secciones colapsadas, tamaño de ventana) no contienen datos personales, y las notificaciones y los errores no incluyen nombres en el registro técnico.
- **Depende de**: `arquitectura-base` y, para los patrones que materializa, `alumnes-i-assignacions`, `cursos-i-historial` y `configuracio-inicial`.
