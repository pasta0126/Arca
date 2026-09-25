# Datos de ejemplo

Punto 10 de `docs/preparacion-desarrollo.md`. Define **qué datos ficticios** necesita el proyecto y para qué sirve cada situación. La herramienta que los genera es código y se implementa después; aquí solo se fija el contenido.

## Para qué sirven

1. **Demostración** del hito 1 con los conserjes (`docs/hito-1.md`), sin usar nunca datos reales de menores.
2. **Pruebas de integración y de volumen**: que las consultas, el mapa y la conciliación respondan con fluidez con un centro completo.
3. **Cubrir los casos raros** que las specs exigen: homónimos, deuda de cursos anteriores, taquillas averiadas con alumno, bajas con dipòsit, números de taquilla reutilizados...
4. **Ficheros ODS** para probar la importación de alumnos (hito 2). El de alumnos es un ODS con el formato de secretaría; no se usa ningún fichero real.

## Principios

- **Contraseña de prueba conocida.** Las bases generadas usan una contraseña de desarrollo pública que **cumple la política de contraseñas** (por ejemplo `gat ratllat sota pluja`; `demo-demo-demo` la rechaza la política por repetición, decidido 2026-09-25) y su clave de recuperación se escribe junto al fichero; nunca se reutiliza en producción.

- **Totalmente ficticios.** Ningún nombre ni correo procede de personas reales. Los correos usan el dominio `test.cat` (por ejemplo `aina.bosch@test.cat`) y son únicos, porque el correo identifica al alumno.
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
| **Homónimos** (mismo nombre y apellidos, correos distintos) | 8 parejas, algunas en el mismo grupo | Búsqueda; el correo los distingue y no generan casos dudosos |
| Nombres con `ç`, `l·l`, `ny`, apóstrofos, guiones y apellidos compuestos | 60 | Orden y búsqueda catalanes, exportación UTF-8 |
| Todos con correo único | todos | Pruebas de que el correo no sale en listados ni informes |
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

## Ficheros de prueba

Sirven para la importación de alumnos (ODS) y las exportaciones del hito 2. Las taquillas no se importan.

### Alumnos (`alumnes-i-assignacions`, importación ODS)

Formato: una hoja por grupo, con el nombre `<nivel> <grupo>` (`1r ESO A`, `2n BATX B`); fila 1 con `Nom complet` y `Correu`; nombre con el formato `Apellidos, Nombre`; correo único.

| Fichero | Contenido | Escenarios que cubre |
|---------|-----------|----------------------|
| `datos-de-ejemplo-anonimizado.ods` (ya en `docs/`) | 16 hojas (1r ESO a 2n BATX, grupos A a C), 358 alumnos, correos `@test.cat` | Importación normal y prueba de volumen del formato |
| `alumnes-nous-i-baixes.ods` | Alumnos nuevos, ausentes y uno de baja que reaparece | Altas, bajas y reactivación |
| `alumnes-canvis.ods` | Mismo correo con otro nombre, otro nivel u otro grupo | Actualización de nombre y de matrícula |
| `alumnes-parcial-60.ods` | Solo el 40 % del centro | Salvaguarda de bajas masivas |
| `alumnes-correu-repetit.ods` | Mismo correo en dos filas de una hoja y en dos hojas | Filas erróneas por correo repetido |
| `alumnes-errors.ods` | Sin correo, correo mal formado, nombre sin coma, valores largos | Validación por fila |
| `alumnes-fulls-rars.ods` | Hoja vacía, hoja de una palabra, filas vacías | Hojas ignoradas y rechazo de nombre de hoja |
| `alumnes-sense-capcalera.ods` | Falta la columna `Correu` | Rechazo por cabecera |
| `alumnes-no-es-ods.ods` | Fichero que no es un ODS válido (texto o zip dañado) | Rechazo por formato |
| `alumnes-repeticions.ods` | Hoja con una repetición de celdas enorme | Tope de repeticiones |
| `alumnes-buit.ods` y `alumnes-5001.ods` | Sin filas y con 5001 filas | Límites del fichero |

## Herramienta (diseño, sin implementar)

Aplicación de consola en `tools/Arca.SampleData`:

```
arca-sampledata generate --profile demo --seed 42 --db ./demo.arca
arca-sampledata ods --out ./fixtures
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
- Guardar en el repositorio la base `tiny` cifrada y los ficheros pequeños que las pruebas necesiten.

## Preguntas para los conserjes

Para que los perfiles se parezcan a un centro real (E10 y E12 en `docs/riesgos.md`):

1. ¿Cuántas taquillas hay y cuántos alumnos en total? ¿Qué proporción de alumnos tiene taquilla?
2. ¿Cómo están numeradas y agrupadas las taquillas (pasillos, plantas)?
3. ¿Qué niveles y grupos hay realmente y cómo se llaman?
4. ¿Qué importes se cobran hoy y con qué frecuencia hay deuda de cursos anteriores?
5. ¿Cuántas taquillas están averiadas o fuera de servicio a la vez, de media?
6. ¿Hay nombres o apellidos de origen no catalán muy frecuentes que deban aparecer?

### Zonas y taquillas (a preguntar en conserjería)

Las zonas y las taquillas se crean **una sola vez** con los datos reales del centro y después se modifican a mano; por eso hace falta reunir la información antes de la carga inicial (E12 en `docs/riesgos.md`).

**Zonas**
1. ¿Cuántas zonas hay y cómo se llaman (pasillos, plantas, edificios)?
2. ¿Dónde está cada una y en qué orden conviene mostrarlas? ¿Existe un plano o croquis?
3. ¿Alguna zona está reservada a un nivel o grupo concreto?

**Taquillas**
4. ¿Cuántas taquillas tiene cada zona y cuál es el rango de números? ¿La numeración es continua en todo el centro o empieza de nuevo en cada zona?
5. ¿Hay huecos, números saltados o números repetidos en distintas zonas?
6. ¿Hay tipos o tamaños distintos (superior e inferior, grandes y pequeñas)? ¿Interesa distinguirlos?
7. ¿Cuáles están hoy averiadas, fuera de servicio o reservadas, y para quién o por qué?
8. ¿Tienen llave o candado propio, y cuántas copias? ¿Se identifican por el número de la taquilla?

**Situación actual**
9. ¿Quién tiene ya taquilla este curso, y se conserva la asignación al pasar de curso?
10. ¿Qué pagos y fianzas hay ya cobrados o pendientes que haya que cargar al empezar?
11. ¿En qué formato lo tienen hoy (papel, hoja de cálculo)? Si existe una hoja, pedirla; se cargará una vez y, si contiene datos personales, no entra en el repositorio (solo una versión anonimizada, como con `datos-de-ejemplo-anonimizado.ods`).
12. ¿Quién modifica las taquillas durante el año, y con qué frecuencia se añaden o dan de baja?
