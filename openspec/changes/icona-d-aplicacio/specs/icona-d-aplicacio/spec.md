## Purpose
Definir el icono de ARCA: un símbolo sencillo y provisional que identifica la aplicación en los tres sistemas y se puede sustituir por otro sin tocar código.

## ADDED Requirements

### Requirement: Icono provisional sencillo
El sistema SHALL tener un icono propio, sencillo y discreto, un símbolo neutro de taquilla en los tonos del tema, dibujado para este proyecto, que se pueda reconocer a 16 píxeles y no dependa de texto.

#### Scenario: Reconocible a tamaño pequeño
- **WHEN** se muestra el icono a 16 por 16 píxeles
- **THEN** se distingue como un cuadrado con la puerta de una taquilla y no queda un borrón

#### Scenario: Sin texto
- **WHEN** se revisa el dibujo
- **THEN** no contiene letras ni rótulos que haya que traducir

### Requirement: Icono visible en la aplicación
El sistema SHALL mostrar el icono en la ventana principal y en las ventanas de diálogo, y en la barra de tareas, el dock o el selector de aplicaciones de Windows, Linux y macOS, sin depender de ningún fichero externo a la aplicación.

#### Scenario: Ventana principal
- **WHEN** el usuario abre ARCA
- **THEN** la ventana y su botón en la barra de tareas o en el dock muestran el icono de ARCA

#### Scenario: Diálogos
- **WHEN** se abre un diálogo o un formulario
- **THEN** su ventana muestra el mismo icono

#### Scenario: Modo portable
- **WHEN** ARCA se ejecuta desde una carpeta portable sin instalar
- **THEN** el icono se muestra igualmente

### Requirement: Icono en los paquetes
El sistema SHALL incluir el icono en el paquete de cada sistema: como icono del ejecutable en Windows, como icono de `ARCA.app` en macOS y como imagen con un acceso de escritorio de ejemplo en Linux.

#### Scenario: Windows
- **WHEN** se genera el paquete de Windows
- **THEN** el ejecutable lleva el icono incrustado y el Explorador lo muestra

#### Scenario: macOS
- **WHEN** se genera el paquete de macOS
- **THEN** `ARCA.app` lleva su icono en el Finder y en el dock

#### Scenario: Linux
- **WHEN** se genera el paquete de Linux
- **THEN** incluye la imagen del icono y un fichero de acceso de escritorio de ejemplo que la usa

### Requirement: Icono sustituible
El sistema SHALL generar todos los formatos del icono desde un único fichero maestro mediante un script, de modo que cambiar la imagen sea sustituir ese fichero y ejecutar el script, sin modificar código ni especificaciones, y SHALL guardar los formatos generados en el repositorio para que compilar no exija herramientas de imágenes.

#### Scenario: Cambiar el icono
- **WHEN** se sustituye el fichero maestro por otra imagen y se ejecuta el script
- **THEN** se regeneran los tamaños, el icono de Windows y el de macOS y la aplicación muestra la imagen nueva al compilar

#### Scenario: Compilar sin herramientas de imágenes
- **WHEN** se compila en un equipo sin las herramientas de generación
- **THEN** la compilación funciona con los formatos que ya están en el repositorio

#### Scenario: Derivados al día
- **WHEN** se cambia el fichero maestro y no se regeneran los derivados
- **THEN** la verificación local avisa de que los derivados no corresponden al maestro

### Requirement: Coste cero y licencia
El sistema SHALL usar solo un dibujo original con la licencia del repositorio y herramientas de generación gratuitas y de código abierto, sin fuentes, imágenes ni bancos de iconos de terceros con licencia restrictiva o de pago, y SHALL dejar la marca y el logotipo definitivos de ARCA como decisión aparte.

#### Scenario: Procedencia
- **WHEN** se revisa el icono
- **THEN** consta que es un dibujo propio del proyecto y qué herramientas gratuitas lo generan

#### Scenario: Marca
- **WHEN** se decide el logotipo definitivo
- **THEN** el icono provisional se sustituye sin cambiar la licencia del código

### Requirement: Independiente del logotipo del centro
El sistema SHALL mantener el icono de la aplicación separado del logotipo que cada centro sube en Ajustes: el icono identifica a ARCA en el sistema y el logotipo identifica al centro dentro de la aplicación.

#### Scenario: Logotipo del centro
- **WHEN** un centro sube su logotipo
- **THEN** se muestra en la cabecera de la aplicación y el icono de la ventana y del paquete no cambia
