# Dossier para la revisión legal (E11)

Documento para entregar a la persona profesional del derecho que revise ARCA. Resume el proyecto, las decisiones tomadas y las preguntas concretas, para que la consulta sea corta. **No es asesoramiento legal**: lo ha preparado el equipo de desarrollo, con ayuda de una herramienta de IA, sin validez jurídica.

## 1. Qué es ARCA

- **ARCA** es un acrónimo: *Administració de Recursos, Claus i Armariets* (nombre completo, que consta en la descripción del proyecto y en el `README`). Es aplicación de escritorio, **software libre y gratuito**, que ayuda a los conserjes de un centro educativo a gestionar taquillas, alumnos asignados, pagos, llaves e incidencias.
- Funciona en **un solo equipo**, sin nube. **Los datos de alumnos (menores) nunca salen del equipo.** Se guardan cifrados con una contraseña compartida del centro y una clave de recuperación.
- Titular de los derechos: **Guillermo Garcia Carballo**. Una sola persona la mantiene. Repositorio público: https://github.com/pasta0126/Arca
- Usuarios: centros educativos (públicos y privados), en Cataluña y potencialmente otros territorios. Interfaz en catalán.
- El mantenedor **no tiene acceso a los datos de los centros, no custodia ninguna clave y no recupera datos perdidos** (decisión deliberada).
- Modelo previsto: gratuito. Posibles ingresos futuros por servicios (implantación, soporte), donaciones o financiación institucional. **No se venden licencias.**

## 2. Documentos que acompañan

| Documento | Ruta en el repositorio |
|-----------|------------------------|
| Licencia (texto oficial GPL-3.0) | `LICENSE` |
| Política de marca (borrador) | `TRADEMARK.md` |
| Política de contribuciones | `CONTRIBUTING.md` |
| Política de seguridad | `SECURITY.md` |
| Lo que ARCA puede enviar fuera del equipo (registro opcional y avisos) | `docs/registro-de-instalaciones.md` |
| Especificación del registro y avisos | `openspec/changes/registre-i-actualitzacions/` |
| Especificación de contraseña y cifrado | `openspec/changes/acces-i-xifrat/` |
| Pila técnica y dependencias con sus licencias | `docs/stack.md` |
| Registro de riesgos | `docs/riesgos.md` |

## 3. Decisiones ya tomadas

| Tema | Decisión |
|------|----------|
| Licencia del código | GPL-3.0-o-posterior (`GPL-3.0-or-later`) |
| Marca | «ARCA» y su logotipo reservados; las versiones modificadas deben usar otro nombre |
| Contribuciones | No se aceptan propuestas de código o texto de terceros por ahora; sí ideas y avisos |
| Datos de alumnos | Nunca salen del equipo |
| Registro de instalaciones | Opcional, desactivado por defecto, contenido cerrado, consentimiento explícito |
| Avisos de versión | Firmados; sin descarga ni instalación automática; no bloquean el uso |
| Correo de aviso | Solo si el usuario lo escribe y lo consiente, con confirmación previa y baja |
| Cifrado | Contraseña compartida del centro y clave de recuperación; sin custodia por el mantenedor |
| Licencias de uso / claves de activación | No existen; el programa nunca se bloquea |

## 4. Preguntas para la persona profesional

### A. Titularidad y autoría

1. ¿Consta correctamente **Guillermo Garcia Carballo como único titular**? ¿Conviene una **declaración o depósito** que lo acredite (por ejemplo, registro en el Registro de la Propiedad Intelectual)?
2. Si el titular tiene o ha tenido una **relación laboral o de servicios**, ¿podría el empleador o cliente reclamar derechos sobre este proyecto? ¿Qué debe comprobarse?
3. Parte de la especificación y del código futuro se elabora **con ayuda de una herramienta de inteligencia artificial**, con dirección y revisión humanas. ¿Qué implicaciones tiene para la autoría y la titularidad, y cómo conviene documentar la aportación humana?
4. La política es **no aceptar código de terceros por ahora**. ¿Es suficiente `CONTRIBUTING.md`, o conviene una advertencia más formal? ¿Qué cambiaría si más adelante se aceptan contribuciones con DCO o con cesión (CLA)?

### B. Licencia

5. ¿Es adecuada la **GPL-3.0-o-posterior** para el objetivo (proyecto gratuito que evita forks cerrados)? ¿Hay diferencias relevantes entre «solo» y «o posterior»?
6. ¿Son **compatibles con la GPL-3.0** las dependencias previstas? Licencias: MIT (Avalonia, SQLite3 Multiple Ciphers, NSec.Cryptography, .NET, EF Core), Apache 2.0, BSD, ISC (libsodium). Una biblioteca de CSV con doble licencia MS-PL/Apache 2.0 se usaría bajo Apache 2.0.
7. ¿Es correcto el aviso de garantía y de limitación de responsabilidad de la GPL en el contexto español y de la UE, o conviene añadir un texto propio para los centros?
8. ¿Deben añadirse **avisos de licencia y de copyright** en cada fichero (SPDX), y un fichero `THIRD-PARTY-NOTICES.md`?

### C. Marca

9. El nombre **«ARCA»** (acrónimo de *Administració de Recursos, Claus i Armariets*) es muy común como sigla. Por ejemplo, una agencia tributaria de otro país usa esas siglas, y puede haber marcas registradas. ¿Se recomienda una **búsqueda de anterioridades** en España y la UE (clases 9, 41 y 42), y hay riesgo de conflicto?
10. ¿Conviene **registrar la marca** (OEPM o EUIPO) y en qué clases? ¿Es adecuado el borrador de `TRADEMARK.md`?
11. Si el nombre resultara problemático, ¿qué coste y qué pasos tendría cambiarlo cuanto antes?

### D. Protección de datos

12. **Roles**: el centro es el responsable del tratamiento de los datos de alumnos, y el mantenedor no accede a ellos ni los recibe. ¿Se confirma que **no es encargado del tratamiento**, incluso si un centro le envía voluntariamente una copia para pedir ayuda? ¿Qué texto conviene para ese caso?
13. **Registro opcional de instalaciones**: el mantenedor sería **responsable del tratamiento** de los datos que reciba (identificador aleatorio, versión, sistema, idioma, dirección IP y, si el usuario lo escribe, nombre y código del centro y correo).
    - ¿Es válida la **base jurídica del consentimiento** tal como se ofrece (casillas separadas, desactivado por defecto, contenido visible)?
    - ¿Es suficiente la **información a los usuarios** en la pantalla y en `docs/registro-de-instalaciones.md` (artículo 13 del RGPD)? ¿Qué datos de contacto debe indicar el responsable?
    - **Conservación**: 24 meses sin actividad; IP no conservada más de 7 días. ¿Es razonable?
    - ¿Hace falta **registro de actividades de tratamiento**, o hay excepciones por tamaño?
    - Si el servidor estuviera fuera de la UE, ¿qué implicaría?
14. Los **avisos por correo** de versión nueva, con doble confirmación y enlace de baja: ¿qué exigen la LSSI y el RGPD?
15. El **almacenamiento de un identificador aleatorio** en el equipo y su envío: ¿aplica la normativa sobre dispositivos terminales (artículo 22.2 de la LSSI) y qué información previa exige?
16. ¿Qué **documentación debería ofrecerse a los centros** y a sus delegados de protección de datos (ficha de datos personales, medidas de seguridad según el artículo 32, evaluación de impacto si procede)?
17. Los **centros públicos**, ¿necesitan autorizaciones (dirección, consejo escolar, departamento) o cumplir el Esquema Nacional de Seguridad para instalar y usar ARCA?

### E. Responsabilidad y distribución

18. **Pérdida de datos por olvido de contraseña y clave**: el mantenedor no custodia nada. ¿Es suficiente la advertencia prevista, obligatoria y confirmada al crear la contraseña?
19. ¿Qué **límites de responsabilidad** son posibles y válidos para un software gratuito que gestiona datos de menores, dado que hay responsabilidades que no se pueden excluir por dolo o negligencia grave?
20. **Reglamento de Ciberresiliencia de la UE (Cyber Resilience Act)**: ¿afecta a un proyecto de software libre gratuito con un solo mantenedor? ¿Qué obligaciones tendría, por ejemplo de notificación de vulnerabilidades, y desde cuándo?
21. La distribución de **binarios sin firma de código** (por coste) genera avisos del sistema. ¿Hay implicaciones legales, aparte de la información al usuario?
22. Si en el futuro hay **ingresos por servicios**, ¿qué forma jurídica o alta se necesita (autónomo, asociación, fundación) y cómo afecta a la titularidad del software?

### F. Documentos que redactaría la persona profesional

- Aviso de privacidad del registro de instalaciones.
- Texto de condiciones de uso y descarga, si procede.
- Versión definitiva de la política de marca.
- Acuerdo tipo de cesión o de DCO si algún día se aceptan contribuciones.
- Modelo de contrato de prestación de servicios para implantación y soporte.

## 5. Qué ya se ha comprobado (sin valor legal)

- No hay secretos ni claves en los ficheros ni en el historial del repositorio.
- Las dependencias previstas tienen licencias MIT, Apache 2.0, BSD o ISC; se comprobará en cada versión con un control automático.
- El repositorio tiene un `LICENSE` con el texto oficial de la GPL-3.0, política de marca, de contribuciones y de seguridad.

## 6. Cómo conseguir la consulta sin coste o a bajo coste

Como el proyecto sigue la regla de coste cero, conviene empezar por opciones gratuitas o baratas y valorar después una consulta puntual:

- **Redes de asesoría legal sobre software libre**, por ejemplo la de la Free Software Foundation Europe, que orienta a proyectos pequeños.
- **Servicios de orientación jurídica** de los colegios de abogados.
- **Oficinas de transferencia de conocimiento** de universidades, si hay alguna vinculación con el proyecto.
- Una **consulta puntual de una hora** con un abogado de propiedad intelectual y protección de datos, con este dossier como guion.

*Disponibilidad y condiciones por verificar en cada caso.*
