## Context

Ver `proposal.md` (Why). Estado actual: no hay ningún icono en el repositorio; `Arca.Desktop` es una aplicación Avalonia 12 (.NET 10) que se empaqueta con `build/package.sh` (zip portable para Windows, tar.gz para Linux y `ARCA.app` sin firmar para macOS). El instalador de Windows (Inno Setup) queda para más adelante y usará el mismo `.ico`.

## Goals / Non-Goals

**Goals:**
- Un icono correcto y neutro en todos los sitios del sistema, hoy.
- Que sustituirlo sea una operación de un fichero y un script.

**Non-Goals:**
- El diseño gráfico definitivo de la marca.

## Decisions

**D1. Dibujo vectorial propio, sencillo.** Un SVG de 1024 × 1024 con un cuadrado redondeado en azul grisáceo pastel (el acento por defecto del tema), la puerta de una taquilla (rectángulo con una rendija de ventilación y un pomo) en un tono más claro y sin degradados ni sombras, para que aguante 16 píxeles. Se dibuja a mano en el propio SVG; no se usa ningún banco de iconos. *Alternativa*: reutilizar un icono de Material Design (ya se usa en la interfaz): se descarta porque es un símbolo genérico de la librería y no una identidad propia.

**D2. Maestro vectorial y derivados versionados.** `assets/icon/arca.svg` es el maestro; `assets/icon/` guarda también `arca-16…512.png`, `arca.ico` y `arca.icns`. El script `build/icons.sh` regenera los derivados con herramientas gratuitas (en macOS `rsvg-convert`/`sips` e `iconutil`; en Linux ImageMagick o `rsvg-convert`; el `.ico` con ImageMagick). Los derivados van en git para que `dotnet build` no dependa de esas herramientas. *Alternativa*: generarlos al compilar; se descarta porque obligaría a instalar herramientas en cada equipo.

**D3. Comprobación de derivados al día.** `build/test.sh` compara un resumen del maestro con uno guardado junto a los derivados (`assets/icon/arca.svg.sha256`) y falla con un mensaje claro si el maestro cambió sin regenerar. Es una comprobación de texto, sin herramientas de imágenes.

**D4. Icono en la ventana.** El PNG de 256 píxeles y el `.ico` se incrustan como recurso de `Arca.Desktop` y se asignan con `Window.Icon` a la ventana principal y, desde la base común de ventanas, a los diálogos (formularios, confirmaciones y selectores), de modo que ninguna ventana nueva muestre el genérico. Incrustado en el ensamblado, funciona también en modo portable.

**D5. Empaquetado por sistema.** Windows: `<ApplicationIcon>` con el `.ico` en `Arca.Desktop.csproj`. macOS: `package.sh` copia el `.icns` a `ARCA.app/Contents/Resources` y declara `CFBundleIconFile` en `Info.plist`. Linux: el `tar.gz` incluye `arca.png` y `arca.desktop` de ejemplo (con `Icon=` apuntando a la imagen).

**D6. Separación del logotipo del centro.** El icono de la aplicación es un recurso del ensamblado y nunca se lee de la base de datos; el logotipo del centro sigue guardado en `CentreIdentity` y solo se dibuja en la cabecera (`identitat-i-tema`).

## Risks / Trade-offs

- [Un dibujo sencillo puede parecerse a otros iconos de taquillas] → es provisional; la marca definitiva se decide aparte.
- [Las herramientas de generación difieren entre macOS y Linux] → el script detecta cuál hay y avisa de qué falta; los derivados versionados evitan el problema en compilación.
- [Un `.icns` sin firmar no cambia el aviso de macOS sobre aplicaciones no firmadas] → es una limitación conocida documentada en `docs/firma-de-codigo.md`; el icono no la resuelve.

## Migration Plan

1. Añadir `assets/icon/` y `build/icons.sh` y generar los derivados.
2. Incrustar el recurso y asignarlo a las ventanas.
3. Ajustar `package.sh` y el proyecto de escritorio.
4. Retroceso: quitar el recurso y las líneas de empaquetado; la aplicación vuelve al icono genérico sin más efecto.
