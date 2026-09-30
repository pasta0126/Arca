## Purpose
Definir las tarjetas de Inicio: filtros guardados que llevan a una pantalla con el filtro ya puesto y muestran cuántos elementos lo cumplen, con las de serie y la forma de crearlas, editarlas, ordenarlas y borrarlas.

## ADDED Requirements

### Requirement: Una tarjeta es un filtro guardado
El sistema SHALL representar cada tarjeta de Inicio con un título, la pantalla a la que lleva (mapa de Taquillas, lista de Taquillas o Alumnos), un conjunto de criterios y una posición, y SHALL guardar únicamente criterios y el título, nunca datos de alumnos. Los criterios de Taquillas son estado y zona; los de Alumnos son taquilla (con o sin), pago (con pendientes o al corriente), nivel, grupo e incluir bajas.

#### Scenario: Tarjeta de taquillas libres
- **WHEN** existe una tarjeta «Taquillas libres» con el criterio estado libre que lleva al mapa
- **THEN** al abrirla se muestra el mapa de Taquillas con solo las libres de cada zona y el filtro visible como etiqueta

#### Scenario: Criterios combinados
- **WHEN** una tarjeta de Alumnos tiene los criterios sin taquilla y nivel 2.º de ESO
- **THEN** al abrirla se muestra la lista de Alumnos con ambos filtros y nada más

#### Scenario: Sin datos personales
- **WHEN** se guarda una tarjeta
- **THEN** lo guardado es el título, la pantalla, los criterios y la posición, sin nombres, correos ni identificadores de alumnos

### Requirement: Recuento en vivo de la tarjeta
El sistema SHALL mostrar en cada tarjeta cuántos elementos cumplen su filtro, calculados con los mismos criterios que aplicaría la pantalla al abrirla, y nunca importes ni saldos. Los recuentos SHALL leerse al abrir Inicio y de nuevo cuando cambie el estado de la aplicación, con una consulta por tipo de elemento y no una por tarjeta.

#### Scenario: Recuento coherente con la pantalla
- **WHEN** una tarjeta dice 60 alumnos con pendientes
- **THEN** al abrirla la lista muestra 60 alumnos con el mismo filtro

#### Scenario: Tras un cambio
- **WHEN** el usuario cobra el último pendiente de un alumno y vuelve a Inicio
- **THEN** la tarjeta de pendientes muestra un alumno menos

#### Scenario: Muchas tarjetas
- **WHEN** Inicio tiene 20 tarjetas
- **THEN** el recuento de todas se obtiene con una lectura de las taquillas y otra de los alumnos

#### Scenario: Sin curso activo
- **WHEN** no hay curso activo
- **THEN** las tarjetas de Alumnos no muestran recuento y lo explican, y las de Taquillas siguen contando

### Requirement: Tarjetas de serie
El sistema SHALL crear, al estrenar un centro y en las bases de demostración, las tarjetas de serie: taquillas libres, ocupadas, reservadas, avariadas y en mantenimiento (al mapa), alumnos sin taquilla y alumnos con pendientes de pago (a Alumnos). Las de serie SHALL poder editarse y borrarse como cualquier otra y no se vuelven a crear por sí solas una vez creadas; una acción Restaurar las de serie SHALL añadir las que falten sin duplicar las que existen.

#### Scenario: Centro nuevo
- **WHEN** se abre Inicio por primera vez en un centro recién creado
- **THEN** aparecen las siete tarjetas de serie

#### Scenario: Borrar una de serie
- **WHEN** el usuario borra la tarjeta de taquillas reservadas y reinicia la aplicación
- **THEN** la tarjeta no reaparece

#### Scenario: Restaurar
- **WHEN** el usuario elige Restaurar las de serie con seis de las siete presentes
- **THEN** se añade la que falta y las demás no se duplican

#### Scenario: Base de demostración
- **WHEN** se crea una base con los datos de demostración
- **THEN** Inicio ya tiene las tarjetas de serie con sus recuentos

### Requirement: Crear una tarjeta desde una pantalla filtrada
El sistema SHALL ofrecer en Taquillas (mapa y lista) y en Alumnos la acción Desa com a targeta, disponible solo cuando hay algún filtro activo (y deshabilitada con su motivo si no), que abre un formulario con el título propuesto a partir de los filtros, el resumen de los criterios que se guardan, y el aviso de no escribir nombres de alumnos en el título.

#### Scenario: Guardar el filtro actual
- **WHEN** el usuario filtra Alumnos por sin taquilla y 1.º de ESO, elige Desa com a targeta y confirma un título
- **THEN** la tarjeta aparece en Inicio con esos criterios y una notificación lo confirma

#### Scenario: Sin filtros
- **WHEN** la pantalla no tiene ningún filtro activo
- **THEN** la acción aparece deshabilitada y explica que primero hay que filtrar

#### Scenario: Título vacío
- **WHEN** el usuario deja el título vacío o de más de 60 caracteres
- **THEN** el formulario marca el campo y no deja confirmar

#### Scenario: Doble clic
- **WHEN** el usuario hace doble clic en Confirmar
- **THEN** se crea una sola tarjeta

### Requirement: Crear una tarjeta desde Inicio
El sistema SHALL ofrecer en Inicio la acción Nova targeta, que pide el título, la pantalla de destino y los filtros con listas desplegables (estado y zona en Taquillas; taquilla, pago, nivel y grupo en Alumnos), y SHALL mostrar el recuento que tendría antes de guardarla.

#### Scenario: Nueva tarjeta de taquillas averiadas de una zona
- **WHEN** el usuario crea una tarjeta para Taquillas con estado avariada y la zona Planta 1
- **THEN** la tarjeta aparece en Inicio con el recuento de las avariadas de Planta 1

#### Scenario: Vista previa del recuento
- **WHEN** el usuario cambia un filtro en el formulario
- **THEN** el formulario muestra cuántos elementos cumplen el filtro elegido

#### Scenario: Límite de tarjetas
- **WHEN** ya hay 24 tarjetas
- **THEN** Nova targeta se muestra deshabilitada y explica que es el máximo

### Requirement: Editar, ordenar y borrar tarjetas
El sistema SHALL permitir editar el título y los filtros de una tarjeta, cambiar su posición con acciones de mover antes y mover después accesibles con teclado, y borrarla con confirmación que indique su título y que el borrado no afecta a ningún dato.

#### Scenario: Editar
- **WHEN** el usuario cambia el título y el estado de una tarjeta y guarda
- **THEN** la tarjeta muestra el nuevo título y su recuento recalculado

#### Scenario: Mover
- **WHEN** el usuario mueve una tarjeta antes de la anterior
- **THEN** cambia de posición y el orden se mantiene al reiniciar la aplicación

#### Scenario: Primera tarjeta
- **WHEN** la tarjeta es la primera
- **THEN** la acción de mover antes aparece deshabilitada con su motivo

#### Scenario: Borrar
- **WHEN** el usuario borra una tarjeta y confirma
- **THEN** desaparece de Inicio, los alumnos y las taquillas no cambian y una notificación lo confirma

### Requirement: Tarjetas con filtros obsoletos
El sistema SHALL detectar una tarjeta cuyo criterio ya no existe (zona borrada o desactivada sin taquillas, nivel o grupo que ya no figura en el catálogo), marcarla en la propia tarjeta con texto y no solo con color, no contar con ese criterio y, al abrirla, abrir la pantalla sin el criterio obsoleto y avisar de cuál se ha ignorado.

#### Scenario: Zona que ya no existe
- **WHEN** una tarjeta filtra por una zona que se ha eliminado
- **THEN** la tarjeta indica que su filtro de zona ya no existe y ofrece editarla o borrarla

#### Scenario: Abrir una tarjeta obsoleta
- **WHEN** el usuario abre esa tarjeta
- **THEN** se abre la pantalla con el resto de filtros y una notificación dice que se ha ignorado la zona

### Requirement: Persistencia y copia de seguridad
El sistema SHALL guardar las tarjetas en la base del centro, de modo que entren en la copia de seguridad y en la restauración, y SHALL conservarlas al actualizar la aplicación mediante una migración que no toca los datos existentes.

#### Scenario: Copia y restauración
- **WHEN** se hace una copia y se restaura en otro equipo
- **THEN** Inicio muestra las mismas tarjetas en el mismo orden

#### Scenario: Actualizar desde una versión sin tarjetas
- **WHEN** se abre un centro creado antes de esta versión
- **THEN** la migración añade las tablas sin perder datos y se crean las tarjetas de serie una sola vez

### Requirement: Feedback y accesibilidad de las tarjetas
El sistema SHALL confirmar cada operación sobre tarjetas con una notificación, mostrar los errores de forma comprensible, proteger de la doble ejecución, permitir operar todo con teclado y distinguir el estado de una tarjeta (con recuento, sin recuento, obsoleta) con texto además de color.

#### Scenario: Error al guardar
- **WHEN** falla el guardado de una tarjeta
- **THEN** se muestra un error comprensible sin detalles técnicos y el formulario sigue abierto con lo escrito

#### Scenario: Teclado
- **WHEN** el usuario recorre Inicio con Tab
- **THEN** cada tarjeta se abre con Intro y sus acciones de editar, mover y borrar son alcanzables sin ratón
