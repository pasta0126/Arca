## Why

ARCA es software libre con el código abierto: no puede haber ningún secreto dentro del programa. Los datos de menores solo quedan protegidos de verdad si la llave depende de algo que tiene únicamente el centro. La solución más simple y que funciona igual en Windows, Linux y macOS es una **contraseña compartida del centro** que abre el fichero, con una **clave de recuperación** para no perder los datos si se olvida.

## What Changes

- **Contraseña compartida del centro**: se crea en la primera ejecución (mínimo 12 caracteres de cualquier tipo, sin reglas de composición) y se pide al abrir ARCA. No hay usuarios ni roles: una sola contraseña para el centro.
- **Clave de recuperación**: un código largo generado al crear la contraseña, que se muestra una sola vez para imprimir o apuntar, y que el centro debe confirmar que ha guardado antes de continuar. Sirve como segunda llave para entrar y para restablecer la contraseña.
- **Cifrado con llave aleatoria protegida**: la base de datos se cifra con una llave aleatoria de 256 bits. Un pequeño fichero de claves junto a la base guarda esa llave "envuelta" con la contraseña (derivada con Argon2id) y con la clave de recuperación. Cambiar la contraseña solo renueva ese envoltorio, sin recifrar la base ni invalidar las copias.
- **Cambiar la contraseña y regenerar la clave de recuperación** desde ajustes, con la aplicación desbloqueada.
- **Las copias de seguridad llevan su propio fichero de claves**: restaurar pide la contraseña, o la clave de recuperación, de esa copia.
- **Sin recuperación sin llave**: si se pierden la contraseña y la clave de recuperación, los datos son irrecuperables, y así se advierte con claridad. El mantenedor del proyecto no guarda ninguna llave ni puede recuperar los datos: es responsable del software, no de los datos de los centros.
- **Ningún secreto en el código** ni en las compilaciones, y ninguna dependencia de un servidor ni del almacén de claves del sistema operativo.

## Capabilities

### New Capabilities
- `contrasenya-del-centre`: crear, pedir y cambiar la contraseña compartida.
- `clau-de-recuperacio`: generación, confirmación, uso y regeneración de la clave de recuperación.
- `xifrat-de-la-base`: cifrado con llave aleatoria, fichero de claves, apertura, copias y restauración.

### Modified Capabilities

<!-- Ninguna en las specs vigentes. Sustituye el requisito de "clave interna sin contraseña" de `arquitectura-base` (aún no archivado), que se ajusta en ese cambio. -->

## Fuera de alcance

- Usuarios, roles y registro de quién hace cada operación (sigue siendo una sola contraseña de centro).
- Bloqueo por inactividad, recordar la contraseña en el equipo y autenticación biométrica.
- Custodia de la clave de recuperación por terceros, incluido el mantenedor del proyecto.
- Cifrado de los ficheros CSV exportados y cifrado del disco del equipo.
- Protección frente a malware que se ejecute con la sesión abierta.

## Impacto

- **Código**: servicio de claves en Application con puertos, implementación criptográfica en Infrastructure (Argon2id, cifrado autenticado y derivación con NSec), pantalla de contraseña en el arranque y en la primera ejecución.
- **Datos personales (RGPD)**: es la medida técnica que hace real el cifrado en reposo de los datos de menores. La contraseña y la clave de recuperación nunca salen del equipo, nunca se guardan en claro y nunca se registran.
- **Riesgo de producto**: si el centro pierde la contraseña y la clave de recuperación, no hay forma de recuperar los datos. Se mitiga con la clave de recuperación obligatoria y la confirmación de que se ha guardado.
- **Depende de**: `arquitectura-base` (base cifrada, arranque por etapas). Lo usan `configuracio-inicial` (primera ejecución) y `copies-de-seguretat` (copias y restauración).
