## 1. Marco común de lista y pantalla

- [x] 1.1 Ampliar el modelo de lista con recuento, comando Reiniciar (limpia búsqueda y filtros) y colección de filtros activos quitables (D1)
- [x] 1.2 Vista de la barra de lista: cuadro de búsqueda, botón Reiniciar (deshabilitado sin nada que reiniciar), etiquetas de filtro y recuento, con claves i18n en catalán
- [x] 1.3 Manejo de `Esc` en el marco con la prioridad del diseño (diálogo, búsqueda con texto, selección y foco) (D2)
- [x] 1.4 `ScrollViewer` en `ScreenView` para pantallas sin lista virtualizada y en los paneles de detalle; corregir Ajustes (D3)
- [x] 1.5 Pruebas: reinicio con búsqueda y dos filtros, reinicio deshabilitado, quitar un filtro, `Esc` con selección, con diálogo abierto y con búsqueda enfocada, Ajustes que desplaza hasta el último bloque, tamaño mínimo de ventana y DPI alto

## 2. Filtro de pendientes de pago

- [x] 2.1 Filtro «Pagaments» (cualquiera, amb pendents, al corrent) en la lista de Alumnos sobre las filas que ya traen la deuda, con etiqueta quitable y reinicio, y estado de pago de las filas sin importes (D5)
- [x] 2.2 Recuento de alumnos con pendientes en el estado global (`StudentsWithPending`) para el indicador de la barra lateral, que pasa de Cobros a Alumnos, y mensaje positivo cuando nadie tiene pendientes
- [x] 2.3 Pruebas: solo con pendientes de cualquier curso, al corriente, combinado con nivel y sin taquilla, alumno de baja, nadie con pendientes y 900 alumnos sin consultas una a una

## 3. Lista y ficha de Alumnos

- [ ] 3.1 Usar la barra de lista común en Alumnos con el filtro de pendientes, el recuento y el estado de pago sin importes en las filas
- [ ] 3.2 Partir `StudentChargesPanel` en lista de pendientes con acciones en la fila y lista de historial de pagos (D4)
- [ ] 3.3 Rehacer `StudentDetailPanel` en una columna: cabecera, acciones, pendientes o «al corriente», y bloques colapsables cerrados con recuento y carga al abrir (D4)
- [ ] 3.4 Recordar los bloques abiertos como preferencia local y mantenerlos al cambiar de alumno
- [ ] 3.5 Estados vacíos y de carga guiados (sin cargos, al corriente, nadie con pendientes) y notificación de cada cobro con cargo e importe
- [ ] 3.6 Pruebas: alumno con pendientes, al corriente, sin cargos, cobrar desde la ficha y refresco de cabecera y lista, doble clic en confirmar, cambio de alumno con bloque abierto y privacidad del correo

## 4. Retirar la sección Cobros

- [ ] 4.1 Quitar la entrada Cobros de la barra, de las definiciones de sección y de la búsqueda global (D8)
- [ ] 4.2 Eliminar `PaymentsSection`, la consulta de morosos y su composición, conservando las operaciones sobre cargos y el aviso de deuda al asignar
- [ ] 4.3 Sustituir las claves i18n obsoletas y comprobar que «Pendents de pagament» sigue en el filtro, la ficha y el indicador
- [ ] 4.4 Pruebas: ninguna ruta abre Cobros, el aviso de deuda al asignar sigue funcionando y la reposición de llave se cobra desde la ficha

## 5. El mapa en la sección Taquillas

- [ ] 5.1 Tercera pestaña Mapa (vista inicial) con el modelo compartido de filtros, búsqueda, selección y detalle con la lista (D6)
- [ ] 5.2 Mover el mapa y el panel de alumnos sin taquilla a la sección, con arrastrar y soltar y su alternativa de menú y teclado, y eliminar el detalle duplicado del mapa
- [ ] 5.3 Espacio entre zonas del mapa con espaciado de tema (D7) y `Esc` que quita la taquilla seleccionada
- [ ] 5.4 La búsqueda global que elige una taquilla abre Taquillas en el mapa con ella resaltada y su detalle
- [ ] 5.5 Pruebas: cambio de Mapa a Lista con filtro y selección, filtro por estado, resaltado de búsqueda, asignar arrastrando, 300 taquillas y actualización de una sola taquilla tras asignar

## 6. Inicio mínimo provisional

- [ ] 6.1 Nueva pantalla de Inicio con curso activo y recuentos como accesos que abren Taquillas o Alumnos con el filtro adecuado precargado
- [ ] 6.2 Estados: sin curso activo con acceso a Curso, y sin datos con guía a la configuración guiada
- [ ] 6.3 Pruebas: cada acceso abre la sección con su filtro visible como etiqueta quitable y sin importes en ningún texto

## 7. Datos de demostración y cierre

- [ ] 7.1 Comprobar con `build/run.sh --new-year` y `--demo` los flujos nuevos (pendientes, reinicio, `Esc`, mapa en Taquillas, Ajustes con scroll)
- [ ] 7.2 Actualizar `docs/riesgos.md` (Cobros retirado, Inicio provisional), `docs/glosario.md` si cambia algún término y `docs/hito-1.md` si lo cita
- [ ] 7.3 `dotnet test`, `openspec validate --all --strict` y revisión de las specs con lo aprendido; archivar el cambio
