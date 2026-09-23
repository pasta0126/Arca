## 1. Biblioteca de componentes

- [ ] 1.1 Crear el proyecto de componentes de UI, dependiente de Application solo por interfaces, y la prueba de arquitectura que impide referencias a Domain e Infrastructure (D1)
- [ ] 1.2 Conjunto mínimo de recursos de tema con nombre (colores semánticos, tipografías y espaciados) y su contrato para `ui-shell` (D13)
- [ ] 1.3 Planificador de tiempo inyectable para pruebas deterministas de temporizadores (D2)
- [ ] 1.4 Configurar las pruebas de renderizado sin ventana de Avalonia y comprobar que se ejecutan en Windows, Linux y macOS

## 2. Feedback

- [ ] 2.1 `INotificationService` y modelo de vista de notificaciones apiladas: éxito con desaparición y pausa con el ratón, aviso y error persistentes, cola y máximo visible (D3)
- [ ] 2.2 Conversión de resultado estructurado y código de error a notificación con `ILocalizer`, con la clave visible si falta el recurso y registro de ello
- [ ] 2.3 Error inesperado con mensaje genérico y referencia del registro técnico, sin datos de alumnos
- [ ] 2.4 `IConfirmationService` y diálogo: consecuencia, recuentos, acción destructiva, foco inicial en cancelar, Escape y un solo diálogo (D4)
- [ ] 2.5 Comando asíncrono de ejecución única con indicador de trabajo tras 300 ms y resultado hacia las notificaciones (D5)
- [ ] 2.6 Componente de progreso con "N de M", cancelación condicionada y motivo cuando no es cancelable (D6)
- [ ] 2.7 Componentes de estado de carga y estado vacío con acción principal y limpiar filtro
- [ ] 2.8 Pruebas: cada escenario de los specs sobre los modelos de vista, doble ejecución, acción de menos de 300 ms, recurso ausente y cambio de tema sin cambios de código

## 3. Adaptabilidad

- [ ] 3.1 Tamaño mínimo de ventana y disposición adaptable con umbral de apilado (D9)
- [ ] 3.2 Sección colapsable con título, resumen y expansión automática ante error de validación
- [ ] 3.3 Vista compacta de listas de zonas y taquillas
- [ ] 3.4 Preferencias locales de interfaz con lectura tolerante y corrección de posición fuera de pantalla (D10)
- [ ] 3.5 Pruebas con DPI del 100 %, 150 % y 200 %, ventana reducida, preferencias dañadas y posición inválida

## 4. Teclado y menús

- [ ] 4.1 Registro único de acciones con nombre, atajo por sistema, disponibilidad y motivo (D11)
- [ ] 4.2 Atajos fijos de buscar, nuevo, confirmar, cancelar y ayuda, con Control o Comando según el sistema
- [ ] 4.3 Menús y descripciones emergentes que leen el atajo del registro
- [ ] 4.4 Menú contextual construido de la misma lista de acciones que los botones, con las no disponibles deshabilitadas y su motivo, y apertura con la tecla de menú
- [ ] 4.5 Orden de foco, foco visible con recurso de tema y devolución del foco al cerrar diálogos
- [ ] 4.6 Pruebas: recorrido con Tab, atajos por sistema, atajo no aplicable, acción deshabilitada con motivo y foco tras un diálogo

## 5. Arrastrar y soltar

- [ ] 5.1 Comportamiento de arrastre de un alumno que produce la intención alumno-taquilla y usa el comando de asignación existente (D12)
- [ ] 5.2 Respuesta visual del destino con la comprobación previa de asignación, sin escribir datos, y motivo cuando no es válido
- [ ] 5.3 Cancelación con Escape, soltar fuera y protección contra soltar dos veces
- [ ] 5.4 Alternativas de menú y teclado con el mismo resultado y los mismos avisos
- [ ] 5.5 Pruebas del modelo de la intención y de que el arrastre limita los casos a alumno sobre taquilla, y prueba manual en Windows, Linux y macOS

## 6. Listas, planes y pasos

- [ ] 6.1 Lista virtualizada con orden, filtro y selección persistente por identidad (D7)
- [ ] 6.2 Selección múltiple con seleccionar todo, quitar, invertir, teclado y recuento "N de M"
- [ ] 6.3 Vista de plan de operaciones masivas con selección editable, bloqueos con motivo, plan que cambia y confirmación
- [ ] 6.4 Componente de pasos con estado, progreso, siguiente paso, apertura en cualquier orden y omisión solo de opcionales (D8)
- [ ] 6.5 Pruebas: 300 filas virtualizadas, selección tras ordenar y filtrar, plan cambiado, elementos bloqueados y pasos de cierre y de configuración con el mismo componente

## 7. Verificación transversal

- [ ] 7.1 Prueba de arquitectura: los comandos que modifican datos usan el comando de ejecución única (D5)
- [ ] 7.2 Prueba automática de que todas las claves de recurso nuevas existen en catalán y de que los componentes no contienen textos ni colores literales
- [ ] 7.3 Prueba de privacidad: notificaciones y errores no dejan datos de alumnos en el registro técnico
- [ ] 7.4 Ejemplo mínimo de uso de cada componente para documentar cómo lo consume `ui-shell`
- [ ] 7.5 Documentar los puntos de enganche con `ui-shell` (tema, navegación, dónde se ofrece cada componente)
