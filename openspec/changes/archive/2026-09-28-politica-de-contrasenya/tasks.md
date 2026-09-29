## 1. Política

- [x] 1.1 Bajar `PasswordPolicy.MinimumLength` a 6, añadir `RecommendedLength` (12) y tratar como débil toda contraseña por debajo de él, sin bloquear
- [x] 1.2 Añadir el ejemplo del aviso a la lista de contraseñas habituales
- [x] 1.3 Pruebas: 5 caracteres rechazada, 6 aceptada y débil, 11 débil, 12 con varias palabras no débil, habituales y secuencias cortas rechazadas, el ejemplo rechazado y contraseñas de 12 o más sin cambios

## 2. Aviso al usuario

- [x] 2.1 Textos en catalán del riesgo, los criterios y el ejemplo, y líneas de ayuda de los formularios de contraseña nueva (primera ejecución y cambio) cuando la contraseña es débil
- [x] 2.2 El contador cuenta sobre el nuevo mínimo y la longitud mínima del mensaje de error sale de la política
- [x] 2.3 Pruebas: aviso completo con contraseña débil, sin aviso con una no débil, contador «n de 6» y error de demasiado corta con el mínimo nuevo

## 3. Documentación y verificación

- [x] 3.1 Actualizar `openspec/config.yaml`, `docs/guia-clave-de-recuperacion.md` y `docs/ejecutar-en-desarrollo.md`, y dar por resuelta la decisión D6 en `docs/riesgos.md` anotando el riesgo aceptado
- [x] 3.2 Comprobar que las contraseñas ya creadas (12 o más caracteres) siguen abriendo la base y que abrir no comprueba la política
- [x] 3.3 Claves de recurso en catalán para todos los textos nuevos
