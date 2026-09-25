# taquilles Specification

## Purpose
Gestionar el inventario de taquillas del centro: su identificación, su zona, su estado, su alta y baja y su historial, como base sobre la que se apoyan las asignaciones, los cobros y el mantenimiento.

## Requirements

### Requirement: Identidad de la taquilla y número visible
El sistema SHALL identificar cada taquilla con un identificador interno inmutable y SHALL mostrarla al usuario por su número, un entero positivo de hasta cinco cifras.

#### Scenario: Número no válido
- **WHEN** el usuario intenta dar de alta una taquilla con número cero, negativo, decimal o superior a 99999
- **THEN** el sistema la rechaza con un error de número no válido

#### Scenario: Historial ligado a la identidad
- **WHEN** una taquilla cambia de número
- **THEN** su historial y todo lo asociado a ella se conservan porque están ligados al identificador interno y no al número

### Requirement: Número único entre taquillas activas
El sistema SHALL impedir dos taquillas activas con el mismo número, entendiendo por activa la que no está de baja, y SHALL permitir que una taquilla de baja comparta número con otra activa o con otra de baja.

#### Scenario: Número duplicado entre activas
- **WHEN** existe la taquilla activa número 15 y el usuario da de alta otra con el número 15
- **THEN** el sistema lo rechaza con un error de número en uso

#### Scenario: Reutilizar el número de una baja
- **WHEN** la taquilla 15 está de baja y el usuario da de alta una nueva con el número 15
- **THEN** el sistema la crea y ambas taquillas coexisten con historiales independientes

#### Scenario: Varias bajas con el mismo número
- **WHEN** existen dos taquillas de baja con el número 15 y se da de alta una activa con el número 15
- **THEN** el sistema la crea sin conflicto

### Requirement: Alta individual de una taquilla
El sistema SHALL permitir dar de alta una taquilla indicando su número, una zona activa y opcionalmente una nota, y SHALL crearla en estado libre.

#### Scenario: Alta correcta
- **WHEN** el usuario da de alta la taquilla 101 en la zona activa "Planta 1"
- **THEN** la taquilla queda creada, libre y asociada a esa zona, y se registra el evento de alta en su historial

#### Scenario: Zona desactivada
- **WHEN** el usuario intenta dar de alta una taquilla en una zona desactivada
- **THEN** el sistema lo rechaza con un error de zona no disponible

#### Scenario: Nota demasiado larga
- **WHEN** el usuario indica una nota de más de 500 caracteres
- **THEN** el sistema lo rechaza con un error de longitud máxima

### Requirement: Alta por rangos
El sistema SHALL permitir dar de alta varias taquillas consecutivas indicando el primer número, el último y una zona activa, con una vista previa antes de confirmar y sin efectos parciales.

#### Scenario: Rango correcto
- **WHEN** el usuario indica del 1 al 40 en la zona "Planta 1" y confirma
- **THEN** se crean 40 taquillas libres en esa zona, cada una con su evento de alta

#### Scenario: Vista previa
- **WHEN** el usuario indica un rango
- **THEN** el sistema muestra cuántas taquillas se crearían y cuáles chocan con números activos existentes, sin crear nada todavía

#### Scenario: Conflicto con números existentes
- **WHEN** el rango incluye al menos un número que ya pertenece a una taquilla activa
- **THEN** el sistema no crea ninguna taquilla y lista todos los números en conflicto

#### Scenario: Rango invertido
- **WHEN** el primer número es mayor que el último
- **THEN** el sistema lo rechaza con un error de rango no válido

#### Scenario: Rango demasiado grande
- **WHEN** el rango incluye más de 1000 taquillas
- **THEN** el sistema lo rechaza con un error de rango excesivo

#### Scenario: Rango de un solo número
- **WHEN** el primer y el último número coinciden
- **THEN** el sistema crea una única taquilla

#### Scenario: Fallo durante la creación
- **WHEN** ocurre un error al guardar a mitad de la operación
- **THEN** no queda creada ninguna taquilla del rango

### Requirement: Estado visible derivado
El sistema SHALL determinar el estado visible de una taquilla a partir de sus hechos, aplicando esta precedencia: de baja, fuera de servicio (averiada o en mantenimiento), ocupada, reservada y libre.

#### Scenario: Taquilla sin hechos
- **WHEN** una taquilla no está de baja, no está averiada, no tiene asignación y no está reservada
- **THEN** su estado es libre

#### Scenario: Taquilla averiada con alumno
- **WHEN** una taquilla está averiada y conserva una asignación
- **THEN** su estado visible es averiada y se indica además que conserva asignación

#### Scenario: Taquilla reservada y averiada
- **WHEN** una taquilla reservada se marca como averiada
- **THEN** su estado visible es averiada y conserva la reserva

#### Scenario: Estado tras resolver la avería
- **WHEN** se resuelve la avería de una taquilla que además estaba ocupada
- **THEN** su estado visible vuelve a ser ocupado sin ninguna otra acción

### Requirement: Reserva con nota opcional
El sistema SHALL permitir reservar una taquilla libre, con una nota libre opcional, y quitar la reserva después. La reserva no se asocia a ningún alumno en este cambio.

#### Scenario: Reservar sin nota
- **WHEN** el usuario reserva una taquilla libre sin indicar nota
- **THEN** la taquilla queda reservada sin nota y se registra el evento en su historial

#### Scenario: Reservar con nota
- **WHEN** el usuario reserva una taquilla libre con la nota "Profesorado de educación física"
- **THEN** la taquilla queda reservada, no es asignable y se registra el evento en su historial

#### Scenario: Reservar una taquilla ocupada
- **WHEN** el usuario intenta reservar una taquilla que tiene una asignación
- **THEN** el sistema lo rechaza con un error de taquilla no libre

#### Scenario: Quitar la reserva
- **WHEN** el usuario quita la reserva de una taquilla reservada
- **THEN** la taquilla vuelve a estar libre, siempre que no tenga otros hechos que lo impidan, y se registra el evento

### Requirement: Marcar y resolver una avería
El sistema SHALL permitir marcar como averiada una taquilla que no esté de baja y resolver la avería después, registrando ambos eventos.

#### Scenario: Avería en una taquilla libre
- **WHEN** el usuario marca como averiada una taquilla libre
- **THEN** la taquilla queda averiada y deja de ser asignable

#### Scenario: Avería en una taquilla reservada
- **WHEN** el usuario marca como averiada una taquilla reservada
- **THEN** la taquilla queda averiada y conserva su reserva

#### Scenario: Avería en una taquilla de baja
- **WHEN** el usuario intenta marcar como averiada una taquilla de baja
- **THEN** el sistema lo rechaza con un error de taquilla de baja

#### Scenario: Resolver la avería
- **WHEN** el usuario resuelve la avería de una taquilla averiada
- **THEN** la taquilla deja de estar averiada y su estado visible se recalcula

### Requirement: En mantenimiento como segundo tipo de fuera de servicio
El sistema SHALL permitir marcar una taquilla que no esté de baja como en mantenimiento, con las mismas reglas que la avería, y SHALL tratar averiada y en mantenimiento como tipos excluyentes de un mismo estado de fuera de servicio.

#### Scenario: Marcar en mantenimiento
- **WHEN** el usuario marca como en mantenimiento una taquilla libre
- **THEN** la taquilla queda en mantenimiento, deja de ser asignable y se registra el evento

#### Scenario: Cambiar de tipo
- **WHEN** el usuario cambia una taquilla averiada a en mantenimiento
- **THEN** la taquilla pasa a estar en mantenimiento sin pedir decisión adicional y se registra el cambio de tipo

#### Scenario: Mismas reglas que la avería
- **WHEN** una taquilla está en mantenimiento
- **THEN** no es asignable, conserva su reserva o asignación y solo puede darse de baja si no tiene asignación ni reserva

#### Scenario: Volver a servicio
- **WHEN** el usuario da por terminado el mantenimiento o resuelve la avería
- **THEN** la taquilla deja de estar fuera de servicio y su estado visible se recalcula

### Requirement: Decisión obligatoria al poner fuera de servicio una taquilla ocupada
El sistema SHALL exigir una decisión explícita antes de marcar como averiada o en mantenimiento una taquilla que tiene una asignación, y SHALL no cambiar nada mientras no se decida.

#### Scenario: Avería sin decisión
- **WHEN** el usuario marca como averiada o en mantenimiento una taquilla ocupada sin indicar decisión
- **THEN** el sistema no modifica la taquilla y responde que se requiere una decisión, indicando las opciones de reasignar, mantener o liberar

#### Scenario: Decisión de mantener
- **WHEN** el usuario marca como averiada una taquilla ocupada decidiendo mantener al alumno
- **THEN** la taquilla queda averiada conservando su asignación y se registra el evento con la decisión tomada

### Requirement: Cambio de número y de zona
El sistema SHALL permitir cambiar el número o la zona de una taquilla que no esté de baja, respetando las reglas de validez, y registrar el cambio.

#### Scenario: Cambio de zona
- **WHEN** el usuario mueve una taquilla a otra zona activa
- **THEN** la taquilla pertenece a la nueva zona y el evento registra la zona anterior y la nueva

#### Scenario: Cambio de número en conflicto
- **WHEN** el usuario cambia el número de una taquilla a uno que ya usa otra taquilla activa
- **THEN** el sistema lo rechaza con un error de número en uso

#### Scenario: Cambio sobre una taquilla de baja
- **WHEN** el usuario intenta cambiar el número o la zona de una taquilla de baja
- **THEN** el sistema lo rechaza con un error de taquilla de baja

### Requirement: Baja definitiva
El sistema SHALL permitir dar de baja una taquilla que no tenga asignación ni reserva, conservando su historial, y SHALL tratar la baja como irreversible.

#### Scenario: Baja correcta
- **WHEN** el usuario da de baja una taquilla libre
- **THEN** la taquilla queda de baja, deja de contar como activa, no es asignable y se registra el evento

#### Scenario: Baja con asignación
- **WHEN** el usuario intenta dar de baja una taquilla con asignación
- **THEN** el sistema lo rechaza con un error que indica que debe liberarse antes

#### Scenario: Baja con reserva
- **WHEN** el usuario intenta dar de baja una taquilla reservada
- **THEN** el sistema lo rechaza con un error que indica que debe quitarse antes la reserva

#### Scenario: Baja de una taquilla averiada
- **WHEN** el usuario da de baja una taquilla averiada sin asignación ni reserva
- **THEN** la taquilla queda de baja

#### Scenario: Reactivar una baja
- **WHEN** el usuario intenta reactivar una taquilla de baja
- **THEN** el sistema no ofrece esa operación y solo permite dar de alta una taquilla nueva

### Requirement: Confirmación de baja y de alta por rangos
El sistema SHALL pedir confirmación explícita antes de dar de baja una taquilla, indicando que es irreversible, y antes de confirmar un alta por rangos, indicando cuántas taquillas se crearán.

#### Scenario: Confirmar la baja
- **WHEN** el usuario inicia la baja de una taquilla
- **THEN** el sistema indica que la baja no se puede deshacer y no actúa hasta que el usuario confirme

#### Scenario: Confirmar un rango
- **WHEN** el usuario confirma un alta por rangos de 40 taquillas
- **THEN** el sistema pide confirmación indicando que se crearán 40 taquillas

### Requirement: Estados vacíos del inventario
El sistema SHALL guiar al usuario cuando no hay taquillas o zonas, indicando cómo crearlas.

#### Scenario: Sin zonas
- **WHEN** el usuario abre el inventario y no existe ninguna zona
- **THEN** el sistema indica que hay que crear una zona primero y ofrece hacerlo

#### Scenario: Sin taquillas
- **WHEN** existen zonas pero ninguna taquilla activa
- **THEN** el sistema ofrece el alta por rangos y el alta individual

#### Scenario: Filtros sin resultados
- **WHEN** los filtros no devuelven ninguna taquilla
- **THEN** el sistema lo indica y ofrece limpiar los filtros

### Requirement: Historial de eventos de la taquilla
El sistema SHALL registrar en un historial de solo añadir cada alta, cambio de número, cambio de zona, reserva, fin de reserva, avería, resolución de avería y baja, con su instante y los valores anterior y nuevo, y SHALL conservarlo aunque la taquilla esté de baja.

#### Scenario: Consulta del historial
- **WHEN** el usuario consulta el historial de una taquilla
- **THEN** ve sus eventos ordenados del más reciente al más antiguo, con el texto en el idioma activo

#### Scenario: Historial inmutable
- **WHEN** se intenta modificar o borrar un evento ya registrado
- **THEN** el sistema no ofrece esa operación

#### Scenario: Historial de una baja con número reutilizado
- **WHEN** existen una taquilla de baja y otra activa con el mismo número
- **THEN** el historial de cada una muestra solo sus propios eventos

### Requirement: Consulta y filtros
El sistema SHALL listar las taquillas con filtros por zona, estado y número, ordenadas por número, y SHALL mostrar contadores por estado.

#### Scenario: Listado por defecto
- **WHEN** el usuario abre el listado sin filtros
- **THEN** ve las taquillas activas ordenadas numéricamente y no las de baja

#### Scenario: Incluir bajas
- **WHEN** el usuario pide incluir las taquillas de baja
- **THEN** el listado las muestra diferenciadas, con las activas antes que las de baja cuando comparten número

#### Scenario: Filtro combinado
- **WHEN** el usuario filtra por la zona "Planta 1" y el estado averiada
- **THEN** ve solo las taquillas averiadas de esa zona

#### Scenario: Búsqueda por número
- **WHEN** el usuario busca el número 15
- **THEN** ve la taquilla activa 15 y, si pidió incluir bajas, también las bajas con ese número

#### Scenario: Contadores
- **WHEN** el usuario consulta los contadores
- **THEN** ve el total de taquillas activas y cuántas hay libres, ocupadas, averiadas, en mantenimiento y reservadas, en total y por zona

#### Scenario: Sin resultados
- **WHEN** ningún registro cumple los filtros
- **THEN** el sistema devuelve una lista vacía sin error
