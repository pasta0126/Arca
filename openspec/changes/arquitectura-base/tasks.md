## 1. Solución y reglas de capas

- [x] 1.1 Crear la solución .NET con los proyectos `Arca.Domain`, `Arca.Application`, `Arca.Infrastructure` y `Arca.Desktop`, y un proyecto de pruebas por capa
- [x] 1.2 Fijar la versión LTS de .NET y la versión de Avalonia; centralizar versiones de paquetes
- [x] 1.3 Configurar las referencias según D1 y añadir una prueba de arquitectura que falle si se incumplen
- [x] 1.4 Añadir analizadores y `.editorconfig` con las convenciones del proyecto (identificadores y commits en inglés)

## 2. Puertos y utilidades transversales

- [x] 2.1 Definir `IClock` y su implementación real y de pruebas (fecha de calendario e instante UTC)
- [x] 2.2 Definir el tipo de importe con precisión exacta y su conversión a céntimos
- [x] 2.3 Definir el tipo de error de negocio con código estable independiente del idioma
- [x] 2.4 Centralizar la comparación y búsqueda de texto según la cultura catalana, insensible a mayúsculas y acentos
- [x] 2.5 Pruebas: suma exacta de pagos parciales, fechas estables entre zonas horarias, búsqueda "garcia" ↔ "García", nombres con "ç" y "l·l"

## 3. Almacenamiento local cifrado

- [x] 3.1 Integrar EF Core con SQLite cifrado (SQLite3 Multiple Ciphers en formato SQLCipher 4) en `Infrastructure` y comprobar que carga en Windows, Linux y macOS (hecho en macOS; Linux descartado por ahora y Windows antes de producción, `docs/stack.md`)
- [x] 3.2 Integrar la apertura de la base con la llave que entrega `acces-i-xifrat` (contraseña del centro y fichero de claves), sin ningún secreto en el código, y con una contraseña de prueba solo en los proyectos de pruebas
- [x] 3.3 Implementar la creación de la base de datos en el primer arranque aplicando todas las migraciones desde cero
- [x] 3.4 Prueba: el fichero no se puede leer con una herramienta SQLite estándar sin la clave
- [x] 3.5 Prueba: un fichero creado en un sistema se abre en otro (fichero de ejemplo versionado en el repositorio de pruebas; creado en macOS, se abre en Windows en la verificación previa a producción)
- [x] 3.6 Detección y mensaje claro para fichero dañado o que no es una base de datos de ARCA, sin modificarlo

## 4. Migraciones de esquema

- [x] 4.1 Crear la migración inicial de EF Core y el servicio migrador propio que sustituye al `Migrate()` directo (D5)
- [x] 4.2 Rechazar sin tocar el fichero una base de datos cuyo historial contiene migraciones desconocidas (versión más nueva)
- [x] 4.3 Crear la copia previa junto al original y verificar su integridad antes de migrar
- [x] 4.4 Ejecutar las migraciones en una transacción con reversión completa ante fallos
- [x] 4.5 Conservar solo las 3 últimas copias previas a migración tras migrar con éxito
- [x] 4.6 Prueba que falla si el modelo de EF Core tiene cambios sin migración
- [x] 4.7 Pruebas: esquema ya actualizado (sin copia), migración correcta, fallo a mitad de camino, copia corrupta, versión más nueva, retención de copias

## 5. Ubicación, modo portable e instancia única

- [x] 5.1 Resolver la ruta por defecto por sistema operativo sin requerir administrador
- [x] 5.2 Implementar el modo portable por fichero marcador junto al ejecutable (D6)
- [x] 5.3 Guardar y leer la ruta configurada en un fichero de configuración local
- [x] 5.4 Validar la ruta (existencia, permisos de escritura) con mensaje claro y sin crear ni modificar ficheros si falla
- [x] 5.5 Implementar el bloqueo de instancia única asociado al fichero de base de datos
- [x] 5.6 Pruebas: ruta por defecto, ruta configurada, ruta inaccesible, modo portable con y sin marcador, segunda instancia

## 6. Internacionalización

- [ ] 6.1 Crear `ILocalizer` en Application y su implementación con recursos `.resx`, idioma base catalán
- [ ] 6.2 Fijar la cultura de formato en catalán de España, ignorando la del sistema operativo
- [ ] 6.3 Resolver el mensaje visible de cada código de error desde recursos
- [ ] 6.4 Retroceso al catalán por clave en idiomas adicionales; en ejecución, una clave inexistente muestra la propia clave
- [ ] 6.5 Prueba automática que recorre las claves usadas y falla si alguna no está en el idioma base
- [ ] 6.6 Pruebas: formato de importe y de fecha en catalán con el sistema en otra cultura; idioma adicional incompleto

## 7. Aplicación de escritorio mínima

- [ ] 7.1 Crear el proyecto Avalonia con la raíz de composición (inyección de dependencias) y arranque que abre la base de datos
- [ ] 7.2 Mostrar los errores de arranque (ruta inaccesible, fichero dañado, versión más nueva, segunda instancia) con mensajes localizados y salida limpia
- [ ] 7.3 Crear una pantalla de información con versión de aplicación y versión de esquema
- [ ] 7.4 Comprobar que la aplicación arranca en Windows, Linux y macOS

## 8. Feedback, arranque y registro

- [ ] 8.1 Definir el tipo de resultado estructurado (éxito con datos, avisos, error con código) y el informador de progreso con token de cancelación (D12)
- [ ] 8.2 Prueba de arquitectura que exige que los casos de uso públicos devuelvan el tipo de resultado
- [ ] 8.3 Implementar la secuencia de arranque por etapas con informe de avance y detención ante fallo (D13)
- [ ] 8.4 Crear la pantalla de arranque ligera con identidad de la aplicación, texto de etapa y modo de error con referencia
- [ ] 8.5 Implementar el ejecutor común de comandos: estado en curso, deshabilitado contra doble clic, indicador tras 300 ms y recogida del resultado (D14)
- [ ] 8.6 Definir el servicio de notificaciones (éxito transitorio, error persistente, historial de sesión) y el servicio de confirmación de acciones irreversibles, como puertos con implementación en la UI
- [ ] 8.7 Desactivar la carga perezosa de EF Core y fijar la carga explícita como norma; prueba que detecta proxies de carga perezosa (D15)
- [ ] 8.8 Implementar el registro en fichero con rotación, tamaño máximo, referencia de error y política de privacidad (D16)
- [ ] 8.9 Pruebas: éxito, error de negocio, aviso con éxito parcial, error inesperado con referencia, acción rápida sin indicador, doble ejecución, cancelación antes y durante el guardado, registro sin datos personales y registro acotado
- [ ] 8.10 Claves de recurso en catalán para todos los mensajes de arranque, resultado, confirmación, progreso y estados vacíos genéricos

## 9. Verificación y distribución

- [ ] 9.1 Crear los scripts de verificación `build/test.sh`, `build/test.ps1` y `build/test-linux.sh` (contenedor) que compilen con advertencias como errores y ejecuten todas las pruebas, y `build/publish.sh` para generar paquetes por sistema, incluido `win-x64` desde macOS
- [ ] 9.1b Añadir a los scripts de verificación un control de licencias que liste las de todos los paquetes NuGet y falle si alguna no está en la lista permitida (MIT, Apache 2.0, BSD, ISC), compatibles con la GPL-3.0, y crear `THIRD-PARTY-NOTICES.md` (coste cero, `docs/stack.md`)
- [ ] 9.2 Generar el paquete portable con fichero marcador: `zip` en Windows, `tar.gz` en Linux y `.app` comprimido en `.zip` en macOS (D11)
- [ ] 9.3 Generar el instalador de Windows con Inno Setup, sin requerir conexión, con instalación por usuario o por equipo
- [ ] 9.4 Comprobar que desinstalar conserva la base de datos y que instalar sobre una versión anterior conserva los datos y migra en el siguiente arranque
- [ ] 9.5 Documentar para la dirección del centro el alcance real del cifrado, cómo guardar la clave de recuperación y qué ocurre si se pierden la contraseña y la clave
