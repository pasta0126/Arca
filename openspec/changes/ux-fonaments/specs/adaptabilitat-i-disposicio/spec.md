## Purpose

Garantizar que la interfaz se ve y se usa bien con distintos tamaños de ventana y densidades de pantalla, y que ahorra espacio con secciones colapsables cuyo estado se recuerda.

## ADDED Requirements

### Requirement: Tamaño mínimo de ventana
El sistema SHALL definir un tamaño mínimo de ventana y SHALL impedir reducirla por debajo de él, de modo que ningún control quede inaccesible.

#### Scenario: Reducir la ventana
- **WHEN** el usuario intenta reducir la ventana por debajo del mínimo
- **THEN** la ventana se detiene en el mínimo

### Requirement: Escalado por DPI
El sistema SHALL escalar la interfaz según la densidad de la pantalla y el factor de escala del sistema operativo, sin recortes ni solapamientos.

#### Scenario: Pantalla de alta densidad
- **WHEN** la aplicación se ejecuta con un factor de escala del 150 %
- **THEN** textos y controles se ven completos y proporcionados

#### Scenario: Cambio de pantalla
- **WHEN** el usuario arrastra la ventana a una pantalla de otra densidad
- **THEN** la interfaz se reescala sin reiniciar

### Requirement: Disposición adaptable
El sistema SHALL adaptar la disposición de las pantallas al ancho disponible, apilando o reduciendo paneles secundarios antes de recortar el contenido principal.

#### Scenario: Ancho reducido
- **WHEN** el ancho disponible no basta para el panel de detalle junto a la lista
- **THEN** el detalle se apila bajo la lista o se abre a demanda, sin scroll horizontal del contenido principal

#### Scenario: Ventana ampliada
- **WHEN** el usuario maximiza la ventana
- **THEN** el contenido aprovecha el espacio adicional sin estirar los controles de forma desproporcionada

### Requirement: Secciones colapsables
El sistema SHALL ofrecer secciones colapsables con título y resumen de su contenido cuando está colapsada, operables con ratón y teclado, y SHALL recordar su estado en el equipo.

#### Scenario: Colapsar
- **WHEN** el usuario colapsa una sección
- **THEN** solo se ve su título y un resumen breve, y el espacio se libera

#### Scenario: Recordar el estado
- **WHEN** el usuario reabre la aplicación
- **THEN** las secciones están como las dejó

#### Scenario: Sección con error
- **WHEN** una sección colapsada contiene un error de validación
- **THEN** se expande y muestra el error

### Requirement: Zonas compactables
El sistema SHALL permitir compactar las listas de zonas y taquillas para mostrar más elementos en el espacio disponible, y SHALL recordar la elección.

#### Scenario: Modo compacto
- **WHEN** el usuario activa la vista compacta
- **THEN** la lista muestra más filas con menor altura y se recuerda al reabrir

### Requirement: Preferencias locales de interfaz
El sistema SHALL guardar el tamaño y la posición de la ventana, los estados de las secciones y la densidad en los ajustes locales, fuera de la base de datos, sin datos personales, y SHALL funcionar con valores por defecto si no se pueden leer.

#### Scenario: Ajustes ilegibles
- **WHEN** el fichero de preferencias está dañado o ausente
- **THEN** la aplicación arranca con los valores por defecto sin mostrar errores

#### Scenario: Ventana fuera de pantalla
- **WHEN** la posición guardada queda fuera de las pantallas disponibles
- **THEN** la ventana se coloca visible en la pantalla principal
