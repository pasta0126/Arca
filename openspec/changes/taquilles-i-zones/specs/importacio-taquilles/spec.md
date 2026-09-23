## Purpose

Permitir dar de alta muchas taquillas a la vez desde un fichero CSV, con una plantilla descargable y una revisión previa que muestre qué se va a crear y qué filas tienen problemas antes de guardar nada.

## ADDED Requirements

### Requirement: Formato del fichero de importación
El sistema SHALL aceptar ficheros CSV en UTF-8 con o sin marca BOM, con separador punto y coma o coma detectado automáticamente, con una fila de cabecera y las columnas número, zona y nota opcional.

#### Scenario: Cabeceras en el idioma activo
- **WHEN** el fichero tiene las cabeceras "Número" y "Zona" en el idioma activo, en cualquier combinación de mayúsculas y acentos
- **THEN** el sistema las reconoce

#### Scenario: Columna opcional ausente
- **WHEN** el fichero no incluye la columna de nota
- **THEN** el sistema lo acepta y crea las taquillas sin nota

#### Scenario: Columna obligatoria ausente
- **WHEN** el fichero no incluye la columna de número o la de zona
- **THEN** el sistema rechaza el fichero completo con un error que indica la columna que falta

#### Scenario: Valores con separador
- **WHEN** una nota contiene el separador entre comillas
- **THEN** el sistema la lee como un único valor

#### Scenario: Codificación no válida
- **WHEN** el fichero no está en UTF-8 válido
- **THEN** el sistema lo rechaza con un error de codificación y no procesa ninguna fila

#### Scenario: Fichero vacío
- **WHEN** el fichero no contiene filas de datos
- **THEN** el sistema lo rechaza con un error de fichero vacío

#### Scenario: Fichero demasiado grande
- **WHEN** el fichero contiene más de 5000 filas de datos
- **THEN** el sistema lo rechaza con un error de tamaño excesivo

### Requirement: Plantilla descargable
El sistema SHALL ofrecer un fichero CSV de plantilla con las cabeceras en el idioma activo.

#### Scenario: Descargar la plantilla
- **WHEN** el usuario pide la plantilla
- **THEN** obtiene un CSV en UTF-8 con BOM y separador punto y coma que contiene solo la fila de cabecera

### Requirement: Revisión previa sin efectos
El sistema SHALL analizar el fichero y mostrar el resultado de la revisión antes de guardar, sin crear ni modificar ningún dato hasta que el usuario confirme.

#### Scenario: Resultado de la revisión
- **WHEN** el usuario carga un fichero
- **THEN** el sistema muestra cuántas filas son válidas, cuántas tienen errores y, para cada error, su número de línea y el motivo

#### Scenario: Cancelar tras la revisión
- **WHEN** el usuario cancela después de la revisión
- **THEN** no se crea ninguna taquilla ni ninguna zona

### Requirement: Validación de cada fila
El sistema SHALL marcar como errónea cada fila que incumpla las reglas de taquillas o del propio fichero, indicando el motivo.

#### Scenario: Número no válido
- **WHEN** una fila tiene un número vacío, no numérico, cero, negativo o superior a 99999
- **THEN** la fila se marca errónea por número no válido

#### Scenario: Número repetido dentro del fichero
- **WHEN** el mismo número aparece en dos o más filas
- **THEN** todas las filas afectadas se marcan erróneas por número repetido en el fichero

#### Scenario: Número ya en uso
- **WHEN** una fila usa el número de una taquilla activa existente
- **THEN** la fila se marca errónea por número en uso

#### Scenario: Número de una baja
- **WHEN** una fila usa el número de una taquilla que está de baja y ninguna activa lo usa
- **THEN** la fila es válida

#### Scenario: Zona vacía
- **WHEN** una fila no indica zona
- **THEN** la fila se marca errónea por zona obligatoria

#### Scenario: Zona desactivada
- **WHEN** una fila indica una zona que existe pero está desactivada
- **THEN** la fila se marca errónea por zona no disponible

#### Scenario: Zona con distinta grafía
- **WHEN** una fila indica "gimnas" y existe la zona activa "Gimnàs"
- **THEN** el sistema la reconoce como esa zona

#### Scenario: Nota demasiado larga
- **WHEN** una nota supera los 500 caracteres
- **THEN** la fila se marca errónea por longitud máxima

### Requirement: Zonas inexistentes
El sistema SHALL tratar como error las filas con una zona que no existe, salvo que el usuario elija en la revisión crear las zonas que faltan.

#### Scenario: Por defecto
- **WHEN** una fila indica una zona que no existe y el usuario no ha elegido crearlas
- **THEN** la fila se marca errónea por zona inexistente

#### Scenario: Crear las zonas que faltan
- **WHEN** el usuario elige crear las zonas que faltan
- **THEN** la revisión indica cuántas zonas nuevas se crearían y las filas afectadas pasan a ser válidas

#### Scenario: Nombres de zona equivalentes
- **WHEN** varias filas indican una zona inexistente con grafías equivalentes, como "Planta 3" y "planta 3"
- **THEN** se crearía una única zona

### Requirement: Confirmación e importación de las filas válidas
El sistema SHALL, tras la confirmación del usuario, crear todas las taquillas de las filas válidas en una única operación indivisible y SHALL no importar las filas con errores.

#### Scenario: Importación con errores
- **WHEN** el usuario confirma una revisión con 290 filas válidas y 10 erróneas
- **THEN** se crean las 290 taquillas libres, cada una con su evento de alta, y las 10 erróneas no se importan y se informa de ello

#### Scenario: Todas las filas erróneas
- **WHEN** el usuario intenta confirmar una revisión sin ninguna fila válida
- **THEN** el sistema no ofrece la confirmación

#### Scenario: Fallo al guardar
- **WHEN** ocurre un error al guardar a mitad de la importación
- **THEN** no queda creada ninguna taquilla ni ninguna zona de la importación

#### Scenario: Datos cambiados entre la revisión y la confirmación
- **WHEN** entre la revisión y la confirmación una fila válida pasa a chocar con una taquilla creada mientras tanto
- **THEN** el sistema vuelve a validar al confirmar y no crea ningún dato hasta mostrar de nuevo la revisión actualizada

#### Scenario: Importar el mismo fichero dos veces
- **WHEN** el usuario importa un fichero ya importado
- **THEN** todas sus filas se marcan erróneas por número en uso y no se crean duplicados

### Requirement: Progreso, cancelación y resultado de la importación
El sistema SHALL mostrar el progreso del análisis y de la importación con recuentos, SHALL permitir cancelar antes del guardado y SHALL informar del resultado final.

#### Scenario: Progreso del análisis
- **WHEN** el sistema analiza un fichero de 300 filas
- **THEN** muestra el avance con recuentos y la interfaz sigue respondiendo

#### Scenario: Cancelar durante el análisis
- **WHEN** el usuario cancela mientras se analiza el fichero
- **THEN** el análisis se detiene y no se crea ningún dato

#### Scenario: Resultado final
- **WHEN** la importación termina
- **THEN** el sistema informa de cuántas taquillas se han creado, cuántas zonas nuevas se han creado y cuántas filas se han omitido

### Requirement: Confirmación de la importación
El sistema SHALL pedir confirmación explícita antes de guardar una importación, indicando cuántas taquillas y zonas se crearán.

#### Scenario: Diálogo de confirmación
- **WHEN** el usuario pulsa confirmar en la revisión
- **THEN** el sistema pide confirmación indicando el número de taquillas y de zonas que se crearán, y no guarda hasta que el usuario confirme

### Requirement: Estado inicial de las taquillas importadas
El sistema SHALL crear todas las taquillas importadas en estado libre.

#### Scenario: Fichero con columna de estado
- **WHEN** el fichero incluye una columna adicional no reconocida, como un estado
- **THEN** el sistema la ignora, lo indica en la revisión y crea las taquillas libres
