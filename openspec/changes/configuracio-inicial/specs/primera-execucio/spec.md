## Purpose

Definir qué ocurre al arrancar ARCA por primera vez en un equipo, antes de que exista la base de datos: dónde se guardan los datos y si se empieza de cero o desde una copia de seguridad.

## ADDED Requirements

### Requirement: Detección de primera ejecución
El sistema SHALL detectar que no existe una base de datos en la ubicación configurada y SHALL mostrar entonces la pantalla de primera ejecución en lugar de crear una base vacía en silencio.

#### Scenario: Instalación nueva
- **WHEN** el usuario abre ARCA por primera vez y no existe la base de datos
- **THEN** ve la pantalla de primera ejecución con las opciones de empezar de cero o restaurar una copia

#### Scenario: Base de datos existente
- **WHEN** existe la base de datos en la ubicación configurada
- **THEN** el sistema arranca con normalidad sin mostrar la primera ejecución

#### Scenario: Base de datos dañada
- **WHEN** el fichero existe pero está dañado
- **THEN** el sistema aplica el tratamiento de fichero dañado y no muestra la primera ejecución

### Requirement: Carpeta de datos visible y modificable
El sistema SHALL mostrar en la primera ejecución la carpeta de datos propuesta, SHALL permitir elegir otra antes de crear nada y SHALL comprobar que admite escritura.

#### Scenario: Ubicación propuesta
- **WHEN** el usuario abre la primera ejecución
- **THEN** ve la carpeta de datos por defecto del sistema operativo

#### Scenario: Elegir otra carpeta
- **WHEN** el usuario elige otra carpeta con permisos
- **THEN** se guarda como ruta configurada y se usa para crear la base de datos

#### Scenario: Carpeta sin permisos
- **WHEN** el usuario elige una carpeta que no admite escritura
- **THEN** el sistema lo rechaza con un mensaje comprensible y no crea nada

#### Scenario: Ya existe una base de datos en esa carpeta
- **WHEN** el usuario elige una carpeta que ya contiene una base de datos de ARCA
- **THEN** el sistema pregunta si usarla y, si la elige, arranca con ella sin crear otra

### Requirement: Empezar de cero
El sistema SHALL crear una base de datos nueva aplicando todas las migraciones cuando el usuario elige empezar de cero, y SHALL continuar con el asistente de configuración.

#### Scenario: Base de datos nueva
- **WHEN** el usuario elige empezar de cero
- **THEN** se crea la base de datos y se abre el asistente de configuración en su primer paso pendiente

#### Scenario: Fallo al crear
- **WHEN** falla la creación de la base de datos
- **THEN** el sistema muestra un mensaje claro, no deja un fichero a medias y permite reintentar o elegir otra carpeta

### Requirement: Restaurar una copia en una instalación nueva
El sistema SHALL ofrecer en la primera ejecución restaurar una copia de seguridad con el asistente de restauración, y SHALL arrancar con los datos restaurados, sin repetir los pasos que ya están hechos.

#### Scenario: Restaurar en un equipo nuevo
- **WHEN** el usuario elige restaurar y selecciona una copia válida
- **THEN** la aplicación arranca con los datos de la copia y el asistente de configuración solo muestra los pasos que sigan pendientes

#### Scenario: Copia rechazada
- **WHEN** la copia no supera la verificación
- **THEN** el sistema lo indica y permite elegir otra copia o empezar de cero

### Requirement: Licencia desde el primer arranque
El sistema SHALL iniciar el período de prueba en la primera ejecución sin clave, SHALL permitir introducir una clave en cualquier momento del asistente y SHALL no exigir conexión.

#### Scenario: Sin clave
- **WHEN** el usuario empieza de cero sin introducir clave
- **THEN** la prueba está en marcha con sus días restantes

### Requirement: Feedback en la primera ejecución
El sistema SHALL explicar cada opción con su consecuencia, informar del resultado y de la ubicación de los datos, y evitar la doble ejecución.

#### Scenario: Doble clic
- **WHEN** el usuario pulsa dos veces empezar de cero
- **THEN** la base de datos se crea una sola vez

#### Scenario: Confirmación de ubicación
- **WHEN** se crea la base de datos
- **THEN** el sistema indica dónde se guardan los datos
