## 1. Estado local y contenido enviado

- [ ] 1.1 Ajustes locales de consentimiento de registro y de avisos, identificador aleatorio, datos opcionales, fechas de envío, última versión avisada y borrado pendiente, con lectura tolerante (D1)
- [ ] 1.2 Tipo cerrado `RegistrationPayload` construido solo desde los ajustes y el sistema, con generación del identificador aleatorio (D2)
- [ ] 1.3 Validación del correo opcional y de la longitud de los datos de centro
- [ ] 1.4 Pruebas: contenido mínimo, con opcionales, sin datos de alumnos, identificador estable y aleatorio, y prueba de contrato contra la lista documentada

## 2. Envío y consentimiento

- [ ] 2.1 Servicio único de decisión de red con las reglas de consentimiento y de una vez cada 24 horas (D3)
- [ ] 2.2 Cliente HTTP con tiempo de espera de 5 segundos, sin reintentos, en segundo plano tras el arranque completo
- [ ] 2.3 Retirada del consentimiento, solicitud de borrado con pendiente sin conexión y envío diferido (D9)
- [ ] 2.4 Registro técnico de fallos sin identificador completo, correo ni contenido
- [ ] 2.5 Pruebas con manejador HTTP falso: sin consentimiento no hay ninguna petición, una vez al día, sin conexión, tiempo de espera, borrado con y sin conexión y ausencia de datos en el registro

## 3. Avisos de versión

- [ ] 3.1 Modelo de la información de versión y verificación de la firma Ed25519 con NSec y la clave pública embebida (D4)
- [ ] 3.2 Lista de dominios oficiales y comprobación de `https` en el enlace (D5)
- [ ] 3.3 Comparación semántica de versiones y estado del aviso: una vez por versión y crítica persistente (D6)
- [ ] 3.4 Comprobación independiente del registro (solo versión y sistema) y acción "Comprobar ahora"
- [ ] 3.5 Pruebas: firma válida, alterada y ausente, enlace de otro dominio o sin `https`, versiones iguales, anteriores y posteriores, aviso descartado, crítica, y funcionamiento sin conexión durante meses

## 4. Actualización

- [ ] 4.1 Oferta de copia de seguridad en el aviso, enlazando con el asistente de `copies-de-seguretat` (D7)
- [ ] 4.2 Prueba de extremo a extremo: base de datos con datos, sustitución del ejecutable por uno con migraciones nuevas, copia previa, migración e integridad de los datos
- [ ] 4.3 Prueba de que una versión antigua se niega a abrir una base de datos más nueva
- [ ] 4.4 Documentar en la guía de usuario cómo actualizar la versión portable y la instalada

## 5. Pantallas y enganches

- [ ] 5.1 Paso de registro en la primera ejecución con las casillas de registro, avisos y correo y la lista exacta de datos, en coordinación con `configuracio-inicial`
- [ ] 5.2 Sección de ajustes con las mismas opciones, la acción de borrar el registro y el texto de transparencia
- [ ] 5.3 Aviso de versión nueva en el marco, en coordinación con `ui-shell`
- [ ] 5.4 Resultados y errores mediante las notificaciones comunes, con protección contra doble ejecución
- [ ] 5.5 Claves de recurso en catalán para todos los textos
- [ ] 5.6 Pruebas de mensajes, aviso descartable, crítica, doble clic y estados sin conexión

## 6. Verificación transversal

- [ ] 6.1 Prueba de arquitectura: el constructor del contenido enviado no referencia datos de alumnos, taquillas ni cobros
- [ ] 6.2 Prueba de privacidad: con una base de datos llena, la petición contiene solo los campos documentados
- [ ] 6.3 Prueba automática de que todas las claves de recurso nuevas existen en catalán
- [ ] 6.4 Publicar y mantener `docs/registro-de-instalaciones.md`, con la lista de campos comprobada por la prueba de contrato
- [ ] 6.5 Documentar el punto de enganche con el servidor de registro (proyecto aparte) y con `configuracio-inicial` y `ui-shell`
