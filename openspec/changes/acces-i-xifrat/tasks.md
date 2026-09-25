## 1. Primitivas y formato

- [x] 1.1 Puerto de servicios de claves en Application y su implementación en Infrastructure con NSec: generación de la DEK, Argon2id, HKDF y XChaCha20-Poly1305 (D1, D2)
- [x] 1.2 Formato del fichero de claves con versión, parámetros, sales y las dos DEK envueltas, y su lectura y escritura atómicas con versión previa (D3)
- [x] 1.3 Generación, normalización y validación de la clave de recuperación en formato Crockford base32 (D4)
- [x] 1.4 Pruebas: envolver y desenvolver, fichero alterado, versión desconocida, parámetros guardados, clave de recuperación con distinto formato de escritura y ausencia de datos personales en el fichero

## 2. Contraseña del centro

- [x] 2.1 Política de contraseña (mínimo 12 caracteres sin reglas de composición) con contador de longitud, lista de contraseñas habituales embebida, rechazo de repeticiones y secuencias, normalización Unicode e indicador de fortaleza sin dependencias (D5)
- [x] 2.2 Casos de uso: crear la contraseña y la base, desbloquear, cambiar la contraseña y restablecer con la clave de recuperación
- [x] 2.3 Etapa de desbloqueo en el arranque por etapas y cancelación que cierra la aplicación (D6)
- [x] 2.4 Higiene de memoria y garantías de no registrar la contraseña (D9)
- [x] 2.5 Pruebas: creación válida, demasiado corta, una sola palabra de 12 letras aceptada con aviso, contraseña habitual, repetición y secuencia rechazadas, frase con espacios, letras acentuadas, desbloqueo correcto e incorrecto, cambio con fallo a mitad, contraseñas con acentos, "ç" y "l·l", y ausencia de la contraseña en el registro técnico

## 3. Clave de recuperación

- [x] 3.1 Mostrar la clave una vez con imprimir y copiar, y confirmarla escribiendo dos grupos al azar antes de continuar
- [x] 3.2 Entrar con la clave de recuperación y obligar a definir contraseña nueva y clave nueva
- [x] 3.3 Regenerar la clave desde ajustes, invalidando la anterior y conservando la anterior si se cancela
- [x] 3.4 Pruebas: confirmación correcta e incorrecta, entrada con la clave, tolerancia al escribirla, regeneración y cancelación

## 4. Base de datos cifrada

- [x] 4.1 Abrir la base con la DEK desenvuelta e integrarlo con la apertura de `arquitectura-base` (formato SQLCipher 4 con SQLite3 Multiple Ciphers)
- [x] 4.2 Crear la base con DEK aleatoria y fichero de claves en una sola operación atómica
- [x] 4.3 Detectar fichero de claves ausente, dañado o de versión desconocida sin modificar nada y con mensaje que ofrece restaurar una copia
- [x] 4.4 Migraciones y copias previas con la misma DEK, guardando cada copia previa junto con el fichero de claves
- [x] 4.5 Pruebas de integración con SQLite cifrado temporal: base ilegible sin la llave, apertura en otra compilación con la contraseña, migración con la misma contraseña, fichero de claves ausente y dañado

## 5. Copias y restauración

- [x] 5.1 Contenedor de copia con base y fichero de claves, verificado con la DEK en memoria al copiar (D7)
- [x] 5.2 Restauración que lee el fichero de claves de la copia, pide la contraseña o la clave de recuperación y adopta su llave, con aviso previo
- [x] 5.3 Copia previa a la restauración con la base y el fichero de claves anteriores
- [x] 5.4 Pruebas: copia y restauración con contraseña, con clave de recuperación, copia anterior a un cambio de contraseña, contraseña equivocada y restauración en otro equipo

## 6. Pantallas y textos

- [x] 6.1 Pantalla de contraseña en el arranque y en la primera ejecución, integrada con `configuracio-inicial`
- [ ] 6.2 Sección de seguridad en ajustes: cambiar la contraseña, regenerar la clave y recordatorio de que no hay recuperación sin llave
- [ ] 6.3 Guía para la dirección del centro sobre cómo guardar la clave de recuperación (sobre cerrado, dos copias) y qué pasa si se pierde todo
- [ ] 6.4 Claves de recurso en catalán para todos los textos
- [ ] 6.5 Pruebas de mensajes, doble Intro, uso solo con teclado y estados de error

## 7. Verificación transversal

- [ ] 7.1 Prueba de que no existe ninguna contraseña ni llave por defecto en el código de producción (D8)
- [ ] 7.2 Prueba de privacidad: un fallo con contraseña o clave de recuperación no deja rastro en el registro técnico
- [ ] 7.3 Prueba de arquitectura: `Domain` y `Application` no referencian NSec ni SQLite3 Multiple Ciphers
- [ ] 7.4 Prueba de tiempo de desbloqueo con los parámetros de Argon2id en equipo de gama baja
- [ ] 7.5 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 7.6 Prueba de extremo a extremo: crear contraseña, guardar la clave, cerrar, desbloquear, olvidar y recuperar, cambiar la contraseña y restaurar una copia antigua
