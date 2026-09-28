## Why

La contraseña del centro exige hoy 12 caracteres como mínimo. En la práctica, al probar la aplicación, la exigencia frena a quien quiere una contraseña corta que recuerde bien, y una contraseña que se olvida es peor para los datos que una corta: sin contraseña ni clave de recuperación no hay datos. La persona responsable ha decidido (decisión D6 de `docs/riesgos.md`) bajar el mínimo, no exigir mayúsculas, números ni símbolos y, en su lugar, **evaluar la fortaleza y avisar con claridad cuando es débil**, con un ejemplo de contraseña fuerte y sus criterios, sin impedir continuar.

## What Changes

- El mínimo de la contraseña del centro baja de 12 a **6 caracteres**, al crearla y al cambiarla. Sigue sin haber reglas de composición.
- Una contraseña de menos de 12 caracteres se considera **débil**, además de las que ya lo eran (una sola palabra, solo dígitos, muy pocos símbolos distintos).
- El aviso de contraseña débil deja de ser una línea suelta: explica **qué riesgo hay** (quien consiga el fichero de datos podría adivinarla), da **los criterios** de una contraseña fuerte (12 o más caracteres, varias palabras sin relación) y muestra **un ejemplo** de contraseña fuerte. Nunca impide continuar.
- Siguen rechazándose las contraseñas de la lista de habituales y las repeticiones y secuencias obvias, con cualquier longitud; el ejemplo que muestra el aviso entra en esa lista para que nadie lo use tal cual.
- El contador de longitud cuenta sobre el nuevo mínimo (6) y el aviso recomienda 12.
- Las contraseñas ya creadas no cambian ni se revisan: la política solo se aplica al crear o cambiar la contraseña, nunca al abrir la aplicación.

## Capabilities

### New Capabilities

<!-- Ninguna. -->

### Modified Capabilities
- `contrasenya-del-centre`: mínimo de 6 caracteres, contraseñas cortas tratadas como débiles y aviso de fortaleza con riesgo, criterios y ejemplo.

## Fuera de alcance

- Cambiar la clave de recuperación (sigue siendo la garantía fuerte: 26 caracteres aleatorios) ni la derivación de la llave (Argon2id).
- Exigir mayúsculas, números o símbolos, o caducidad de la contraseña.
- Medir la fortaleza con estimadores externos o listas descargadas: la evaluación sigue siendo local y sin dependencias.
- Cambios de pantallas más allá de los textos del aviso.

## Impacto

- **Código**: `PasswordPolicy` (mínimo y criterio de débil), lista de contraseñas habituales, textos y líneas de ayuda de los formularios de contraseña nueva (primera ejecución y cambio).
- **Seguridad**: bajar el mínimo reduce el coste de adivinar la contraseña si alguien consigue una copia del fichero de datos. Se acepta como decisión de producto, se compensa con el aviso claro y con que la llave real de la base es aleatoria y la protege Argon2id (que encarece cada intento). Debe constar en `docs/riesgos.md` y en la guía de la clave de recuperación.
- **Datos personales (RGPD)**: sin cambios; la contraseña sigue sin guardarse ni registrarse.
