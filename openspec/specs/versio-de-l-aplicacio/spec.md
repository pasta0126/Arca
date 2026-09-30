# versio-de-l-aplicacio Specification

## Purpose
Definir cómo se muestra la versión de la aplicación para localizarla de un vistazo: en pequeño junto al título en la cabecera y en Ajustes, con una única fuente.

## Requirements

### Requirement: Versión visible en la cabecera
El sistema SHALL mostrar en la cabecera, en un tamaño pequeño y discreto junto al título de la aplicación, el número de versión con la forma `x.y.z`, desde cualquier pantalla y sin depender del curso, de la identidad del centro ni del tema elegido.

#### Scenario: Siempre a la vista
- **WHEN** el usuario abre cualquier sección
- **THEN** ve el número de versión junto al título en la cabecera

#### Scenario: Con la identidad del centro
- **WHEN** el centro ha definido su nombre y su logotipo
- **THEN** la cabecera muestra la identidad del centro y el número de versión sigue visible en pequeño junto a ella

#### Scenario: Ventana estrecha
- **WHEN** la ventana tiene el tamaño mínimo permitido
- **THEN** la versión sigue visible y no tapa el título, la búsqueda ni el estado global

#### Scenario: Descripción
- **WHEN** el usuario se detiene sobre el número
- **THEN** una descripción emergente dice que es la versión de la aplicación

### Requirement: Versión en Ajustes
El sistema SHALL mostrar en Ajustes el mismo número de versión, con su etiqueta, y SHALL permitir seleccionarlo y copiarlo para pegarlo en un aviso de soporte.

#### Scenario: Mismo número
- **WHEN** el usuario compara la cabecera con Ajustes
- **THEN** ambos muestran exactamente el mismo número

#### Scenario: Copiar
- **WHEN** el usuario selecciona la versión en Ajustes y copia
- **THEN** el portapapeles contiene solo el número de versión

### Requirement: Una sola fuente de la versión
El sistema SHALL tomar el número de versión de una única definición en el proyecto, la misma de la que se obtiene el nombre de los paquetes, sin repetirlo en ningún otro sitio del código, de los textos de interfaz ni de los scripts.

#### Scenario: Cambiar la versión
- **WHEN** se cambia el número en su única definición y se compila
- **THEN** la cabecera, Ajustes y el nombre del paquete muestran el número nuevo

#### Scenario: Coherencia con el paquete
- **WHEN** se genera el paquete de un sistema
- **THEN** el número del nombre del paquete es el que muestra la aplicación al abrirse

### Requirement: Formato de la versión
El sistema SHALL escribir la versión como `MAYOR.MENOR.PARCHE`, con un sufijo opcional tras un guion para las compilaciones que no son una versión publicada (por ejemplo `-dev`), y SHALL mostrarlo tal cual, sin recortar el sufijo.

#### Scenario: Compilación de desarrollo
- **WHEN** la versión del proyecto es `0.1.0-dev`
- **THEN** la cabecera y Ajustes muestran `0.1.0-dev`

#### Scenario: Versión publicada
- **WHEN** la versión del proyecto es `1.2.3`
- **THEN** la cabecera y Ajustes muestran `1.2.3`

#### Scenario: Versión ilegible
- **WHEN** la aplicación no puede leer su número de versión
- **THEN** muestra una marca de versión desconocida y registra el fallo sin detener el arranque
