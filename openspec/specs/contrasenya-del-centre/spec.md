# contrasenya-del-centre Specification

## Purpose
Definir la contraseña compartida del centro que desbloquea los datos: cómo se crea, cómo se pide al abrir la aplicación y cómo se cambia.

## Requirements

### Requirement: Contraseña obligatoria de al menos 12 caracteres en la primera ejecución
El sistema SHALL exigir la contraseña del centro al crear la base de datos en la primera ejecución, de al menos 12 caracteres de cualquier tipo y sin más reglas de composición, confirmada escribiéndola dos veces, y SHALL aplicar los mismos requisitos cada vez que se cambie.

#### Scenario: Contraseña válida
- **WHEN** el usuario escribe dos veces "riu cadira blau gos"
- **THEN** el sistema la acepta y continúa con la creación

#### Scenario: Sin reglas de composición
- **WHEN** el usuario escribe "insmonturiol", de 12 letras minúsculas
- **THEN** el sistema la acepta porque cumple la longitud, y el indicador de fortaleza le avisa de que es una sola palabra

#### Scenario: Demasiado corta
- **WHEN** el usuario escribe una contraseña de menos de 12 caracteres
- **THEN** el sistema la rechaza indicando la longitud mínima

#### Scenario: Contador de longitud
- **WHEN** el usuario escribe la contraseña
- **THEN** ve cuántos caracteres lleva sobre el mínimo, por ejemplo "9 de 12"

#### Scenario: Contraseña demasiado habitual
- **WHEN** el usuario escribe una contraseña de la lista de contraseñas habituales, como "contrasenya1234", o una repetición o secuencia obvia, como "111111111111" o "qwertyuiop12"
- **THEN** el sistema la rechaza indicando que es demasiado habitual

#### Scenario: No coinciden
- **WHEN** las dos contraseñas escritas son distintas
- **THEN** el sistema lo indica y no continúa

#### Scenario: Caracteres libres
- **WHEN** la contraseña incluye espacios, acentos, "ç" o "l·l"
- **THEN** el sistema los acepta y los trata sin alterarlos

#### Scenario: Sin contraseña
- **WHEN** el usuario intenta continuar sin contraseña
- **THEN** el sistema no lo permite

#### Scenario: Se establece al empezar
- **WHEN** el usuario elige empezar de cero en la primera ejecución
- **THEN** la creación de la contraseña es un paso obligatorio antes de crear la base de datos

### Requirement: Indicador de fortaleza y aviso de pérdida
El sistema SHALL mostrar al crear la contraseña un indicador de su fortaleza y SHALL advertir con claridad de que, si se pierden la contraseña y la clave de recuperación, los datos no se podrán recuperar.

#### Scenario: Aviso visible
- **WHEN** el usuario crea la contraseña
- **THEN** ve el aviso de que sin contraseña ni clave de recuperación los datos son irrecuperables

#### Scenario: Contraseña débil
- **WHEN** la contraseña cumple el mínimo pero es una sola palabra, solo minúsculas seguidas o un patrón previsible
- **THEN** el indicador lo señala y sugiere usar varias palabras sin relación, sin impedir continuar

### Requirement: Contraseña al abrir la aplicación
El sistema SHALL pedir la contraseña del centro cada vez que se abre la aplicación, antes de abrir la base de datos, y SHALL no mostrar ningún dato hasta que sea correcta.

#### Scenario: Contraseña correcta
- **WHEN** el usuario escribe la contraseña correcta
- **THEN** se abre la base de datos y la aplicación continúa su arranque

#### Scenario: Contraseña incorrecta
- **WHEN** el usuario escribe una contraseña incorrecta
- **THEN** el sistema lo indica con un mensaje claro, sin revelar nada más, y permite volver a intentarlo

#### Scenario: Cancelar
- **WHEN** el usuario cancela la petición de contraseña
- **THEN** la aplicación se cierra sin abrir la base de datos

#### Scenario: Ofrecer la recuperación
- **WHEN** el usuario indica que ha olvidado la contraseña
- **THEN** el sistema ofrece entrar con la clave de recuperación

### Requirement: La contraseña nunca se guarda ni se registra
El sistema SHALL no guardar la contraseña en ningún fichero, no incluirla en el registro técnico ni en mensajes de error y no enviarla fuera del equipo.

#### Scenario: Registro técnico
- **WHEN** falla un desbloqueo
- **THEN** el registro técnico no contiene la contraseña ni derivados de ella

#### Scenario: Sin persistencia
- **WHEN** se inspeccionan los ficheros de la aplicación tras cerrarla
- **THEN** no aparece la contraseña

### Requirement: Cambiar la contraseña
El sistema SHALL permitir cambiar la contraseña con la aplicación desbloqueada, pidiendo la contraseña actual y la nueva dos veces, sin recifrar la base de datos, e informar de que las copias de seguridad anteriores conservan la contraseña con la que se hicieron.

#### Scenario: Cambio correcto
- **WHEN** el usuario cambia la contraseña indicando la actual y una nueva válida
- **THEN** la nueva contraseña abre la base de datos desde ese momento y la anterior deja de funcionar

#### Scenario: Contraseña actual incorrecta
- **WHEN** el usuario escribe mal la contraseña actual
- **THEN** el sistema rechaza el cambio y no modifica nada

#### Scenario: Aviso sobre las copias
- **WHEN** el usuario completa el cambio
- **THEN** el sistema recuerda que las copias hechas antes se abren con la contraseña anterior

#### Scenario: Nueva contraseña que no cumple los requisitos
- **WHEN** el usuario escribe una contraseña nueva que no cumple los requisitos
- **THEN** el sistema la rechaza indicando el motivo y no cambia nada

#### Scenario: Fallo a mitad
- **WHEN** ocurre un error durante el cambio
- **THEN** la contraseña anterior sigue funcionando

### Requirement: Bloqueo sin interrupciones
El sistema SHALL integrar la petición de contraseña en la pantalla de arranque, sin bloquear el resto de etapas ni aceptar interacción con otras funciones hasta desbloquear.

#### Scenario: Arranque
- **WHEN** la aplicación arranca
- **THEN** la pantalla de arranque indica que espera la contraseña y las etapas que dependen de la base continúan tras desbloquear

### Requirement: Feedback y guía
El sistema SHALL informar del resultado de cada operación con contraseña con mensajes comprensibles, permitir operar con teclado y evitar la doble ejecución.

#### Scenario: Doble Intro
- **WHEN** el usuario pulsa dos veces Intro al enviar la contraseña
- **THEN** se intenta una sola vez

#### Scenario: Solo teclado
- **WHEN** el usuario no usa el ratón
- **THEN** puede escribir la contraseña, enviarla y cancelar con el teclado
