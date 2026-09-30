# Icono de ARCA

Icono **provisional** de la aplicación (cambio `icona-d-aplicacio`): una puerta de taquilla sobre el color de acento pastel del tema, sin texto.

- **Procedencia**: dibujo propio hecho para este proyecto (`arca.svg`, formas geométricas simples), con la licencia del repositorio, GPL-3.0-o-posterior. No usa imágenes, fuentes ni bancos de iconos de terceros. La marca y el logotipo definitivos de ARCA son una decisión aparte (`TRADEMARK.md`).
- **Maestro**: `arca.svg` (vectorial). Todo lo demás se genera de él.
- **Derivados** (versionados, para que compilar no necesite herramientas de imágenes): `arca-16…1024.png`, `arca.ico` (Windows), `arca.icns` (macOS) y `arca.svg.sha256` (resumen del maestro con el que comprueban los tests que los derivados están al día).

## Cambiar el icono

1. Sustituir `arca.svg` por el dibujo nuevo (cuadrado, 1024 × 1024).
2. Ejecutar `build/icons.sh`.
3. Hacer commit de los ficheros que cambien. Nada del código ni de las especificaciones depende del dibujo.

`build/icons.sh` dibuja los PNG con la herramienta gratuita que encuentre (`sips` en macOS; `rsvg-convert` de librsvg, Inkscape o ImageMagick en Linux y macOS) y `build/MakeIcons.cs` (solo .NET) construye el `.ico` y el `.icns`. Si falta una herramienta, el script lo dice.
