## Purpose

Registrar los periodos en que una taquilla está averiada o en mantenimiento, con su motivo y notas, de forma que el conserje pueda marcarlas fuera de servicio y reparadas con un gesto mínimo y cada taquilla conserve su historial.

## ADDED Requirements

### Requirement: Incidencia como periodo fuera de servicio
El sistema SHALL representar cada periodo en que una taquilla está fuera de servicio como una incidencia, con su tipo (averiada o en mantenimiento), su motivo, una nota opcional, su fecha de inicio y, cuando se repara, su fecha y nota de resolución.

#### Scenario: Abrir una incidencia
- **WHEN** el usuario abre una incidencia de tipo averiada con el motivo "Cerradura" en una taquilla operativa
- **THEN** la taquilla queda averiada, la incidencia queda abierta con la fecha de hoy y se registra el evento en el historial de la taquilla

#### Scenario: Fecha de inicio
- **WHEN** el usuario indica una fecha de inicio anterior a hoy
- **THEN** el sistema la acepta

#### Scenario: Fecha de inicio futura
- **WHEN** el usuario indica una fecha de inicio posterior a hoy
- **THEN** el sistema lo rechaza con un error de fecha no válida

#### Scenario: Nota demasiado larga
- **WHEN** la nota supera los 500 caracteres
- **THEN** el sistema lo rechaza con un error de longitud máxima

### Requirement: Una sola incidencia abierta por taquilla
El sistema SHALL permitir como máximo una incidencia abierta por taquilla, y SHALL mantener la coincidencia entre tener una incidencia abierta y estar la taquilla fuera de servicio.

#### Scenario: Segunda incidencia abierta
- **WHEN** el usuario intenta abrir una incidencia en una taquilla que ya tiene una abierta
- **THEN** el sistema lo rechaza con un error que indica que ya está fuera de servicio y sugiere cambiar el tipo o el motivo de la existente

#### Scenario: Taquilla de baja
- **WHEN** el usuario intenta abrir una incidencia en una taquilla de baja
- **THEN** el sistema lo rechaza con un error de taquilla de baja

#### Scenario: Coherencia
- **WHEN** una taquilla tiene una incidencia abierta
- **THEN** su estado visible es averiada o en mantenimiento según el tipo de la incidencia, y cuando no tiene ninguna abierta no está fuera de servicio

### Requirement: Motivo obligatorio de una lista
El sistema SHALL exigir un motivo elegido de una lista para abrir una incidencia.

#### Scenario: Sin motivo
- **WHEN** el usuario intenta abrir una incidencia sin motivo
- **THEN** el sistema lo rechaza con un error de motivo obligatorio

#### Scenario: Motivo desactivado
- **WHEN** el usuario intenta abrir una incidencia con un motivo desactivado
- **THEN** el sistema lo rechaza con un error de motivo no disponible

### Requirement: Catálogo de motivos editable
El sistema SHALL ofrecer una lista de motivos que el usuario puede ampliar, renombrar, desactivar y reactivar, con nombres únicos sin distinguir mayúsculas ni acentos, y SHALL crear una lista inicial de motivos habituales en el idioma activo al crear la base de datos.

#### Scenario: Lista inicial
- **WHEN** se crea la base de datos por primera vez
- **THEN** existe una lista de motivos habituales, entre ellos cerradura, puerta, bisagra, limpieza, vandalismo y otro

#### Scenario: Motivo nuevo
- **WHEN** el usuario añade el motivo "Ventilación"
- **THEN** el motivo queda disponible para nuevas incidencias

#### Scenario: Nombre duplicado
- **WHEN** existe el motivo "Cerradura" y el usuario añade "cerradura"
- **THEN** el sistema lo rechaza con un error de nombre duplicado

#### Scenario: Nombre vacío o demasiado largo
- **WHEN** el usuario indica un nombre vacío o de más de 60 caracteres
- **THEN** el sistema lo rechaza con el error de negocio correspondiente

#### Scenario: Renombrar
- **WHEN** el usuario renombra un motivo usado en incidencias anteriores
- **THEN** las incidencias anteriores muestran el nuevo nombre

#### Scenario: Desactivar un motivo en uso
- **WHEN** el usuario desactiva un motivo que tienen incidencias
- **THEN** el motivo deja de ofrecerse para nuevas incidencias y las existentes lo conservan

#### Scenario: Eliminar un motivo usado
- **WHEN** el usuario intenta eliminar un motivo que tiene o tuvo incidencias
- **THEN** el sistema lo rechaza y sugiere desactivarlo

#### Scenario: Eliminar un motivo sin uso
- **WHEN** el usuario elimina un motivo que nunca se ha usado
- **THEN** el motivo desaparece de la lista

### Requirement: Decisión al abrir una incidencia en una taquilla ocupada
El sistema SHALL aplicar, al abrir una incidencia en una taquilla con asignación, la decisión obligatoria de mantener al alumno, reasignarlo o liberarlo, y SHALL no abrirla mientras no se decida.

#### Scenario: Sin decisión
- **WHEN** el usuario abre una incidencia en una taquilla ocupada sin indicar decisión
- **THEN** el sistema no abre la incidencia ni modifica nada y pide la decisión

#### Scenario: Mantener al alumno
- **WHEN** el usuario abre la incidencia decidiendo mantener al alumno
- **THEN** la incidencia queda abierta y la taquilla conserva su asignación

#### Scenario: Reasignar o liberar
- **WHEN** el usuario abre la incidencia decidiendo reasignar o liberar
- **THEN** la incidencia queda abierta y se aplica la decisión en la misma operación indivisible

#### Scenario: Decisión que falla
- **WHEN** la decisión no puede aplicarse, como una reasignación a una taquilla no asignable
- **THEN** no se abre la incidencia y no cambia nada

### Requirement: Cambiar el tipo, el motivo o la nota de una incidencia abierta
El sistema SHALL permitir cambiar el tipo, el motivo y la nota de una incidencia abierta, registrando cada cambio en su historial.

#### Scenario: De avería a mantenimiento
- **WHEN** el usuario cambia una incidencia abierta de averiada a en mantenimiento
- **THEN** la taquilla pasa a estar en mantenimiento sin pedir decisión adicional y se registra el cambio

#### Scenario: Corregir el motivo
- **WHEN** el usuario cambia el motivo de una incidencia abierta
- **THEN** el historial conserva el motivo anterior y el nuevo

#### Scenario: Incidencia reparada
- **WHEN** el usuario intenta cambiar una incidencia ya reparada
- **THEN** el sistema lo rechaza con un error de incidencia cerrada

### Requirement: Marcar como reparada
El sistema SHALL permitir marcar como reparada una incidencia abierta, con una fecha que por defecto es hoy, no puede ser futura ni anterior al inicio de la incidencia, y una nota de resolución opcional, devolviendo la taquilla a su estado de servicio.

#### Scenario: Reparación correcta
- **WHEN** el usuario marca como reparada una incidencia abierta
- **THEN** la incidencia queda cerrada con su fecha y nota y la taquilla deja de estar fuera de servicio

#### Scenario: Estado visible tras reparar
- **WHEN** se repara la incidencia de una taquilla que conservaba a su alumno
- **THEN** su estado visible vuelve a ser ocupada sin ninguna otra acción

#### Scenario: Fecha anterior al inicio
- **WHEN** la fecha de reparación es anterior al inicio de la incidencia
- **THEN** el sistema lo rechaza con un error de fecha no válida

#### Scenario: Fecha futura
- **WHEN** la fecha de reparación es posterior a hoy
- **THEN** el sistema lo rechaza con un error de fecha no válida

#### Scenario: Incidencia ya reparada
- **WHEN** el usuario intenta reparar una incidencia ya cerrada
- **THEN** el sistema lo rechaza con un error de estado no válido

#### Scenario: Sin confirmación
- **WHEN** el usuario marca como reparada una incidencia
- **THEN** el sistema actúa sin pedir confirmación, porque la acción es reversible abriendo una nueva incidencia

### Requirement: Sin reapertura
El sistema SHALL no ofrecer reabrir una incidencia reparada; si la taquilla vuelve a fallar, se abre una nueva.

#### Scenario: Nueva avería
- **WHEN** una taquilla reparada vuelve a fallar y el usuario abre otra incidencia
- **THEN** se crea una incidencia nueva y la anterior conserva su resolución

### Requirement: Baja de la taquilla con incidencia abierta
El sistema SHALL cerrar automáticamente la incidencia abierta de una taquilla cuando esta se da de baja, con la fecha de la baja y una nota que lo indique.

#### Scenario: Baja de una taquilla averiada
- **WHEN** el usuario da de baja una taquilla con una incidencia abierta
- **THEN** la incidencia queda cerrada con la fecha de la baja y una nota automática, y se conserva en el historial

### Requirement: Historial de incidencias por taquilla
El sistema SHALL mostrar el historial de incidencias de cada taquilla ordenado de la más reciente a la más antigua, con tipo, motivo, notas, fechas y duración, y SHALL conservarlo aunque la taquilla esté de baja.

#### Scenario: Consulta del historial
- **WHEN** el usuario consulta las incidencias de una taquilla
- **THEN** ve todas, abiertas y cerradas, con la duración en días de cada una

#### Scenario: Taquilla sin incidencias
- **WHEN** la taquilla nunca ha tenido incidencias
- **THEN** el sistema lo indica con un mensaje explicativo

#### Scenario: Número reutilizado
- **WHEN** existen una taquilla de baja y otra activa con el mismo número
- **THEN** cada una muestra solo sus propias incidencias

### Requirement: Lista de taquillas fuera de servicio
El sistema SHALL ofrecer una lista de las taquillas fuera de servicio con su tipo, motivo, fecha de inicio y días fuera de servicio, con filtros por tipo, motivo y zona, y con indicación de las que conservan un alumno.

#### Scenario: Lista por defecto
- **WHEN** el usuario abre la lista
- **THEN** ve las incidencias abiertas ordenadas de la más antigua a la más reciente, con el total

#### Scenario: Filtros
- **WHEN** el usuario filtra por en mantenimiento y por una zona
- **THEN** ve solo las taquillas en mantenimiento de esa zona

#### Scenario: Con alumno
- **WHEN** una taquilla fuera de servicio conserva a su alumno
- **THEN** la lista lo indica

#### Scenario: Ninguna fuera de servicio
- **WHEN** no hay ninguna taquilla fuera de servicio
- **THEN** el sistema lo indica con un mensaje positivo en lugar de una lista vacía sin explicación

### Requirement: Privacidad de las notas
El sistema SHALL tratar las notas de las incidencias como texto libre sin incluirlas en listados generales ni en el registro técnico.

#### Scenario: Listados generales
- **WHEN** el usuario consulta el listado de taquillas o la lista de fuera de servicio
- **THEN** no se muestran las notas y solo aparecen en el detalle de la incidencia

### Requirement: Feedback y guía en las incidencias
El sistema SHALL informar del resultado de cada operación y SHALL guiar al usuario cuando no hay incidencias, motivos o taquillas fuera de servicio.

#### Scenario: Resultado
- **WHEN** el usuario abre o repara una incidencia
- **THEN** el sistema confirma qué taquilla queda fuera de servicio o vuelve a estar operativa

#### Scenario: Sin motivos activos
- **WHEN** no hay ningún motivo activo al abrir una incidencia
- **THEN** el sistema indica que hay que crear o reactivar un motivo y ofrece hacerlo
