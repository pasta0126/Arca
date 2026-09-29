## RENAMED Requirements
- FROM: `### Requirement: Contraseña obligatoria de al menos 6 caracteres en la primera ejecución`
- TO: `### Requirement: Contraseña obligatoria de al menos 4 caracteres en la primera ejecución`

## MODIFIED Requirements

### Requirement: Indicador de fortaleza y aviso de pérdida
El sistema SHALL mostrar al crear la contraseña un indicador de su fortaleza y SHALL advertir con claridad de que, si se pierden la contraseña y la clave de recuperación, los datos no se podrán recuperar. Cuando la contraseña sea débil, el sistema SHALL explicar el riesgo, los criterios de una contraseña fuerte y un ejemplo, sin impedir continuar.

#### Scenario: Aviso visible
- **WHEN** el usuario crea la contraseña
- **THEN** ve el aviso de que sin contraseña ni clave de recuperación los datos son irrecuperables

#### Scenario: Contraseña débil
- **WHEN** la contraseña cumple el mínimo pero tiene menos de 12 caracteres, es una sola palabra, son solo dígitos, tiene muy pocos símbolos distintos, es de la lista de habituales o es una repetición o secuencia obvia
- **THEN** el indicador lo señala como débil y el sistema muestra el riesgo (que se puede adivinar si alguien consigue el fichero de datos), los criterios de una contraseña fuerte (12 o más caracteres y varias palabras sin relación) y un ejemplo, sin impedir continuar

#### Scenario: Contraseña no débil
- **WHEN** la contraseña tiene 12 o más caracteres y varias palabras sin relación
- **THEN** el indicador la señala como aceptable o buena y no se muestra el aviso de riesgo

#### Scenario: El ejemplo no se puede usar
- **WHEN** el usuario escribe tal cual el ejemplo de contraseña fuerte que muestra el aviso
- **THEN** el sistema lo acepta como cualquier otra contraseña, porque ya no se rechaza ninguna por ser habitual

### Requirement: Contraseña obligatoria de al menos 4 caracteres en la primera ejecución
El sistema SHALL exigir la contraseña del centro al crear la base de datos en la primera ejecución, de al menos 4 caracteres de cualquier tipo y sin más reglas de composición, confirmada escribiéndola dos veces, y SHALL aplicar los mismos requisitos cada vez que se cambie. El mínimo SHALL comprobarse solo al crear o cambiar la contraseña, nunca al abrir la aplicación.

#### Scenario: Contraseña válida
- **WHEN** el usuario escribe dos veces "riu cadira blau gos"
- **THEN** el sistema la acepta y continúa con la creación

#### Scenario: Contraseña corta permitida
- **WHEN** el usuario escribe dos veces "cotx", de 4 caracteres
- **THEN** el sistema la acepta y continúa con la creación, y el indicador de fortaleza la señala como débil

#### Scenario: Sin reglas de composición
- **WHEN** el usuario escribe "insmonturiol", de 12 letras minúsculas
- **THEN** el sistema la acepta porque cumple la longitud, y el indicador de fortaleza le avisa de que es una sola palabra

#### Scenario: Demasiado corta
- **WHEN** el usuario escribe una contraseña de menos de 4 caracteres
- **THEN** el sistema la rechaza indicando la longitud mínima

#### Scenario: Contador de longitud
- **WHEN** el usuario escribe la contraseña
- **THEN** ve cuántos caracteres lleva junto al mínimo y al recomendado, por ejemplo "3 caracteres (mínimo 4, recomendado 12)"

#### Scenario: Contraseña demasiado habitual
- **WHEN** el usuario escribe una contraseña de la lista de habituales, como "contrasenya1234", o una repetición o secuencia obvia, como "1111" o "qwerty"
- **THEN** el sistema la acepta y el indicador la señala como débil con el aviso de riesgo, sin rechazarla

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

#### Scenario: Contraseñas ya creadas
- **WHEN** el usuario abre la aplicación con una contraseña creada antes de este cambio
- **THEN** el sistema la acepta sin comprobar su longitud ni su fortaleza
