# AGENTS.md

Instrucciones para cualquier agente de IA o herramienta que trabaje en este repositorio
(formato estándar abierto; no depende de ninguna herramienta concreta).

## Qué es este proyecto
ARCA: aplicación de escritorio multiplataforma (.NET + Avalonia) para que los conserjes
de un centro educativo gestionen taquillas, alumnos asignados y pagos.

## Fuente de verdad
- **Contexto, decisiones y convenciones**: `openspec/config.yaml` (campo `context`).
- **Especificaciones vigentes**: `openspec/specs/`.
- **Cambios en curso**: `openspec/changes/` (cada uno con proposal, design, specs y tasks).
- Léelos antes de proponer o escribir nada. Si algo contradice una spec, se discute y se
  actualiza la spec; no se resuelve en el código.

## Flujo de trabajo (OpenSpec, spec-driven)
1. Explorar o discutir la idea sin escribir código.
2. Proponer un cambio: `openspec new change <nombre>` y completar sus artefactos.
3. Validar: `openspec validate <nombre>`.
4. Implementar solo cuando las specs del cambio estén aprobadas por la persona responsable.
5. Archivar el cambio al terminar: `openspec archive <nombre>`.

Los skills de flujo están en `.agents/skills/` (compartidos) y `.claude/` (Claude Code).
Cualquier herramienta puede seguir los mismos pasos con la CLI `openspec`.

## Reglas clave (resumen; el detalle está en la config)
- Ningún texto de interfaz escrito a fuego: todo por claves i18n. v1 solo en catalán.
- La lógica de negocio vive en una biblioteca sin dependencias de UI.
- Nunca enviar datos de alumnos fuera del equipo.
- Los pagos no se editan ni se borran: se anulan con motivo.
- Exportar y hacer copia de seguridad deben estar siempre disponibles, incluso sin licencia.
- Debe funcionar en Windows, Linux y macOS.
- Coste cero, ahora y en el futuro: solo librerías, herramientas y servicios gratuitos y con licencia permisiva; nada que haya que pagar ni que pueda pasar a serlo. Detalle en `docs/stack.md`.
- Toda acción del usuario da feedback claro (resultado, progreso, errores comprensibles); los principios
  de UX obligatorios están en `openspec/config.yaml`, sección "UX transversal".

## Idiomas
- Documentación y specs del proyecto: castellano.
- Interfaz de usuario: catalán.
- Código (identificadores, tests, commits): inglés.

## Fuera de este repositorio
El servidor de licencias es otro proyecto. Aquí solo vive el cliente de licencias y el
documento de requisitos para ese servidor (`docs/`).
