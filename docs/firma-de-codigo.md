# Firma de código en Windows

Valoración del 2026-09-26 (solo Windows; macOS queda fuera por ahora: sin cuenta de Apple, la aplicación se abre con «Abrir igualmente»). Es una decisión aplazada: **la v1 no se firma** (riesgo P8 de `docs/riesgos.md`); se decide antes del primer centro piloto. Las cifras vienen de las fuentes indicadas al final y hay que **reconfirmarlas** al decidir, porque cambian a menudo.

## Qué resuelve y qué no

Un instalador sin firmar muestra el aviso fuerte de SmartScreen. Firmar lo atenúa y muestra un editor identificado, pero **no lo elimina el primer día**: la reputación se gana con el tiempo y con descargas limpias del mismo firmante. Desde 2024 los certificados EV ya no saltan el aviso, así que no compensan (400 $ o más al año). En cualquier caso hay que documentar cómo autorizar la instalación.

## Opciones para un particular en España

| Opción | Coste | Encaje con ARCA |
|--------|-------|-----------------|
| **Certum «Open Source Code Signing»** (nube, SimplySign) | ≈ 50 $/año (precio de un revendedor; comprobar en la tienda de Certum) | Solo particulares. El editor sale como «Open Source Developer» y el nombre. Se firma desde los scripts locales: sin CI ni cambios en el proyecto. Vigencia máxima de 459 días desde febrero de 2026 |
| **SignPath Foundation** (firma gratuita para código abierto) | 0 € | Cumple la licencia (GPL-3.0-or-later), el repositorio público y la autenticación de dos factores. Exige que el binario se construya de forma verificable en CI (GitHub Actions), aprobación manual de cada versión, versión ya publicada, página de descarga, política de privacidad y política de firma publicadas. El editor que ve el usuario es «SignPath Foundation». Revisión manual de una a dos semanas |
| **Microsoft Store** (paquete MSIX; Microsoft lo firma) | 0 € (el registro de particulares es gratuito, con DNI y selfie) | Sin aviso de SmartScreen. Obliga a empaquetar como MSIX y pasar la certificación en cada versión; los centros instalarían desde la Store (sin versión portable ni «sustituir el ejecutable»). **Riesgo sin verificar:** muchos equipos de centros educativos tienen la Store bloqueada |
| Certificado OV tradicional (DigiCert, Sectigo…) | 150–300 $/año | Clave en token USB o HSM en la nube (obligatorio desde junio de 2023). Identidad validada en unos días |
| Azure Artifact Signing | ≈ 10 $/mes | **No disponible** para particulares fuera de EE. UU. y Canadá; en la UE solo para organizaciones |

## Recomendación

1. **v1: no firmar**, documentar el aviso y cómo autorizar.
2. **Primer centro piloto:** elegir entre **Certum** (≈ 50 $/año, sin cambios en el proceso) y **SignPath** (0 €, pero exige CI en GitHub Actions, que es gratuita en repositorios públicos y obliga a revisar la decisión de «sin CI remota»; y el editor se llamará «SignPath Foundation»).
3. **Microsoft Store** como canal adicional solo si los conserjes confirman que la tienen habilitada (pregunta añadida a `docs/roadmap.md`).

Coste esperado: **0 a 50 $ al año**, frente a 150–400 $ de un certificado tradicional.

## Fuentes (consultadas el 2026-09-26)

- Microsoft Learn: «Code signing options for Windows app developers» y «SmartScreen reputation for Windows app developers».
- Windows Developer Blog: «Free developer registration for individual developers on Microsoft Store» (2025-09-10).
- SignPath Foundation: condiciones para proyectos de código abierto (`signpath.org/terms.html`) y documentación de sistemas de construcción de confianza.
- Certum (a través de revendedores): Open Source Code Signing y Cloud CODE Signing para particulares.
