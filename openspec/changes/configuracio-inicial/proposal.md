## Why

Una instalación nueva no sirve de nada hasta tener un curso, unos importes, unas zonas y unas taquillas, y hoy cada cosa está en una pantalla distinta. Los conserjes no son técnicos: necesitan una guía que les diga qué falta, en qué orden y para qué, que no les obligue a tenerlo todo a mano el primer día y que se pueda retomar o reabrir más adelante, por ejemplo al empezar un curso nuevo.

## What Changes

- Primera ejecución sin base de datos: elegir empezar de cero o restaurar una copia de seguridad de otro equipo, y ver o cambiar la carpeta de datos antes de crear nada.
- Asistente de configuración guiada con estos pasos: curso escolar, importes del curso, zonas, taquillas, alumnos y licencia.
- Obligatorios solo el curso activo y los importes; el resto es opcional y se puede hacer después.
- Cada paso guarda lo suyo en el momento y reutiliza las pantallas y validaciones existentes: alta y activación del curso, importes, zonas, alta por rangos o importación de taquillas, importación de alumnos con su revisión previa y activación de la licencia o continuación con la prueba.
- El estado de cada paso se deduce de los datos ya creados: pendiente, hecho u omitido. Solo se guardan las omisiones y el descarte del asistente.
- El asistente se reabre en el siguiente paso pendiente en cada arranque mientras falten pasos obligatorios, y se puede volver a abrir desde ajustes en cualquier momento.

## Capabilities

### New Capabilities
- `primera-execucio`: arranque sin base de datos, elección entre empezar de cero o restaurar y carpeta de datos.
- `assistent-de-configuracio`: pasos, estado derivado, obligatoriedad, reanudación, reapertura y comportamiento de cada paso.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Orquesta casos de uso definidos en otros cambios sin cambiar sus reglas. -->

## Fuera de alcance

- Lógica de negocio propia: las reglas de cursos, importes, zonas, taquillas, alumnos y licencia son de sus cambios.
- Identidad del centro (nombre, logo, color) y tema: pertenecen a `ui-shell`.
- Idioma: en v1 solo catalán, sin paso de selección.
- Motivos de incidencia: la lista viene con valores iniciales editables desde ajustes, sin paso propio.
- Asistente de cierre de curso (`cursos-i-historial`), que sigue siendo independiente.
- Pantallas y componentes visuales del asistente (`ux-fonaments`, `ui-shell`).

## Impacto

- **Código**: modelo de pasos y estado derivado en Application, marcadores de omisión y descarte en Domain y Infrastructure, detección de primera ejecución en el arranque; reutiliza los casos de uso de otros cambios.
- **Datos personales (RGPD)**: el asistente no guarda datos de alumnos propios: los que se importen pasan por la importación existente y sus salvaguardas. Los marcadores no contienen datos personales y el registro técnico no recibe datos de alumnos.
- **Depende de**: `arquitectura-base`, `taquilles-i-zones`, `alumnes-i-assignacions`, `pagaments`, `copies-de-seguretat` y `llicencies-client`.
