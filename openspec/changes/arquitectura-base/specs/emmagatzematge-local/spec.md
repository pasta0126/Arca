## Purpose

Define cómo la aplicación guarda sus datos en el equipo: en un único fichero local cifrado, con ubicación configurable, evolución segura del esquema entre versiones y protección frente a arranques incompatibles o simultáneos.

## ADDED Requirements

### Requirement: Base de datos local en un único fichero
El sistema SHALL guardar todos sus datos en un único fichero de base de datos local, sin depender de ningún servidor ni de conexión de red.

#### Scenario: Primer arranque sin datos
- **WHEN** la aplicación arranca y no existe fichero de base de datos en la ubicación configurada
- **THEN** el sistema no crea nada en silencio: muestra la pantalla de primera ejecución de `configuracio-inicial`, y al elegir empezar de cero crea el fichero con el esquema de la versión actual

#### Scenario: Arranque sin red
- **WHEN** el equipo no tiene conexión de red
- **THEN** el sistema abre la base de datos y permite trabajar con normalidad

### Requirement: Cifrado en reposo con clave interna
El sistema SHALL cifrar el fichero de base de datos en disco con una clave interna, sin solicitar contraseña al usuario.

#### Scenario: Fichero ilegible sin la clave
- **WHEN** una persona abre el fichero de base de datos con una herramienta SQLite estándar sin la clave
- **THEN** no puede leer ningún dato ni el esquema

#### Scenario: Apertura transparente
- **WHEN** el conserje abre la aplicación
- **THEN** la base de datos se abre sin pedir contraseña ni ninguna otra credencial

#### Scenario: Clave independiente de la licencia y de la red
- **WHEN** la licencia caduca, se revoca o no hay conexión con ningún servidor
- **THEN** el sistema sigue pudiendo abrir y leer la base de datos

### Requirement: Fichero transportable entre equipos y sistemas operativos
El sistema SHALL poder abrir un fichero de base de datos creado en cualquier otro equipo o sistema operativo compatible con ARCA, siempre que su versión de esquema sea igual o anterior a la soportada.

#### Scenario: Restaurar en otro equipo
- **WHEN** se copia un fichero de base de datos creado en un equipo Windows a un equipo Linux o macOS con ARCA instalado y se configura su ruta
- **THEN** el sistema lo abre y muestra los mismos datos

### Requirement: Ubicación de la base de datos configurable
El sistema SHALL usar una ubicación de base de datos por defecto propia de cada sistema operativo y SHALL permitir al usuario cambiarla.

#### Scenario: Ubicación por defecto
- **WHEN** no se ha configurado ninguna ruta
- **THEN** el sistema usa la carpeta de datos de usuario del sistema operativo, sin requerir permisos de administrador

#### Scenario: Ruta configurada
- **WHEN** se configura una ruta distinta y válida
- **THEN** el sistema usa esa ruta en los siguientes arranques

#### Scenario: Ruta no accesible
- **WHEN** la ruta configurada no existe, no es escribible o no es accesible
- **THEN** el sistema muestra un mensaje claro, no crea ni modifica ningún fichero y permite elegir otra ruta

### Requirement: Migraciones de esquema seguras
El sistema SHALL versionar el esquema de la base de datos y SHALL actualizarlo automáticamente cuando el fichero es de una versión anterior, sin perder datos.

#### Scenario: Copia verificada antes de migrar
- **WHEN** el sistema detecta una base de datos con esquema anterior al actual
- **THEN** antes de modificar nada crea una copia junto al fichero original y comprueba su integridad, y solo entonces aplica las migraciones

#### Scenario: Migración atómica
- **WHEN** una migración falla a mitad de camino
- **THEN** la base de datos queda exactamente como estaba antes de migrar, el sistema informa del error y no permite continuar con datos a medias

#### Scenario: Copia previa corrupta
- **WHEN** la copia previa a la migración no supera la comprobación de integridad
- **THEN** el sistema no migra, informa del problema y deja el fichero original intacto

#### Scenario: Esquema ya actualizado
- **WHEN** la base de datos ya tiene el esquema actual
- **THEN** el sistema no crea copias ni aplica migraciones

### Requirement: Retención acotada de copias previas a migración
El sistema SHALL conservar únicamente las 3 copias previas a migración más recientes y SHALL eliminar las más antiguas tras una migración correcta.

#### Scenario: Cuarta migración
- **WHEN** se completa una migración y ya existían 3 copias previas
- **THEN** el sistema conserva las 3 más recientes, incluida la recién creada, y elimina la más antigua

#### Scenario: Migración fallida
- **WHEN** una migración falla
- **THEN** el sistema no elimina ninguna copia previa existente

### Requirement: Rechazo de bases de datos de versión más nueva
El sistema SHALL negarse a abrir una base de datos cuyo esquema sea más reciente que el que conoce.

#### Scenario: Fichero creado por una versión posterior
- **WHEN** se abre un fichero con versión de esquema superior a la soportada por la aplicación instalada
- **THEN** el sistema no lo modifica, informa de que hay que actualizar la aplicación y no continúa

### Requirement: Fichero corrupto o clave incompatible
El sistema SHALL detectar un fichero de base de datos ilegible o no descifrable y SHALL informar sin modificarlo.

#### Scenario: Fichero dañado
- **WHEN** el fichero de base de datos está dañado o no es una base de datos de ARCA
- **THEN** el sistema muestra un mensaje claro, no lo sobrescribe y ofrece elegir otra ruta o restaurar una copia de seguridad (`copies-de-seguretat`)

### Requirement: Instancia única sobre una base de datos
El sistema SHALL impedir que dos instancias de la aplicación abran a la vez la misma base de datos.

#### Scenario: Segunda instancia
- **WHEN** ya hay una instancia abierta y se intenta abrir otra sobre la misma base de datos
- **THEN** la segunda no abre la base de datos, informa de que ya hay otra instancia en ejecución y no altera ningún dato

### Requirement: Importes exactos
El sistema SHALL almacenar y calcular los importes monetarios sin pérdida de precisión.

#### Scenario: Suma de importes con céntimos
- **WHEN** se suman los importes de varios cargos con céntimos
- **THEN** el total es exacto y coincide con la suma decimal de los importes

### Requirement: Fechas sin ambigüedad de zona horaria
El sistema SHALL distinguir entre fechas de calendario (como una fecha de pago o de vencimiento) e instantes (como el momento de un registro), y SHALL conservar cada una sin alterarla al cambiar de equipo o de zona horaria.

#### Scenario: Fecha de calendario estable
- **WHEN** se registra una fecha de pago y el fichero se abre en un equipo con otra zona horaria
- **THEN** la fecha mostrada es la misma
