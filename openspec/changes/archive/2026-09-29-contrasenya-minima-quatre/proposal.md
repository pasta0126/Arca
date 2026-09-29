## Why

La persona responsable quiere bajar aún más la exigencia de la contraseña del centro: al probar y al trabajar con datos de ejemplo, una contraseña corta y sencilla es lo cómodo, y la protección real de los datos la da la clave de recuperación (26 caracteres aleatorios) y el hecho de que los datos no salen del equipo. Decisión tomada el 2026-09-29, a continuación de la D6 de `docs/riesgos.md`.

## What Changes

- El mínimo de la contraseña del centro baja de 6 a **4 caracteres**, al crearla y al cambiarla, de cualquier tipo y sin reglas de composición.
- Las contraseñas de la lista de habituales, las repeticiones y las secuencias **dejan de rechazarse**: pasan a considerarse **débiles** y se avisa con el riesgo, los criterios y el ejemplo, sin impedir continuar (como cualquier contraseña de menos de 12 caracteres).
- El ejemplo de contraseña fuerte del aviso deja de estar prohibido: ya no hay lista que rechace.
- El contador de longitud cuenta sobre el nuevo mínimo (4) y el aviso sigue recomendando 12.
- Las contraseñas ya creadas no cambian ni se revisan: la política solo se aplica al crear o cambiar la contraseña.

## Capabilities

### Modified Capabilities
- `contrasenya-del-centre`: mínimo de 4 caracteres y las contraseñas habituales, repetidas o en secuencia pasan de rechazadas a débiles con aviso.

## Fuera de alcance

- Cambiar la clave de recuperación ni la derivación de la llave (Argon2id).
- Exigir mayúsculas, números o símbolos, o caducidad.

## Impacto

- **Seguridad (riesgo aceptado):** con una contraseña de 4 caracteres el cifrado del fichero de datos depende sobre todo de que el fichero no caiga en manos ajenas; la clave de recuperación sigue siendo fuerte. Se anota en `docs/riesgos.md` (D6).
- **Código:** `PasswordPolicy`, el error `PasswordTooCommon` (se elimina), el texto de los avisos y las pantallas de contraseña; pruebas de política y de pantallas.
