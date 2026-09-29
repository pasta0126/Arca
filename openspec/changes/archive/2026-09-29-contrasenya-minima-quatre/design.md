## Context

`politica-de-contrasenya` dejó un mínimo de 6 caracteres, contraseñas cortas como débiles con aviso, y rechazo de las habituales, repeticiones y secuencias. Este cambio quita el último bloqueo y baja el mínimo.

## Decisions

### D1. Las contraseñas habituales pasan de rechazo a debilidad
La detección (lista de habituales, repetición, secuencia) se conserva, pero solo influye en la fortaleza: cualquier coincidencia es «débil» y muestra el aviso con riesgo, criterios y ejemplo. Así no se pierde la información útil y no se bloquea a nadie. *Alternativa descartada*: borrar la detección; la persona perdería el aviso justo en los casos más adivinables.

### D2. Mínimo de 4 caracteres
Constante única `MinimumLength = 4`. El contador, el mensaje de «demasiado corta» y las pantallas leen esa constante.

### D3. Un error menos
`Keys.PasswordTooCommon` deja de existir (código, recurso y ramas de las pantallas), porque nada lo produce.

## Risks / Trade-offs

- Una contraseña de 4 caracteres se adivina rápido si alguien obtiene el fichero de datos → riesgo aceptado y documentado; el aviso de debilidad lo explica siempre; la clave de recuperación sigue siendo fuerte.

## Migration Plan

Ninguna: las contraseñas existentes no se revisan.
