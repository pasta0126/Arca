## Context

Nuevo cambio que sustituye la "clave interna transparente" de `arquitectura-base` (D4), incompatible con un repositorio abierto. Motivación y alcance en `proposal.md`; comportamiento en `specs/`. La base sigue siendo un único fichero SQLite cifrado en formato SQLCipher 4 con SQLite3 Multiple Ciphers (`docs/stack.md`). Restricciones: sin secretos en el código, funcionamiento igual en los tres sistemas, sin depender de servidores ni del almacén de claves del sistema, conserjes no técnicos y datos de menores.

## Decisión de responsabilidad

**El mantenedor no custodia ninguna contraseña ni clave de recuperación y no puede recuperar los datos de ningún centro.** Si un centro pierde la contraseña y la clave de recuperación, pierde los datos y las copias. Es una decisión deliberada: quien desarrolla ARCA se hace responsable del software, no de los datos de los centros. Se descartó la custodia por el mantenedor (incluida cifrada con su clave pública) por convertirlo en custodio de datos de menores y por el riesgo para todos los centros si esa custodia se comprometiera. Por eso el aviso a la dirección del centro es claro, la clave de recuperación es obligatoria y hay que confirmar que se ha guardado.

## Goals / Non-Goals

**Goals:**
- Protección real de los datos frente a quien tenga el fichero o una copia.
- Que perder la contraseña no signifique perder los datos, si se guardó la clave de recuperación.
- Cambiar la contraseña sin recifrar la base.

**Non-Goals:**
- Usuarios y roles, bloqueo por inactividad, recordar la contraseña, custodia por terceros y cifrado de exportaciones.

## Decisions

### D1. Llave de datos aleatoria envuelta por dos llaves
La base se cifra con una **llave de datos** aleatoria de 256 bits (DEK). El fichero de claves guarda la DEK envuelta con cifrado autenticado (XChaCha20-Poly1305) por dos llaves de envoltorio: una derivada de la contraseña y otra derivada de la clave de recuperación. Cambiar la contraseña o regenerar la clave de recuperación solo rehace un envoltorio; la base y las copias no se tocan. *Alternativa descartada*: usar la contraseña directamente como llave de la base; obligaría a recifrar todo al cambiarla, invalidaría las copias antiguas sin remedio y no permitiría una segunda llave de recuperación.

### D2. Derivación y primitivas con NSec
Argon2id para la contraseña, con memoria y pasadas calibradas para menos de dos segundos en equipos de gama baja (punto de partida: 64 MiB y 3 pasadas) y guardadas en el fichero de claves para poder reforzarlas. Para la clave de recuperación, que ya es aleatoria de alta entropía, basta una derivación con HKDF. Todas las primitivas vienen de NSec.Cryptography (MIT, sobre libsodium), la misma biblioteca de `registre-i-actualitzacions`. El spike de `docs/stack.md` comprueba Argon2id y XChaCha20-Poly1305 en los tres sistemas. *Alternativa descartada*: PBKDF2 del sistema; menos resistente a hardware de ataque.

### D3. Fichero de claves junto a la base, con versión previa
Un fichero pequeño con el mismo nombre que la base y extensión propia. Contiene versión de formato, parámetros y sales de derivación y las dos DEK envueltas, sin datos personales. Se reescribe siempre en un temporal y se sustituye de forma atómica; la versión anterior se conserva solo hasta comprobar, justo después de guardar, que la nueva abre con las credenciales nuevas, y entonces se elimina (ver *Cambios durante la implementación*). Si falta o está dañado la base no se puede abrir: el mensaje ofrece restaurar una copia, que lleva su propio fichero de claves. El riesgo de perder solo este fichero se acota copiándolo con cada copia de seguridad y con cada copia previa a migración.

### D4. Clave de recuperación
26 caracteres de un alfabeto Crockford base32 (sin caracteres ambiguos), 130 bits, en cinco grupos (cuatro de 5 caracteres y el último de 6, porque 26 no es múltiplo de 5). Se normaliza al leerla (mayúsculas, sin guiones ni espacios, equivalencias 0/O y 1/I/L). Se muestra una vez, se puede imprimir o copiar y se confirma escribiendo dos grupos elegidos al azar antes de continuar. Nunca se guarda en claro: solo existe su envoltorio de la DEK. Regenerarla invalida la anterior.

### D5. Política de contraseña
Decisión de producto: **mínimo 12 caracteres de cualquier tipo, sin reglas de composición**, comprobada al crearla y al cambiarla. Es la práctica que recomiendan las guías actuales (NIST): la longitud pesa más que obligar a mayúsculas o símbolos, que empujan a patrones previsibles como `Taquilla#1`. Se añaden dos barreras sencillas: (1) una **lista de contraseñas habituales** de código abierto y licencia permisiva embebida en la aplicación, más el rechazo de repeticiones y secuencias obvias, y (2) un **indicador de fortaleza** orientativo, sin dependencias externas, que avisa sin bloquear cuando es una sola palabra, solo minúsculas seguidas o un patrón previsible, y anima a usar varias palabras sin relación. Se muestra un contador de longitud mientras se escribe. Sin caducidad ni longitud máxima; los espacios están permitidos. La contraseña se normaliza en Unicode (NFC) para que la misma frase se escriba igual en cualquier sistema. Sin límite de intentos ni retardo artificial: un ataque fuera de línea no se frena con eso, y lo que lo frena es Argon2id y la calidad de la contraseña. *Limitación asumida*: una contraseña larga pero previsible, como el nombre del centro, sigue siendo débil; el indicador y la guía para la dirección lo advierten. *Idea descartada por ahora*: un generador de contraseñas de varias palabras, que añadiría una lista de palabras en catalán; se puede incorporar más adelante sin cambiar el resto.

### D6. Desbloqueo en el arranque
El arranque por etapas de `arquitectura-base` incorpora una etapa de desbloqueo antes de abrir la base: la pantalla de arranque pasa a pedir la contraseña y las etapas siguientes esperan. El ajuste local de la ruta y la comprobación de instancia única no dependen del desbloqueo. Cancelar cierra la aplicación.

### D7. Copias de seguridad
La copia es un contenedor sencillo, sin compresión, con la base de datos y el fichero de claves vigente, con extensión propia. Al hacerla, la aplicación ya está desbloqueada y verifica la copia con la DEK en memoria. Al restaurar se lee el fichero de claves del contenedor, se pide la contraseña o la clave de recuperación de esa copia, se desenvuelve la DEK y se verifica. La restauración adopta la contraseña de la copia, avisándolo, y la copia previa conserva la base y el fichero de claves anteriores.

### D8. Sin secretos y contraseñas de prueba
No hay ninguna llave ni contraseña en el código ni en las compilaciones. Las pruebas y la herramienta de datos de ejemplo usan contraseñas de prueba propias (por ejemplo `demo-demo-demo`) definidas solo en sus proyectos, y una prueba comprueba que no existe ninguna contraseña por defecto en el código de producción.

### D9. Higiene de memoria
La contraseña se maneja como un arreglo de bytes que se pone a cero cuando deja de necesitarse, en la medida de lo posible; no se usa `SecureString`, obsoleto y engañoso. No se registran contraseñas ni claves, y los tipos de error propios no llevan valores sensibles.

### D10. Feedback
Resultado estructurado, mensajes comprensibles sin detalles técnicos, doble ejecución protegida y operación completa con teclado, según los principios de UX transversal.

## Risks / Trade-offs

- **Se pierden la contraseña y la clave de recuperación → datos irrecuperables** → clave de recuperación obligatoria y confirmada, avisos claros, y una guía para la dirección del centro (guardarla en sobre cerrado, en dos sitios).
- **Fricción de teclear la contraseña al abrir** → es deliberada y la que da la protección; recordarla en el equipo queda fuera de v1.
- **Contraseña débil elegida por el centro** → indicador de fortaleza y mínimo de longitud; Argon2id encarece cada intento.
- **Contraseña compartida apuntada en un papel pegado al monitor** → riesgo organizativo inevitable; la guía lo advierte.
- **Se pierde solo el fichero de claves** → se copia con cada copia de seguridad y cada copia previa a migración; el mensaje ofrece restaurar.
- **Restaurar cambia la contraseña actual por la de la copia** → aviso antes de confirmar y copia previa conservada.
- **Rendimiento de Argon2id en equipos antiguos** → parámetros calibrados y guardados; el spike lo mide.
- **NSec o libsodium con problemas de binarios en algún sistema** → el spike lo comprueba; alternativa de reserva con bibliotecas gestionadas.

## Migration Plan

No aplica: no hay instalaciones anteriores. Las bases nuevas se crean con contraseña desde el primer arranque.

## Open Questions

- Parámetros exactos de Argon2id: se calibran en el spike con equipos reales.
- Formato del contenedor de copia (por ejemplo un archivo con las dos entradas): detalle de implementación sin efecto en las specs.

## Cambios durante la implementación

### 2026-09-25. La versión anterior del fichero de claves se elimina al comprobar la nueva
Motivo: la autorrevisión detectó que conservar el fichero anterior hasta el siguiente desbloqueo mantenía abierta una ventana: tras regenerar la clave de recuperación (por ejemplo porque se había expuesto) o cambiar la contraseña, el fichero anterior seguía abriendo los datos con la credencial sustituida hasta el siguiente arranque. Se mantiene la finalidad de la versión anterior (poder volver atrás si el fichero nuevo saliera dañado) pero se acota: se guarda el fichero nuevo, se lee de nuevo y se comprueba que abre con las credenciales nuevas; si es así se elimina la anterior en el acto, y si no, se restablece la anterior. Se cambian D3 y el requisito «Cambiar la contraseña no recifra la base» (nuevos escenarios «Credenciales sustituidas» y «Fichero nuevo que no se comprueba»). No afecta a otros cambios: las copias de seguridad ya llevan su propio fichero de claves.
