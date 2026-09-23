## Purpose

Permitir recuperar los datos desde una copia de seguridad de forma guiada y segura, de modo que restaurar nunca pierda los datos actuales sin remedio.

## ADDED Requirements

### Requirement: Restauración guiada
El sistema SHALL ofrecer la restauración como un asistente de pasos: elegir el fichero, verificarlo, ver su contenido, confirmar y aplicar.

#### Scenario: Restauración correcta
- **WHEN** el usuario elige una copia válida, revisa su contenido y confirma
- **THEN** los datos de la aplicación pasan a ser los de la copia y el sistema informa del resultado

### Requirement: Verificación previa de la copia
El sistema SHALL comprobar antes de restaurar que el fichero es una base de datos de ARCA, que se abre con la clave de la aplicación, que supera la comprobación de integridad y que su versión de esquema es conocida, sin modificar nada hasta la confirmación.

#### Scenario: Fichero que no es una copia
- **WHEN** el usuario elige un fichero que no es una copia de ARCA
- **THEN** el sistema lo rechaza con un mensaje claro y no cambia nada

#### Scenario: Copia dañada
- **WHEN** la copia no supera la comprobación de integridad
- **THEN** el sistema lo rechaza indicando que está dañada y no cambia nada

#### Scenario: Copia de una versión más nueva
- **WHEN** la copia procede de una versión de ARCA más nueva que la instalada
- **THEN** el sistema la rechaza indicando que hay que actualizar la aplicación y no cambia nada

#### Scenario: Copia de una versión anterior
- **WHEN** la copia procede de una versión anterior
- **THEN** el sistema la acepta e informa de que se actualizará al restaurar

### Requirement: Vista previa del contenido
El sistema SHALL mostrar antes de confirmar qué contiene la copia: la fecha de creación del fichero, la versión y los recuentos de cursos, alumnos, taquillas y asignaciones, sin datos personales, y compararlos con los datos actuales.

#### Scenario: Comparación
- **WHEN** el usuario elige una copia
- **THEN** ve para la copia y para los datos actuales los recuentos de cursos, alumnos, taquillas y asignaciones

#### Scenario: Copia más antigua que los datos actuales
- **WHEN** la copia tiene menos alumnos que los datos actuales
- **THEN** el sistema resalta la diferencia y advierte de que se perderá lo posterior a la copia

### Requirement: Confirmación con la consecuencia
El sistema SHALL exigir una confirmación explícita que indique que los datos actuales serán sustituidos y que se guardará una copia previa de ellos, y SHALL no actuar hasta entonces.

#### Scenario: Confirmación pendiente
- **WHEN** el usuario llega al último paso
- **THEN** el sistema muestra la consecuencia y no restaura hasta la confirmación

### Requirement: Copia previa automática de los datos actuales
El sistema SHALL guardar, antes de sustituir nada, una copia verificada de los datos actuales junto a la base de datos, y SHALL conservar únicamente las 3 copias previas a restauración más recientes.

#### Scenario: Copia previa
- **WHEN** el usuario confirma la restauración
- **THEN** el sistema guarda y verifica una copia de los datos actuales antes de sustituirlos

#### Scenario: Retención
- **WHEN** se completa una restauración y ya existían 3 copias previas a restauración
- **THEN** se elimina la más antigua

#### Scenario: Copia previa que falla
- **WHEN** no se puede crear o verificar la copia previa
- **THEN** el sistema no restaura, informa del motivo y los datos actuales no cambian

### Requirement: Sustitución atómica y recuperación automática
El sistema SHALL sustituir los datos actuales por los de la copia de forma que, si algo falla en cualquier punto, los datos actuales se recuperen automáticamente de la copia previa y la aplicación siga funcionando con ellos.

#### Scenario: Fallo al sustituir
- **WHEN** ocurre un error durante la sustitución
- **THEN** los datos actuales quedan como antes, el sistema informa del error y la aplicación sigue usable

#### Scenario: Fallo al migrar una copia antigua
- **WHEN** la copia es de una versión anterior y la migración falla
- **THEN** el sistema recupera los datos actuales y no deja los datos de la copia a medias

### Requirement: Migración de copias antiguas
El sistema SHALL migrar al esquema actual una copia de una versión anterior al restaurarla, con el migrador y sus garantías, y SHALL conservar la copia original sin modificar.

#### Scenario: Restaurar una copia antigua
- **WHEN** el usuario restaura una copia de una versión anterior
- **THEN** los datos quedan migrados al esquema actual y el fichero de la copia sigue intacto

### Requirement: Estado de la aplicación tras restaurar
El sistema SHALL dejar la aplicación en un estado coherente con los datos restaurados, sin conservar información de los datos anteriores, y SHALL guiar al usuario tras la restauración.

#### Scenario: Recarga
- **WHEN** termina la restauración
- **THEN** la aplicación muestra los datos restaurados, incluido el curso que estaba activo en la copia, e indica dónde está la copia previa

### Requirement: Restauración siempre disponible
El sistema SHALL permitir restaurar en cualquier estado de la licencia y sin conexión, y SHALL ofrecer la restauración también cuando la base de datos actual está dañada o no se puede abrir.

#### Scenario: Base de datos dañada
- **WHEN** la aplicación no puede abrir la base de datos por fichero corrupto
- **THEN** el sistema ofrece restaurar una copia, y conserva el fichero dañado sin modificar como copia previa

#### Scenario: Licencia caducada
- **WHEN** la licencia está en modo de solo lectura
- **THEN** el usuario puede restaurar una copia

### Requirement: Restauración en otro equipo
El sistema SHALL permitir restaurar en un equipo distinto o con otro sistema operativo, o en una instalación nueva sin datos, una copia hecha en otro equipo.

#### Scenario: Instalación nueva
- **WHEN** el usuario instala ARCA en un PC nuevo y restaura una copia de otro equipo
- **THEN** la aplicación funciona con los datos de la copia

### Requirement: Feedback y guía en la restauración
El sistema SHALL informar en cada paso, mostrar el progreso sin bloquear la interfaz, evitar restauraciones duplicadas por doble clic, permitir cancelar antes de sustituir y no incluir datos de alumnos en el registro técnico.

#### Scenario: Cancelar antes de sustituir
- **WHEN** el usuario cancela en cualquier paso anterior a la confirmación
- **THEN** no cambia ningún dato

#### Scenario: Doble clic
- **WHEN** el usuario pulsa dos veces confirmar
- **THEN** la restauración se ejecuta una sola vez

#### Scenario: Error comprensible
- **WHEN** falla una restauración
- **THEN** el sistema muestra un mensaje claro sin detalles técnicos y el registro técnico no contiene datos de alumnos
