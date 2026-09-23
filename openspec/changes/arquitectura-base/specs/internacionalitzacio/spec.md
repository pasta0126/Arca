## Purpose

Garantizar que todos los textos y formatos de la aplicación salen de recursos por clave, con el catalán como único idioma de la v1 y la posibilidad de añadir otros idiomas después sin modificar el código.

## ADDED Requirements

### Requirement: Textos visibles por clave de recurso
El sistema SHALL obtener todo texto visible para el usuario (etiquetas, mensajes, errores, cabeceras de informes) a partir de claves de recurso, sin textos literales en el código ni en las vistas.

#### Scenario: Texto de interfaz
- **WHEN** una pantalla muestra una etiqueta o un mensaje
- **THEN** el texto proviene de una clave de recurso del idioma activo

#### Scenario: Cabeceras de informes
- **WHEN** se genera un fichero de exportación
- **THEN** los nombres de columna provienen de claves de recurso del idioma activo

### Requirement: Catalán como único idioma de la v1
El sistema SHALL presentar toda la interfaz y todos los informes en catalán en la v1, sin selector de idioma.

#### Scenario: Equipo con sistema operativo en otro idioma
- **WHEN** la aplicación se ejecuta en un equipo cuyo sistema operativo está configurado en castellano o inglés
- **THEN** la interfaz sigue mostrándose íntegramente en catalán

### Requirement: Formatos según la cultura catalana
El sistema SHALL formatear fechas, números e importes según la cultura catalana de España, independientemente de la configuración regional del sistema operativo.

#### Scenario: Importe
- **WHEN** se muestra un importe de mil doscientos treinta y cuatro euros con cincuenta céntimos
- **THEN** se presenta con coma decimal, punto de millares y símbolo del euro según el formato catalán

#### Scenario: Fecha
- **WHEN** se muestra una fecha de calendario
- **THEN** se presenta en el formato de fecha corta catalán con día antes que mes

### Requirement: Añadir un idioma sin cambiar código
El sistema SHALL permitir incorporar un nuevo idioma añadiendo únicamente sus recursos de traducción y registrándolo como disponible.

#### Scenario: Idioma con traducción incompleta
- **WHEN** existe un idioma adicional al que le falta alguna clave
- **THEN** el sistema muestra el texto en catalán para esa clave y no falla

### Requirement: Detección de claves faltantes
El sistema SHALL detectar en el proceso de verificación (pruebas o compilación) las claves de recurso usadas que no existan en el idioma base, y SHALL degradar sin fallar en ejecución.

#### Scenario: Clave inexistente en verificación
- **WHEN** el código referencia una clave que no está definida en el idioma base
- **THEN** la verificación automática falla indicando la clave

#### Scenario: Clave inexistente en ejecución
- **WHEN** en ejecución se solicita una clave inexistente
- **THEN** el sistema muestra la propia clave como texto y sigue funcionando

### Requirement: Errores de negocio con código estable
El sistema SHALL identificar cada error de negocio con un código estable e independiente del idioma, y SHALL resolver su mensaje visible mediante recursos.

#### Scenario: Error mostrado al usuario
- **WHEN** una operación es rechazada por una regla de negocio
- **THEN** el error tiene un código estable y el mensaje mostrado sale de una clave de recurso asociada a ese código

### Requirement: Ordenación y búsqueda según el catalán
El sistema SHALL ordenar y comparar textos según las reglas del catalán y SHALL hacer las búsquedas insensibles a mayúsculas y acentos.

#### Scenario: Búsqueda sin acentos
- **WHEN** el usuario busca "garcia"
- **THEN** los resultados incluyen "García"

#### Scenario: Caracteres propios del catalán
- **WHEN** se guardan y muestran nombres con "ç", "l·l" o "ny"
- **THEN** se conservan exactamente y se ordenan correctamente

### Requirement: Codificación UTF-8 de extremo a extremo
El sistema SHALL conservar sin alteración todos los caracteres Unicode al guardar, mostrar y exportar datos.

#### Scenario: Exportación con caracteres especiales
- **WHEN** se exporta un fichero con nombres que contienen acentos y "l·l"
- **THEN** al abrirlo en Excel de Windows los caracteres se ven correctamente
