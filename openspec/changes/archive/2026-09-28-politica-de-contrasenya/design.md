## Context

Cambio pequeño sobre `acces-i-xifrat` (archivado). La política ya existe (`PasswordPolicy`, D5 de aquel cambio): 12 caracteres, sin reglas de composición, lista de habituales, indicador de fortaleza que avisa sin bloquear. La decisión de producto (D6) es relajar el mínimo y hacer el aviso más útil. La spec vigente es `contrasenya-del-centre`.

## Goals / Non-Goals

**Goals:** permitir contraseñas cortas que la gente recuerde, sin dejar de advertir con claridad; que quien elige una débil sepa por qué, qué es una fuerte y vea un ejemplo.

**Non-Goals:** cambiar el cifrado, la clave de recuperación o la derivación de la llave; reglas de composición; caducidad.

## Decisions

### D1. Mínimo de 6 caracteres, solo al crear o cambiar
`PasswordPolicy.MinimumLength` pasa a 6. Abrir la aplicación nunca comprueba la longitud, así que las contraseñas ya creadas (de 12 o más) no se ven afectadas y no hay migración.

### D2. Débil = menos de 12 caracteres o un criterio de debilidad anterior
El umbral de 12 se conserva como **recomendado** (`RecommendedLength`): por debajo, la contraseña es débil aunque tenga varias palabras. Se mantienen los criterios anteriores (una sola palabra, solo dígitos, cinco símbolos distintos o menos). Una contraseña débil nunca bloquea: la persona decide.
*Alternativa descartada*: bloquear las débiles. Contradice la decisión de producto y el modelo «avisar, no impedir» ya vigente.

### D3. Lo que sí bloquea, con cualquier longitud
La lista de contraseñas habituales y las repeticiones y secuencias obvias siguen rechazándose. Con 6 caracteres cobran más importancia (`123456`, `qwerty`): sin este filtro el mínimo bajo sería casi una invitación a lo peor.

### D4. Aviso con riesgo, criterios y ejemplo
Cuando la contraseña es débil, el formulario muestra, además de «Fortalesa: fluixa»:
1. el **riesgo**: si alguien consigue el fichero de datos, una contraseña débil se puede adivinar;
2. los **criterios** de una fuerte: 12 o más caracteres y varias palabras sin relación;
3. un **ejemplo** (`lluna cotxe formatge radio`) y la indicación de inventar uno propio.
El ejemplo se añade a la lista de habituales para que nadie lo use tal cual. Todo son textos por clave de recurso en catalán; el aviso viaja como el aviso `Keys.PasswordWeak` ya existente, ampliado, y las líneas de criterios y ejemplo son etiquetas del formulario.

### D5. Contador
El contador sigue mostrando «n de m»: `m` es ahora el mínimo (6). El recomendado (12) se explica en los criterios.

## Risks / Trade-offs

- **Una contraseña de 6 caracteres cifra una base con datos de menores** → mitigado por Argon2id (encarece cada intento), por el aviso claro y por la guía de la clave de recuperación; el riesgo se documenta en `docs/riesgos.md`. Es una decisión de producto de la persona responsable.
- **La gente puede ignorar el aviso** → es la decisión: avisar, no impedir.
- **El ejemplo del aviso se copia** → queda en la lista de rechazadas.

## Migration Plan

Sin migración: ningún dato ni fichero cambia.

## Open Questions

Ninguna.
