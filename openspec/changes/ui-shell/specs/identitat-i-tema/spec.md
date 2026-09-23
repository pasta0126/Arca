## Purpose

Permitir que la aplicación lleve la identidad del centro (nombre, logo y color de acento) y que se pueda usar con tema claro, oscuro o el del sistema, con contraste suficiente en todos los casos.

## ADDED Requirements

### Requirement: Nombre del centro
El sistema SHALL permitir definir el nombre del centro, de 1 a 100 caracteres, y mostrarlo en la cabecera.

#### Scenario: Definir el nombre
- **WHEN** el usuario guarda el nombre "Institut Exemple"
- **THEN** la cabecera lo muestra

#### Scenario: Nombre vacío
- **WHEN** el usuario intenta guardar un nombre vacío
- **THEN** el sistema lo rechaza con un error de nombre obligatorio

#### Scenario: Sin nombre definido
- **WHEN** aún no se ha definido el nombre
- **THEN** la cabecera muestra el nombre de la aplicación

### Requirement: Logo del centro
El sistema SHALL permitir cargar un logo en formato PNG o JPEG de hasta 1 MB, mostrarlo en la cabecera y quitarlo, y SHALL rechazar otros formatos y tamaños con un mensaje claro.

#### Scenario: Cargar un logo
- **WHEN** el usuario elige un PNG de 200 KB
- **THEN** se guarda y aparece en la cabecera

#### Scenario: Formato no admitido
- **WHEN** el usuario elige un fichero que no es PNG ni JPEG
- **THEN** el sistema lo rechaza indicando los formatos admitidos

#### Scenario: Fichero demasiado grande
- **WHEN** el usuario elige una imagen de más de 1 MB
- **THEN** el sistema lo rechaza indicando el tamaño máximo

#### Scenario: Quitar el logo
- **WHEN** el usuario quita el logo
- **THEN** la cabecera vuelve a mostrar solo el nombre

#### Scenario: Imagen dañada
- **WHEN** el fichero tiene extensión de imagen pero no se puede leer
- **THEN** el sistema lo rechaza con un mensaje claro y no cambia el logo actual

### Requirement: Color de acento
El sistema SHALL permitir elegir un color de acento entre una paleta o mediante un valor de color, y SHALL aplicarlo a los elementos destacados de la interfaz.

#### Scenario: Elegir un acento
- **WHEN** el usuario elige un color de acento
- **THEN** botones principales, selección y foco usan ese color

#### Scenario: Restablecer
- **WHEN** el usuario restablece el color
- **THEN** se usa el acento por defecto de ARCA

### Requirement: Contraste garantizado
El sistema SHALL garantizar en los temas claro y oscuro un contraste suficiente entre el color de acento y el texto que se muestra sobre él, ajustando el tono del acento o el color del texto cuando sea necesario.

#### Scenario: Acento demasiado claro
- **WHEN** el usuario elige un acento muy claro
- **THEN** el texto sobre los botones principales sigue siendo legible con el contraste requerido

#### Scenario: Acento demasiado oscuro en tema oscuro
- **WHEN** el usuario elige un acento muy oscuro con el tema oscuro
- **THEN** el acento se aclara lo necesario para que el foco y la selección sean visibles

### Requirement: Identidad guardada con los datos
El sistema SHALL guardar el nombre, el logo y el color en la base de datos, de modo que viajen con las copias de seguridad y se restauren con ellos.

#### Scenario: Restaurar en otro equipo
- **WHEN** el usuario restaura una copia en un equipo nuevo
- **THEN** la aplicación muestra el nombre, el logo y el color del centro

### Requirement: Tema claro, oscuro o del sistema
El sistema SHALL ofrecer los temas claro, oscuro y del sistema, con el del sistema por defecto, SHALL aplicar el cambio sin reiniciar y SHALL guardar la elección en los ajustes locales del equipo.

#### Scenario: Tema del sistema
- **WHEN** el tema es el del sistema y el sistema cambia a oscuro
- **THEN** la aplicación cambia a oscuro

#### Scenario: Cambio manual
- **WHEN** el usuario elige el tema oscuro
- **THEN** la aplicación cambia al instante y lo recuerda al reabrir

#### Scenario: Ajuste ilegible
- **WHEN** no se puede leer la elección guardada
- **THEN** se usa el tema del sistema

### Requirement: Tema como recursos con nombre
El sistema SHALL definir el tema como el conjunto de recursos con nombre que consumen los componentes de `ux-fonaments` (colores semánticos de éxito, aviso, error, foco, superficie y texto, tipografías y espaciados), con valores para claro y oscuro.

#### Scenario: Estados semánticos
- **WHEN** se muestra un estado de taquilla o una notificación
- **THEN** su color procede de un recurso semántico y cumple el contraste en ambos temas

#### Scenario: Estado no solo por color
- **WHEN** se muestra el estado de una taquilla
- **THEN** se distingue también por icono o texto, no solo por el color

### Requirement: Pantalla de arranque con la identidad de ARCA
El sistema SHALL mostrar en la pantalla de arranque la identidad de la aplicación y no la del centro, porque el arranque ocurre antes de abrir la base de datos.

#### Scenario: Arranque
- **WHEN** la aplicación arranca
- **THEN** la pantalla de arranque muestra el nombre y el logo de ARCA

### Requirement: Feedback de la identidad
El sistema SHALL informar del resultado de guardar la identidad y mostrar una vista previa del cambio antes de aplicarlo.

#### Scenario: Vista previa
- **WHEN** el usuario elige un color o un logo
- **THEN** ve una vista previa de la cabecera antes de confirmar

#### Scenario: Guardado
- **WHEN** el usuario confirma los cambios
- **THEN** el sistema lo notifica y aplica la identidad en toda la interfaz
