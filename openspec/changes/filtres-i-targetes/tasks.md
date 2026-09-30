## 1. Modelo y reglas de los filtros

- [x] 1.1 Dominio `HomeCard` con título (obligatorio, ≤ 60), destino, criterios, posición y clave de serie, y errores de dominio (D1, D5)
- [x] 1.2 Criterios válidos por destino (Taquillas: estado y zona; Alumnos: taquilla, pago, nivel, grupo, incluir bajas) y su serialización estable (D1)
- [x] 1.3 `LockerCardFilter` y `StudentCardFilter` puros con `Matches(fila)`, y hacer que los modelos de Taquillas y Alumnos los usen al aplicar una petición (D2)
- [x] 1.4 Criterio inverso: `CurrentCardCriteria` en los modelos de Taquillas y Alumnos (D7)
- [x] 1.5 Pruebas: cada criterio y su combinación, la misma fila da el mismo resultado en el recuento y en la lista, criterios inválidos y límites

## 2. Persistencia

- [x] 2.1 Repositorio de tarjetas y de la marca de serie en Application y su implementación EF (`HomeCards`, `HomeCardsState`) (D4)
- [x] 2.2 Migración que solo crea las tablas y prueba de migración desde el esquema anterior con datos
- [x] 2.3 Pruebas de persistencia: guardar, leer en orden, mover en una transacción, borrar, y copia y restauración con las tarjetas (con las pruebas de copia existentes)

## 3. Casos de uso

- [ ] 3.1 `EnsureDefaultCardsHandler`: crea las siete de serie una sola vez, idempotente, y Restaurar sin duplicar (D4)
- [ ] 3.2 Crear, editar, mover y borrar tarjetas con sus errores, el máximo de 24 y la protección de la doble ejecución (D5, D9)
- [ ] 3.3 `GetHomeCardsHandler`: recuentos con dos lecturas, sin curso activo para Alumnos, y estado por tarjeta (con recuento, sin recuento, obsoleta) (D3)
- [ ] 3.4 `ResolveHomeCardHandler`: criterios válidos e ignorados contra zonas y catálogo (D6)
- [ ] 3.5 `GetCardOptionsHandler`: zonas, niveles y grupos para el formulario (D8)
- [ ] 3.6 Pruebas por escenario: serie, restaurar, borrar una de serie, obsoletas, muchas tarjetas con dos lecturas, sin datos personales y recuento coherente con la pantalla por cada tarjeta de serie

## 4. Inicio como panel de tarjetas

- [ ] 4.1 Reescribir `StartHomeModel` y `StartHomeScreen` como panel: curso activo, tarjetas con recuento y carga, acciones Nova targeta y Restaurar (D10)
- [ ] 4.2 Abrir una tarjeta con el enrutador, con `Level`, `Group` e `IncludeRetired` además de los criterios actuales, y avisar de los criterios ignorados (D6, D10)
- [ ] 4.3 Estados: sin curso activo, centro sin configurar, sin tarjetas y tarjeta obsoleta con texto
- [ ] 4.4 Editar, mover antes y después (teclado) y borrar con confirmación, en cada tarjeta
- [ ] 4.5 Quitar `GetHomeSummaryHandler` y los enlaces fijos que sustituyen las tarjetas de serie
- [ ] 4.6 Pruebas de modelo y de pantalla: orden, abrir, editar, mover (primera tarjeta deshabilitada), borrar, teclado y ausencia de importes y nombres

## 5. Crear tarjetas

- [ ] 5.1 Formulario de tarjeta (título, resumen de criterios, aviso de no poner nombres) con el `FormViewModel` existente y errores en el campo (D7)
- [ ] 5.2 Acción `Desa com a targeta` en Taquillas (mapa y lista) y Alumnos, disponible solo con filtros y con su motivo (D7)
- [ ] 5.3 Nova targeta desde Inicio con listas desplegables y vista previa del recuento (D8)
- [ ] 5.4 Notificaciones, doble ejecución y límite de 24
- [ ] 5.5 Pruebas: guardar desde Alumnos, desde el mapa y desde la lista con los mismos criterios, sin filtros, título vacío o largo, doble clic, vista previa y límite

## 6. Datos de demostración, documentación y cierre

- [ ] 6.1 La herramienta de datos de ejemplo crea las tarjetas de serie (`--demo` y `--new-year`)
- [ ] 6.2 Actualizar la spec principal `pantalla-principal` (Purpose) y `docs/componentes-ui.md`, `docs/riesgos.md` (títulos de serie y cambio de idioma, nombres en títulos), `docs/roadmap.md` y el glosario si aparece algún término
- [ ] 6.3 Revisión de pruebas de arquitectura, claves i18n en catalán y `openspec validate --all --strict`; archivar el cambio
