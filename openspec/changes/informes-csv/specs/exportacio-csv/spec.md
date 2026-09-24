## Purpose

Definir cómo se genera un fichero CSV a partir de cualquier informe: formato, destino, protección de datos y comportamiento ante errores.

## ADDED Requirements

### Requirement: Formato del fichero
El sistema SHALL generar los ficheros CSV en UTF-8 con marca de orden de bytes, con `;` como separador, comillas dobles para los campos que las necesiten y una fila de cabeceras.

#### Scenario: Fichero abierto en una hoja de cálculo
- **WHEN** el usuario exporta un informe con nombres con acentos y "l·l"
- **THEN** el fichero conserva los caracteres sin alteración y usa `;` como separador

#### Scenario: Campo con separador o comillas
- **WHEN** un valor contiene `;`, comillas o un salto de línea
- **THEN** se escribe entre comillas dobles con las comillas duplicadas

### Requirement: Cabeceras y valores según la cultura activa
El sistema SHALL escribir las cabeceras a partir de claves de recurso del idioma activo, y las fechas y los importes con el formato de la cultura catalana, sin símbolo de moneda ni separador de millares.

#### Scenario: Importe y fecha
- **WHEN** un informe incluye un importe de 12,5 euros y una fecha de pago
- **THEN** el importe se escribe como "12,50" (y uno de 1234,5 como "1234,50") y la fecha con el formato catalán de fecha corta

### Requirement: Protección contra fórmulas
El sistema SHALL neutralizar los valores de texto que empiecen por `=`, `+`, `-`, `@`, tabulador o retorno de carro anteponiendo una comilla simple, para que una hoja de cálculo no los interprete como fórmula.

#### Scenario: Nombre que parece una fórmula
- **WHEN** un dato de texto empieza por `=`
- **THEN** se escribe con una comilla simple delante

#### Scenario: Importe negativo
- **WHEN** un importe es numérico
- **THEN** se escribe como número sin la protección

### Requirement: Elección del destino
El sistema SHALL proponer un nombre de fichero con el nombre del informe y la fecha, permitir elegir la carpeta y pedir confirmación antes de sobrescribir un fichero existente.

#### Scenario: Nombre propuesto
- **WHEN** el usuario exporta el informe de morosos el 24 de septiembre de 2026
- **THEN** el sistema propone un nombre que incluye el informe y esa fecha

#### Scenario: Fichero existente
- **WHEN** el destino ya existe
- **THEN** el sistema pide confirmar la sobrescritura y no actúa hasta entonces

### Requirement: Escritura atómica
El sistema SHALL escribir el fichero de forma que un error o una cancelación no dejen un fichero incompleto en el destino ni destruyan uno existente.

#### Scenario: Fallo a mitad
- **WHEN** ocurre un error de escritura durante la exportación
- **THEN** no queda ningún fichero parcial, el fichero anterior sigue intacto y el usuario recibe un mensaje claro

#### Scenario: Cancelación
- **WHEN** el usuario cancela una exportación larga
- **THEN** no se crea ni se modifica ningún fichero

#### Scenario: Destino no disponible
- **WHEN** la carpeta de destino no existe o no tiene permisos
- **THEN** el sistema lo indica con un mensaje comprensible y sugiere elegir otra carpeta

### Requirement: Resultado sin datos
El sistema SHALL no crear un fichero cuando el informe con los filtros elegidos no tiene filas, e informar de ello al usuario.

#### Scenario: Sin morosos
- **WHEN** el usuario exporta morosos y no hay ninguno
- **THEN** el sistema indica que no hay datos que exportar y no crea el fichero

### Requirement: Datos personales mínimos
El sistema SHALL incluir en los informes con alumnos solo nombre y apellidos como datos de identificación, SHALL excluir correo, identificador, notas y motivos escritos por el usuario, y SHALL avisar antes de exportar de que el fichero contiene datos de menores.

#### Scenario: Aviso previo
- **WHEN** el usuario exporta un informe con alumnos
- **THEN** el sistema avisa de que el fichero contiene datos de menores y de que su custodia corresponde al centro, y continúa tras la confirmación

#### Scenario: Informe sin alumnos
- **WHEN** el usuario exporta el resumen de cobros o el de taquillas
- **THEN** no se muestra el aviso porque el fichero no contiene datos de alumnos

#### Scenario: Registro técnico
- **WHEN** falla una exportación
- **THEN** el registro técnico no contiene nombres ni valores del informe

### Requirement: Feedback y progreso
El sistema SHALL informar del resultado de cada exportación con el número de filas y la ubicación del fichero, mostrar el avance con recuentos en las exportaciones largas, no bloquear la interfaz y evitar exportar dos veces por un doble clic.

#### Scenario: Resultado
- **WHEN** termina una exportación de 120 filas
- **THEN** el sistema indica 120 filas exportadas y dónde se ha guardado el fichero

#### Scenario: Doble clic
- **WHEN** el usuario pulsa dos veces exportar
- **THEN** la exportación se ejecuta una sola vez
