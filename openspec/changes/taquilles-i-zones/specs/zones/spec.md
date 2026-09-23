## Purpose

Permitir organizar las taquillas en zonas o pasillos con nombre propio, con un catálogo estable que se puede ampliar y renombrar sin perder el historial de las taquillas.

## ADDED Requirements

### Requirement: Crear zonas
El sistema SHALL permitir crear una zona con un nombre, que queda activa desde su creación.

#### Scenario: Zona nueva
- **WHEN** el usuario crea una zona con el nombre "Planta 1"
- **THEN** la zona queda creada y activa, y está disponible para asignarle taquillas

#### Scenario: Nombre vacío
- **WHEN** el usuario intenta crear una zona con un nombre vacío o solo con espacios
- **THEN** el sistema la rechaza con un error de negocio de nombre obligatorio

#### Scenario: Nombre demasiado largo
- **WHEN** el usuario intenta crear una zona con un nombre de más de 60 caracteres
- **THEN** el sistema la rechaza con un error de longitud máxima

#### Scenario: Espacios sobrantes
- **WHEN** el usuario crea una zona con el nombre "  Gimnasio  "
- **THEN** la zona se guarda con el nombre "Gimnasio"

### Requirement: Nombre de zona único
El sistema SHALL impedir dos zonas con el mismo nombre, comparando sin distinguir mayúsculas ni acentos.

#### Scenario: Nombre duplicado
- **WHEN** existe la zona "Gimnàs" y el usuario crea otra llamada "gimnas"
- **THEN** el sistema rechaza la creación con un error de nombre duplicado

#### Scenario: Nombre de una zona desactivada
- **WHEN** existe una zona desactivada llamada "Planta 2" y el usuario crea una nueva con ese nombre
- **THEN** el sistema rechaza la creación con un error de nombre duplicado

### Requirement: Renombrar zonas
El sistema SHALL permitir cambiar el nombre de una zona, respetando las mismas reglas de validez y unicidad, y conservando las taquillas y su historial.

#### Scenario: Renombrado correcto
- **WHEN** el usuario renombra "Planta 1" como "Primera planta"
- **THEN** todas las taquillas de la zona pasan a mostrarse con el nuevo nombre

#### Scenario: Renombrar con el mismo nombre
- **WHEN** el usuario renombra una zona con su nombre actual, cambiando solo mayúsculas o acentos
- **THEN** el sistema acepta el cambio y no lo considera duplicado consigo misma

#### Scenario: Renombrar a un nombre existente
- **WHEN** el usuario renombra una zona con el nombre de otra
- **THEN** el sistema lo rechaza con un error de nombre duplicado

### Requirement: Desactivar y reactivar zonas
El sistema SHALL permitir desactivar una zona que no tenga taquillas activas y reactivarla más tarde.

#### Scenario: Desactivar zona sin taquillas activas
- **WHEN** el usuario desactiva una zona cuyas taquillas están todas de baja o que no tiene ninguna
- **THEN** la zona queda desactivada, no admite nuevas taquillas y deja de ofrecerse por defecto en las selecciones

#### Scenario: Desactivar zona con taquillas activas
- **WHEN** el usuario intenta desactivar una zona que tiene al menos una taquilla que no está de baja
- **THEN** el sistema lo rechaza con un error que indica que hay taquillas activas

#### Scenario: Reactivar
- **WHEN** el usuario reactiva una zona desactivada
- **THEN** la zona vuelve a admitir taquillas, siempre que su nombre siga sin duplicarse

### Requirement: Conservación de zonas con historial
El sistema SHALL impedir eliminar una zona que haya tenido alguna taquilla y SHALL permitir eliminar solo las zonas que nunca han tenido ninguna.

#### Scenario: Eliminar zona sin uso
- **WHEN** el usuario elimina una zona recién creada que nunca ha tenido taquillas
- **THEN** la zona desaparece del catálogo

#### Scenario: Eliminar zona con historial
- **WHEN** el usuario intenta eliminar una zona que tiene o tuvo taquillas, aunque estén de baja
- **THEN** el sistema lo rechaza y sugiere desactivarla

### Requirement: Listado de zonas
El sistema SHALL listar las zonas ordenadas alfabéticamente según el catalán, con su estado y el número de taquillas activas de cada una.

#### Scenario: Listado por defecto
- **WHEN** el usuario consulta las zonas
- **THEN** ve las zonas activas ordenadas y puede pedir incluir también las desactivadas

#### Scenario: Recuento de taquillas
- **WHEN** una zona tiene 40 taquillas activas y 5 de baja
- **THEN** el recuento de taquillas activas de la zona es 40
