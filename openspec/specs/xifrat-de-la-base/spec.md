# xifrat-de-la-base Specification

## Purpose
Definir cómo se cifra la base de datos con una llave aleatoria protegida por la contraseña y la clave de recuperación, y cómo viaja esa protección en las copias de seguridad.

## Requirements

### Requirement: Cifrado con llave aleatoria
El sistema SHALL cifrar la base de datos con una llave aleatoria de 256 bits generada al crearla, en un formato de cifrado estándar y documentado compatible con SQLCipher 4.

#### Scenario: Base ilegible sin la llave
- **WHEN** se abre el fichero de base de datos con una herramienta estándar sin la llave
- **THEN** no se puede leer ningún dato ni el esquema

#### Scenario: Llave única por base
- **WHEN** se crean dos bases de datos
- **THEN** cada una tiene una llave distinta

### Requirement: Fichero de claves protegido
El sistema SHALL guardar junto a la base de datos un fichero de claves con la llave de la base envuelta con cifrado autenticado dos veces, una con la contraseña y otra con la clave de recuperación, sin ningún dato personal.

#### Scenario: Contenido del fichero
- **WHEN** se inspecciona el fichero de claves
- **THEN** contiene solo la versión de formato, los parámetros y las sales de derivación y las dos copias envueltas de la llave

#### Scenario: Manipulación
- **WHEN** se altera el fichero de claves
- **THEN** la autenticación falla y no se abre la base de datos

### Requirement: Derivación resistente a fuerza bruta
El sistema SHALL derivar la llave de envoltorio de la contraseña con Argon2id y SHALL guardar sus parámetros en el fichero de claves para poder reforzarlos en el futuro.

#### Scenario: Parámetros guardados
- **WHEN** se crea o cambia la contraseña
- **THEN** los parámetros y la sal usados quedan en el fichero de claves

#### Scenario: Tiempo de desbloqueo
- **WHEN** el usuario desbloquea en un equipo de gama baja
- **THEN** la derivación tarda menos de dos segundos

### Requirement: Abrir la base de datos
El sistema SHALL abrir la base de datos desenvolviendo la llave con la contraseña o con la clave de recuperación, y SHALL rechazar un fichero de claves ausente, dañado o de una versión desconocida sin modificar nada.

#### Scenario: Con la contraseña
- **WHEN** el usuario escribe la contraseña correcta
- **THEN** la llave se desenvuelve y se abre la base de datos

#### Scenario: Fichero de claves ausente
- **WHEN** no existe el fichero de claves junto a la base de datos
- **THEN** el sistema lo indica con un mensaje claro, no modifica la base y ofrece restaurar una copia

#### Scenario: Fichero de claves dañado
- **WHEN** el fichero de claves está dañado
- **THEN** el sistema lo indica, no modifica nada y ofrece restaurar una copia

### Requirement: Cambiar la contraseña no recifra la base
El sistema SHALL cambiar la contraseña o regenerar la clave de recuperación renovando solo el envoltorio de la llave, de forma atómica, y conservando la versión anterior del fichero de claves solo hasta comprobar que la nueva se ha guardado bien y abre con las credenciales nuevas, momento en que la elimina para que la contraseña o la clave sustituidas dejen de abrir los datos.

#### Scenario: Atomicidad
- **WHEN** el cambio se interrumpe a mitad
- **THEN** el fichero de claves anterior sigue siendo válido

#### Scenario: Credenciales sustituidas
- **WHEN** se completa el cambio de contraseña o la regeneración de la clave de recuperación
- **THEN** no queda ninguna versión anterior del fichero de claves y la contraseña o la clave sustituidas ya no abren los datos

#### Scenario: Fichero nuevo que no se comprueba
- **WHEN** tras guardar el fichero de claves nuevo no se puede comprobar que abre con las credenciales nuevas
- **THEN** el sistema restablece el fichero anterior, avisa del fallo y las credenciales anteriores siguen funcionando

#### Scenario: Copias anteriores
- **WHEN** se cambia la contraseña
- **THEN** las copias de seguridad hechas antes siguen abriéndose con la contraseña que tenían

### Requirement: Ningún secreto en el código
El sistema SHALL no contener ninguna llave, contraseña ni secreto de cifrado en el código fuente, en el repositorio ni en las compilaciones, de modo que cualquier compilación pueda abrir una base con la contraseña correcta.

#### Scenario: Compilación propia
- **WHEN** alguien compila ARCA desde el código fuente y abre una base creada por la versión oficial con la contraseña correcta
- **THEN** la base se abre

#### Scenario: Contraseñas de prueba
- **WHEN** se ejecutan las pruebas
- **THEN** usan contraseñas de prueba definidas solo en el proyecto de pruebas

### Requirement: Copias de seguridad con sus llaves
El sistema SHALL incluir en cada copia de seguridad el fichero de claves vigente, de modo que la copia se pueda abrir con la contraseña o la clave de recuperación que tenía al hacerse.

#### Scenario: Contenido de la copia
- **WHEN** se hace una copia de seguridad
- **THEN** contiene la base de datos y el fichero de claves

#### Scenario: Restaurar con la contraseña de la copia
- **WHEN** se restaura una copia
- **THEN** el sistema pide la contraseña o la clave de recuperación de esa copia

### Requirement: Adoptar la llave de la copia al restaurar
El sistema SHALL adoptar como contraseña del centro la de la copia restaurada, avisándolo antes de confirmar, y SHALL conservar la base y el fichero de claves actuales en la copia previa a la restauración.

#### Scenario: Aviso
- **WHEN** el usuario confirma la restauración
- **THEN** el sistema le avisa de que, tras restaurar, la contraseña será la de la copia

#### Scenario: Conservación de lo anterior
- **WHEN** se completa la restauración
- **THEN** la copia previa contiene la base y el fichero de claves anteriores

### Requirement: Migraciones y copias previas con la misma llave
El sistema SHALL aplicar las migraciones y crear las copias previas sin cambiar la llave de la base, y SHALL guardar cada copia previa junto con el fichero de claves.

#### Scenario: Migración
- **WHEN** se migra una base cifrada
- **THEN** la base migrada se abre con la misma contraseña

### Requirement: Compatibilidad entre compilaciones
El sistema SHALL abrir la misma base de datos con cualquier compilación de ARCA de una versión compatible, con la contraseña correcta.

#### Scenario: Otro equipo
- **WHEN** el usuario copia la base, su fichero de claves y su contraseña a otro equipo con ARCA
- **THEN** la base se abre

### Requirement: Feedback y errores
El sistema SHALL informar de los errores de cifrado y de claves con mensajes comprensibles sin detalles técnicos ni datos sensibles.

#### Scenario: Error inesperado
- **WHEN** falla una operación de claves de forma inesperada
- **THEN** el mensaje incluye una referencia del registro técnico y el registro no contiene contraseñas ni llaves
