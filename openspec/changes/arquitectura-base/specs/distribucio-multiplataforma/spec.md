## Purpose

Definir cómo se instala y se ejecuta ARCA: instalador como vía principal en Windows, versión portable para equipos sin permisos y comportamiento equivalente en Windows, Linux y macOS.

## ADDED Requirements

### Requirement: Instalador para Windows
El sistema SHALL distribuirse en Windows mediante un instalador que no requiere conexión a internet.

#### Scenario: Instalación
- **WHEN** el usuario ejecuta el instalador
- **THEN** la aplicación queda instalada con acceso directo y puede abrirse sin pasos adicionales

#### Scenario: Desinstalación conserva los datos
- **WHEN** el usuario desinstala la aplicación
- **THEN** la base de datos y las copias de seguridad no se eliminan, salvo que el usuario lo elija explícitamente

### Requirement: Actualización sin pérdida de datos
El sistema SHALL conservar la base de datos y la configuración al instalar una versión más nueva sobre una existente.

#### Scenario: Instalar sobre una versión anterior
- **WHEN** se instala una versión nueva con cambios de esquema sobre una instalación existente
- **THEN** los datos se conservan y se migran en el siguiente arranque según las reglas de migración segura

### Requirement: Versión portable
El sistema SHALL ofrecer una versión portable que se ejecuta desde una carpeta sin instalación ni permisos de administrador.

#### Scenario: Ejecución portable
- **WHEN** existe el fichero marcador de modo portable junto al ejecutable
- **THEN** la aplicación guarda su base de datos y su configuración junto al ejecutable y no escribe fuera de esa carpeta

#### Scenario: Sin marcador
- **WHEN** no existe el fichero marcador junto al ejecutable
- **THEN** la aplicación usa las ubicaciones de datos de usuario del sistema operativo

### Requirement: Comportamiento equivalente en Windows, Linux y macOS
El sistema SHALL ofrecer el mismo comportamiento funcional en Windows, Linux y macOS, adaptando únicamente las rutas y convenciones propias de cada sistema.

#### Scenario: Mismos datos, mismo resultado
- **WHEN** se ejecuta la misma operación sobre el mismo fichero de base de datos en cada sistema operativo soportado
- **THEN** el resultado es idéntico

#### Scenario: Pruebas fuera de Windows
- **WHEN** se ejecutan las pruebas de dominio y persistencia en macOS o Linux
- **THEN** todas pueden ejecutarse y superarse sin necesidad de Windows

### Requirement: Funcionamiento sin conexión
El sistema SHALL ofrecer toda su funcionalidad sin conexión a internet.

#### Scenario: Uso continuado sin red
- **WHEN** el equipo permanece sin conexión durante un curso completo
- **THEN** todas las funciones de gestión siguen disponibles

### Requirement: Versión visible
El sistema SHALL mostrar en la aplicación su versión y la versión de esquema de la base de datos.

#### Scenario: Consulta de la versión
- **WHEN** el usuario abre la información de la aplicación
- **THEN** ve la versión de la aplicación y la versión de esquema de su base de datos
