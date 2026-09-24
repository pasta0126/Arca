# Glosario

Punto 5 de `docs/preparacion-desarrollo.md`. Fija cómo se llama cada concepto en los tres idiomas del proyecto, para que nadie traduzca por su cuenta.

| Idioma | Dónde se usa |
|--------|--------------|
| **Castellano** | Specs, documentación y conversación del equipo |
| **Catalán** | Todo texto que ve el conserje (interfaz, mensajes, cabeceras de informes) |
| **Inglés** | Identificadores de código, pruebas, códigos de error y commits |

Estado de cada término: **fijado** (decidido) o **validar** (propuesta a confirmar con los conserjes antes de la primera versión visible).

## Reglas de uso

1. **Un concepto, un nombre.** Si aparece un sinónimo en una spec o en el código, se corrige al del glosario.
2. **El catalán manda en la interfaz.** Los términos del castellano de las specs (por ejemplo "moroso") no se muestran tal cual si el glosario dice otra cosa.
3. **Los identificadores en inglés son estables**, porque forman parte de los códigos de error, las claves de recurso y los tipos de evento. No se renombran una vez publicados; se añade uno nuevo.
4. **Estilo de los textos en catalán**: registro neutro, sin dirigirse al usuario por su nombre ni por *tu* o *vostè* cuando se pueda evitar (infinitivos e impersonales: "Triar una taquilla", "Cal definir els imports"). Primera letra en mayúscula solo al inicio de frase y en nombres propios.
5. **Desambiguar por contexto.** Las palabras con dos sentidos (clave, devuelto, baja) tienen un identificador distinto para cada sentido (ver la columna de notas).

## Centro, curso y personas

| Castellano | Catalán | Inglés | Estado | Notas |
|------------|---------|--------|--------|-------|
| centro educativo | centre educatiu | `School` | fijado | |
| conserje | conserge | *(no aparece en el código)* | fijado | Usuario de la aplicación |
| curso escolar | curs escolar | `SchoolYear` | fijado | Ejemplo: "2026-2027" |
| curso activo | curs actiu | `Active` | fijado | Solo uno a la vez |
| curso sin activar | curs sense activar | `NotActivated` | fijado | Creado y nunca activado |
| curso en cierre | curs en tancament | `Closing` | fijado | Estado intermedio, con pasos pendientes |
| curso cerrado | curs tancat | `Closed` | fijado | Solo lectura; no se reabre |
| alumno | alumne | `Student` | fijado | Ficha única que persiste entre cursos |
| matrícula | matrícula | `Enrollment` | fijado | Alumno + curso + nivel + grupo |
| nivel | nivell | `Level` | fijado | 1r ESO, 2n Batxillerat... |
| grupo | grup | `Group` | fijado | Pertenece a un nivel |
| alumno de baja | alumne de baixa | `Withdrawn` | fijado | Estado del alumno |
| reactivar (alumno) | reactivar | `Reactivate` | fijado | |
| identificador (de secretaría) | identificador | `Identifier` | fijado | Dato opcional, no se muestra |
| correo | correu | `Email` | fijado | Dato opcional, no se muestra |

## Taquillas y zonas

| Castellano | Catalán | Inglés | Estado | Notas |
|------------|---------|--------|--------|-------|
| taquilla | taquilla | `Locker` | fijado | |
| zona | zona | `Zone` | fijado | Antes "pasillo" en algunas conversaciones; el término es *zona* |
| número (de taquilla) | número | `Number` | fijado | Único solo entre las activas |
| taquilla de baja | taquilla de baixa | `Retired` | fijado | Irreversible; distinta de *alumno de baja* (`Withdrawn`) |
| estado visible | estat visible | `LockerStatus` | fijado | Derivado de hechos |
| libre | lliure | `Free` | fijado | |
| ocupada | ocupada | `Occupied` | fijado | |
| reservada | reservada | `Reserved` | fijado | Con o sin alumno |
| fuera de servicio | fora de servei | `OutOfService` | fijado | Agrupa averiada y en mantenimiento |
| averiada | avariada | `Broken` | fijado | |
| en mantenimiento | en manteniment | `UnderMaintenance` | fijado | |
| decisión al poner fuera de servicio | decisió | `OutOfServiceDecision` | fijado | Mantener, reasignar o liberar |
| alta por rangos | alta per rangs | `RangeCreation` | fijado | |
| historial | historial | `History` | fijado | Solo añadir |

## Asignaciones

| Castellano | Catalán | Inglés | Estado | Notas |
|------------|---------|--------|--------|-------|
| asignación | assignació | `Assignment` | fijado | Vigente mientras no tiene fin |
| asignar | assignar | `Assign` | fijado | |
| liberar (taquilla) | alliberar | `Release` | fijado | |
| cambiar de taquilla | canviar de taquilla | `ChangeLocker` | fijado | |
| reservar | reservar | `Reserve` | fijado | |
| sugerencia de taquilla | suggeriment | `Suggestion` | fijado | La libre de menor número de la zona |
| aviso | avís | `Warning` | fijado | Exige confirmación |
| impedimento | impediment | `Blocker` | fijado | Rechaza la operación |

## Cobros

| Castellano | Catalán | Inglés | Estado | Notas |
|------------|---------|--------|--------|-------|
| cargo | càrrec | `Charge` | fijado | Un cobro de un alumno y concepto |
| cuota (anual) | quota | `Fee` | fijado | Por alumno y curso |
| **fianza** | **dipòsit** | `Deposit` | fijado | Único por estancia. **Ojo**: en la interfaz puede confundirse con un depósito bancario; los textos deben decir siempre "dipòsit de la taquilla" o "dipòsit" junto a su importe. |
| reposición de llave | reposició de clau | `KeyReplacementFee` | fijado | Concepto de cobro |
| importe | import | `Amount` | fijado | Tipo `Money` |
| pendiente | pendent | `Pending` | fijado | Estado de un cargo |
| pagado | pagat | `Paid` | fijado | Con fecha |
| exento | exempt | `Exempt` | fijado | Con motivo |
| **condonado** | **condonat** | `Waived` | fijado | Con motivo; distinto de *anulado* |
| anulado | anul·lat | `Voided` | fijado | Estado final, sin deuda |
| revertir | revertir | `Revert` | fijado | Vuelve a pendiente, con motivo |
| motivo | motiu | `Reason` | fijado | |
| al corriente (de pago) | al corrent | `UpToDate` | fijado | |
| **moroso / con deuda** | **pendent de pagament** | `Outstanding` | fijado | En el código el concepto es *outstanding*. En la interfaz **no se usa la palabra "moroso"** (etiqueta sobre menores). Ejemplos: "Pendents de pagament", "Import pendent". |
| deuda de cursos anteriores | deute de cursos anteriors | `PriorDebt` | fijado | |
| arrastrar (deuda) | arrossegar | `CarryOver` | fijado | Queda pendiente y avisa al asignar |
| dipòsit por devolver | dipòsit per retornar | `PendingRefund` | fijado | Estado de la fianza tras la baja del alumno |
| dipòsit devuelto | dipòsit retornat | `Refunded` | fijado | En código `Refunded`, para no confundirlo con la llave `Returned` |

## Llaves

| Castellano | Catalán | Inglés | Estado | Notas |
|------------|---------|--------|--------|-------|
| llave (de la taquilla) | clau | `Key` | fijado | **Ojo**: no confundir con la clave de cifrado (`EncryptionKey`) |
| pendiente de entrega | pendent de lliurament | `PendingDelivery` | fijado | |
| entregada | lliurada | `Delivered` | fijado | |
| devuelta (llave) | retornada | `Returned` | fijado | |
| perdida | perduda | `Lost` | fijado | |
| repuesta | reposada | `Replaced` | validar | Hay llave nueva para la taquilla |
| copia (de llave) | còpia | `Copy` | fijado | Distinta de copia de seguridad (`Backup`) |
| llave disponible | clau disponible | `KeyAvailable` | fijado | Derivada |

## Incidencias y mantenimiento

| Castellano | Catalán | Inglés | Estado | Notas |
|------------|---------|--------|--------|-------|
| incidencia | incidència | `Incident` | fijado | Periodo fuera de servicio |
| abierta / reparada | oberta / reparada | `Open` / `Repaired` | fijado | Sin reapertura |
| motivo de incidencia | motiu de la incidència | `IncidentReason` | fijado | Lista editable |
| en bloque | en bloc | `Bulk` | fijado | Operaciones masivas |
| nota | nota | `Note` | fijado | Texto libre; dato sensible |

## Importaciones y operaciones en dos fases

| Castellano | Catalán | Inglés | Estado | Notas |
|------------|---------|--------|--------|-------|
| importación | importació | `Import` | fijado | Solo CSV |
| revisión previa | revisió prèvia | `Preview` | fijado | Sin efectos |
| plan | pla | `Plan` | fijado | Inmutable |
| confirmar / aplicar | confirmar / aplicar | `Confirm` / `Apply` | fijado | Revalida y aplica todo |
| conciliación | conciliació | `Reconciliation` | fijado | Fichero de secretaría como fuente de verdad |
| dudoso | dubtós | `Ambiguous` | fijado | Lo decide el usuario |
| nuevo / actualizado / sin cambios | nou / actualitzat / sense canvis | `New` / `Updated` / `Unchanged` | fijado | |
| correspondencia de columnas | correspondència de columnes | `ColumnMapping` | fijado | |
| informe | informe | `Report` | fijado | Solo CSV |
| exportar | exportar | `Export` | fijado | |

## Curso: cierre y conservación

| Castellano | Catalán | Inglés | Estado | Notas |
|------------|---------|--------|--------|-------|
| cierre de curso | tancament de curs | `Closing` | fijado | Proceso guiado |
| paso (de un asistente) | pas | `Step` | fijado | |
| hecho / omitido / pendiente | fet / omès / pendent | `Done` / `Skipped` / `Pending` | fijado | Estados de un paso |
| conservación de datos | conservació de dades | `Retention` | fijado | |
| conservar / anonimizar / borrar | conservar / anonimitzar / esborrar | `Keep` / `Anonymize` / `Delete` | fijado | Por curso cerrado |
| decidir más tarde | decidir més tard | `Undecided` | fijado | |

## Copias, registro y actualizaciones

| Castellano | Catalán | Inglés | Estado | Notas |
|------------|---------|--------|--------|-------|
| copia de seguridad | còpia de seguretat | `Backup` | fijado | Manual |
| restaurar | restaurar | `Restore` | fijado | |
| registro de la instalación | registre de la instal·lació | `Registration` | fijado | Opcional, desactivado por defecto |
| identificador de instalación | identificador d'instal·lació | `InstallationId` | fijado | Aleatorio, no derivado del equipo |
| versión nueva | versió nova | `NewVersion` | fijado | |
| aviso de versión | avís de versió | `UpdateNotice` | fijado | No bloquea nunca |
| versión crítica | versió crítica | `CriticalUpdate` | fijado | Aviso persistente, sin bloqueo |
| comprobar ahora | comprovar ara | `CheckNow` | fijado | Comprobación a petición |
| actualizar | actualitzar | `Update` | fijado | Sustituir el ejecutable; la base de datos se conserva |

## Acceso y cifrado

| Castellano | Catalán | Inglés | Estado | Notas |
|------------|---------|--------|--------|-------|
| contraseña del centro | contrasenya del centre | `CenterPassword` | fijado | Compartida, sin usuarios ni roles |
| clave de recuperación | clau de recuperació | `RecoveryKey` | fijado | 26 caracteres, se muestra una vez |
| fichero de claves | fitxer de claus | `KeyFile` | fijado | Llave de la base envuelta; sin datos personales |
| desbloquear | desbloquejar | `Unlock` | fijado | Pedir la contraseña al abrir |

## Interfaz y configuración

| Castellano | Catalán | Inglés | Estado | Notas |
|------------|---------|--------|--------|-------|
| configuración guiada | configuració guiada | `Setup` | fijado | Asistente de pasos |
| primera ejecución | primera execució | `FirstRun` | fijado | |
| ajustes | ajustos | `Settings` | fijado | |
| identidad del centro | identitat del centre | `SchoolIdentity` | fijado | Nombre, logo y color |
| color de acento | color d'accent | `Accent` | fijado | |
| tema claro / oscuro / del sistema | tema clar / fosc / del sistema | `Light` / `Dark` / `System` | fijado | |
| notificación | notificació | `Notification` | fijado | |
| estado vacío | estat buit | `EmptyState` | fijado | |

### Secciones de la barra lateral (`ui-shell`)

| Castellano | Catalán | Inglés |
|------------|---------|--------|
| Inicio | Inici | `Home` |
| Taquillas | Taquilles | `Lockers` |
| Alumnos | Alumnes | `Students` |
| Cobros | Cobraments | `Payments` |
| Llaves e incidencias | Claus i incidències | `KeysAndIncidents` |
| Informes | Informes | `Reports` |
| Curso | Curs | `SchoolYear` |
| Ajustes | Ajustos | `Settings` |

## Palabras con dos sentidos (desambiguación)

| Palabra | Sentido 1 | Sentido 2 | Cómo se distingue |
|---------|-----------|-----------|-------------------|
| clave | llave de la taquilla | clave de cifrado | `Key` / `EncryptionKey`; en catalán *clau* con complemento |
| devuelta | llave devuelta | dipòsit devuelto | `Returned` (llave) / `Refunded` (dipòsit) |
| baja | alumno de baja | taquilla de baja | `Withdrawn` / `Retired` |
| copia | copia de llave | copia de seguridad | `Copy` / `Backup` |
| pendiente | cargo pendiente | paso pendiente | Mismo nombre en el código, distinto tipo (`ChargeStatus.Pending`, `StepStatus.Pending`) |
| cierre | cierre de curso | cierre de incidencia | `Closing` / `Repaired` |
| reposición | cobro de reposición de llave | llave repuesta | `KeyReplacementFee` / `Replaced` |

## Términos a validar con los conserjes

Antes de que exista una interfaz visible, conviene comprobar con quien la usará:

1. **"Pendents de pagament"** en lugar de "morosos": ¿se entiende igual de rápido?
2. **"Dipòsit"** para la fianza: ¿lo reconocen las familias y los conserjes, o prefieren "fiança"? (Es el término elegido; la duda es por confusión con un depósito bancario.)
3. **"Reposada"** para una llave repuesta: ¿existe una palabra más natural?
4. **"Curs en tancament"** y **"condonat"**: ¿suenan naturales?
5. **"Alliberar"** una taquilla frente a otra palabra que ya usen ("deixar lliure", "retornar").
6. Cualquier término que los conserjes ya usen entre ellos y que no esté aquí.
