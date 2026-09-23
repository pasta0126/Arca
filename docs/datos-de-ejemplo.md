# Datos de ejemplo

Punto 10 de `docs/preparacion-desarrollo.md`. Define **qué datos ficticios** necesita el proyecto y para qué sirve cada situación. La herramienta que los genera es código y se implementa después; aquí solo se fija el contenido.

## Para qué sirven

1. **Demostración** del hito 1 con los conserjes (`docs/hito-1.md`), sin usar nunca datos reales de menores.
2. **Pruebas de integración y de volumen**: que las consultas, el mapa y la conciliación respondan con fluidez con un centro completo.
3. **Cubrir los casos raros** que las specs exigen: homónimos, deuda de cursos anteriores, taquillas averiadas con alumno, bajas con dipòsit, números de taquilla reutilizados...
4. **Ficheros CSV** para probar las importaciones (hito 2) sin esperar al fichero real de secretaría.

## Principios

- **Totalmente ficticios.** Ningún nombre, correo o identificador procede de personas reales. Los correos usan el dominio reservado `exemple.invalid` y los identificadores tienen la forma `TEST-000123`.
- **Reproducibles.** La misma **semilla** produce siempre los mismos datos. Así una prueba que falla se puede repetir y la demostración es igual cada vez.
- **Generados con la lógica real.** La herramienta crea los datos llamando a los casos de uso de `Application` (asignar, cobrar, dar de baja...), no con SQL directo, para respetar las reglas, los índices y los historiales.
- **Fuera del producto.** Vive en `tools/`, no se incluye en el instalador ni en el paquete portable y ninguna capa del producto la referencia.
- **Coste cero.** Sin biblioteca externa de datos falsos: se usan listas propias de nombres y apellidos (regla de `docs/stack.md`).
- **Solo lo pequeño se versiona.** Se guardan en el repositorio únicamente los ficheros diminutos que las pruebas necesitan; el resto se genera cuando hace falta.

## Perfiles

| Perfil | Uso | Taquillas | Alumnos | Asignaciones vigentes | Notas |
|--------|-----|-----------|---------|-----------------------|-------|
| `tiny` | Pruebas unitarias y de integración rápidas, y la base cifrada de ejemplo que se versiona (`arquitectura-base`, 3.5) | 20 en 3 zonas | 25 | 12 | Contiene un caso de cada situación relevante |
| `demo` | **Demostración del hito 1** y pruebas de aceptación | 600 en 6 zonas | 900 | 520 | Centro de tamaño mediano, con todas las situaciones de la tabla siguiente |
| `volume` | Pruebas de rendimiento | 1000 en 10 zonas | 2000 | 1400 | Tope de los volúmenes de las specs |
| `empty-year` (más adelante) | Arranque de un curso nuevo | Como `demo` | Como `demo` | 0 | Para probar el curso en cierre y la importación de septiembre |

**Por qué estas proporciones.** No todos los alumnos tienen taquilla: el número de taquillas no tiene por qué igualar al de alumnos. En `demo`, unas 520 de las 600 taquillas están ocupadas y unos 380 alumnos no tienen taquilla, lo que da material para el panel de "alumnos sin taquilla" y el arrastre.

> **Corrección**: el guion del hito 1 hablaba de "300 taquillas y 2000 alumnos". Esa combinación no es coherente con el perfil real de un centro, así que el hito usa el perfil `demo`. Ver también la pregunta de volumen real en la sección final.

## Contenido del perfil `demo`

### Curso, niveles y grupos

| Elemento | Contenido |
|----------|-----------|
| Cursos | **2026-2027 activo** (1 sept. 2026 a 30 jun. 2027) y **2025-2026** como curso anterior, ya finalizado |
| Niveles | 1r, 2n, 3r y 4t d'ESO; 1r y 2n de Batxillerat |
| Grupos | A, B, C y D en cada curso de ESO; A y B en Batxillerat |
| Importes 2025-2026 | Cuota 45,00 · Dipòsit 20,00 · Reposició de clau 10,00 |
| Importes 2026-2027 | Cuota 50,00 · Dipòsit 20,00 · Reposició de clau 10,00 |

### Zonas y taquillas

| Zona | Taquillas | Numeración |
|------|-----------|------------|
| Planta baixa | 120 | 1 a 120 |
| Planta 1 | 140 | 121 a 260 |
| Planta 2 | 140 | 261 a 400 |
| Gimnàs | 80 | 401 a 480 |
| Pati | 60 | 481 a 540 |
| Annex | 60 | 541 a 600 |

Además, una zona **desactivada** ("Antic magatzem") sin taquillas activas, para probar los filtros.

| Situación de taquilla | Cantidad | Para qué sirve |
|-----------------------|----------|----------------|
| Ocupada | 520 | Mapa, búsqueda, liberar y cambiar |
| Libre | 40 | Asignar, sugerencia de la de menor número |
| Reservada sin alumno (con nota) | 5 | Reserva y error "quitar la reserva antes" |
| Reservada para un alumno | 3 | Consumo de la reserva al asignar |
| Averiada (2 de ellas con el alumno mantenido) | 15 | Estado visible averiada con asignación |
| En mantenimiento | 10 | Segundo tipo de fuera de servicio |
| De baja, con número reutilizado por una activa | 4 | Números repetidos entre bajas y activas |
| De baja (números no reutilizados) | 8 | Filtro "incluir bajas" |

(Las cifras suman más de 600 porque algunas taquillas averiadas o en mantenimiento son además ocupadas o reservadas; el generador las reparte respetando la precedencia de estados.)

### Alumnos

| Situación | Cantidad | Para qué sirve |
|-----------|----------|----------------|
| Activos con taquilla | 520 | Caso normal |
| Activos sin taquilla | 340 | Panel de alumnos sin taquilla, asignar y arrastrar |
| De baja | 40 | Bajas con taquilla liberada, dipòsit por devolver |
| **Homónimos en grupos distintos** | 6 parejas | Búsqueda y conciliación con desempate por nivel y grupo |
| **Homónimos idénticos en el mismo grupo** | 2 parejas | Caso dudoso de la importación |
| Nombres con `ç`, `l·l`, `ny`, apóstrofos, guiones y apellidos compuestos | 60 | Orden y búsqueda catalanes, exportación UTF-8 |
| Con correo e identificador | 700 | Pruebas de que no salen en listados ni informes |
| Sin correo ni identificador | 200 | Reconocimiento solo por nombre |
| Con matrícula en 2025-2026 y en 2026-2027 (nivel distinto) | 600 | Persistencia de la ficha entre cursos |

Los nombres salen de listas de nombres y apellidos catalanes habituales, combinados de forma aleatoria con la semilla; se incluyen apellidos de cualquier origen presentes en un centro real.

### Cobros

| Situación | Cantidad | Para qué sirve |
|-----------|----------|----------------|
| Cuota 2026-2027 **pagada** | 380 | Alumnos al corriente |
| Cuota 2026-2027 **pendiente** | 110 | Marca de deuda en el mapa, pendents de pagament |
| Cuota **exenta** con motivo ("Beca") | 15 | Estado al corriente por exención |
| Cuota **condonada** con motivo | 5 | Distinguir de exenta |
| Cargo **anulado** | 3 | Estado final |
| Dipòsit **pagado** | 470 | Dipòsit único por estancia |
| Dipòsit **exento** con motivo ("Beca de menjador") | 12 | Sin devolución |
| Dipòsit **pendiente** | 30 | |
| Dipòsit **por devolver** (alumnos de baja) | 15 | Lista de dipòsits, hito 2 |
| Dipòsit **devuelto** | 5 | Corrección de una devolución |
| **Deuda de 2025-2026 sin pagar** (asignados de nuevo este curso) | 25 | Aviso de deuda anterior al asignar |
| Cargos de un curso anterior ya saldados | 400 | Historial |
| Alumno **sin cargos** | 100 | "Al corriente por no tener cargos" |

### Llaves e incidencias (para el hito 2; el generador los crea desde el primer día)

| Situación | Cantidad | Para qué sirve |
|-----------|----------|----------------|
| Llave entregada en asignación vigente | 500 | Caso normal |
| Llave pendiente de entrega | 20 | |
| Llave **perdida** con copia entregada y reposición cobrada | 8 | Pérdida con copia |
| Llave **entregada en asignación cerrada** (sin devolver) | 30 | Lista de llaves pendientes, taquilla sin llave disponible |
| Llave **repuesta** | 4 | Regularización |
| Incidencias **abiertas** de motivos variados | 25 | Lista de fuera de servicio |
| Incidencias **reparadas** con historial largo | 60 | Historial por taquilla |

### Historial

Cada dato generado lleva su **historial estructurado** (altas, asignaciones, cambios, pagos) con instantes repartidos a lo largo del curso, para que las consultas de historial tengan contenido realista.

## Ficheros CSV de prueba

Sirven para las importaciones y las exportaciones del hito 2 y quedan definidos ahora para no esperar al fichero real de secretaría (E1 en `docs/riesgos.md`).

### Alumnos (`alumnes-i-assignacions`, importación)

| Fichero | Contenido | Escenarios que cubre |
|---------|-----------|----------------------|
| `secretaria-basic.csv` | Cabeceras catalanas, `;`, UTF-8 con BOM, todas las columnas | Importación normal |
| `secretaria-cabeceres-castella.csv` | Cabeceras en castellano, separador coma | Detección de separador, asistente de columnas |
| `secretaria-cabeceres-angles.csv` | Cabeceras en inglés | Sinónimos en tres idiomas |
| `secretaria-sense-grup.csv` | Sin columna de grupo | Datos opcionales |
| `secretaria-amb-id-i-correu.csv` | Con identificador y correo | Reconocimiento por clave |
| `secretaria-nom-i-cognoms-junts.csv` | Nombre y apellidos en una sola columna | Correspondencia extensible |
| `secretaria-nous-i-baixes.csv` | Nuevos alumnos y ausentes | Altas, bajas y reactivación de un repetidor |
| `secretaria-homonims.csv` | Homónimos con y sin desempate | Casos dudosos |
| `secretaria-parcial-60.csv` | Solo el 40 % del centro | Salvaguarda de bajas masivas |
| `secretaria-duplicats.csv` | Alumno repetido y filas idénticas | Errores de conciliación |
| `secretaria-errors.csv` | Sin nombre, sin nivel, valores largos | Validación por fila |
| `secretaria-latin1.csv` | Codificación no UTF-8 | Rechazo por codificación |
| `secretaria-buit.csv` y `secretaria-5001.csv` | Sin filas y con 5001 filas | Límites del fichero |

### Taquillas (`taquilles-i-zones`, importación)

| Fichero | Contenido | Escenarios que cubre |
|---------|-----------|----------------------|
| `taquilles-basic.csv` | 300 filas correctas | Importación normal |
| `taquilles-errors.csv` | Números repetidos, cero, negativos, mayores de 99999, zona vacía, nota larga | Validación por fila |
| `taquilles-zones-noves.csv` | Zonas inexistentes con distinta grafía | Crear las zonas que faltan |
| `taquilles-repetit.csv` | Las mismas filas que ya existen | Importar dos veces |
| `taquilles-5001.csv` | 5001 filas | Límite del fichero |

## Herramienta (diseño, sin implementar)

Aplicación de consola en `tools/Arca.SampleData`:

```
arca-sampledata generate --profile demo --seed 42 --db ./demo.arca
arca-sampledata csv --out ./fixtures
arca-sampledata list-profiles
```

- Referencia `Application` y `Infrastructure` para crear los datos con los casos de uso reales.
- Es **determinista**: la semilla fija las listas, el reparto y los instantes.
- Verifica al terminar que los recuentos coinciden con la tabla del perfil.
- Reutiliza `Arca.Testing` para los constructores de datos de las pruebas unitarias; el generador no los sustituye.
- Sin conexión a la red y sin dependencias de pago.

## Qué queda para implementar

- La herramienta y sus perfiles.
- Las listas de nombres y apellidos.
- El script que compara los recuentos generados con esta tabla.
- Guardar en el repositorio la base `tiny` cifrada y los CSV pequeños que las pruebas necesiten.

## Preguntas para los conserjes

Para que los perfiles se parezcan a un centro real (E10 en `docs/riesgos.md`):

1. ¿Cuántas taquillas hay y cuántos alumnos en total? ¿Qué proporción de alumnos tiene taquilla?
2. ¿Cómo están numeradas y agrupadas las taquillas (pasillos, plantas)?
3. ¿Qué niveles y grupos hay realmente y cómo se llaman?
4. ¿Qué importes se cobran hoy y con qué frecuencia hay deuda de cursos anteriores?
5. ¿Cuántas taquillas están averiadas o fuera de servicio a la vez, de media?
6. ¿Hay nombres o apellidos de origen no catalán muy frecuentes que deban aparecer?
