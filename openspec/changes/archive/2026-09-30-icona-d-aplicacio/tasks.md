## 1. El icono y su generación

- [x] 1.1 Dibujar `assets/icon/arca.svg` (símbolo neutro de taquilla, sin texto, tonos del tema) y comprobar que se reconoce a 16 píxeles (D1)
- [x] 1.2 Script `build/icons.sh` que genera los PNG (16 a 512), el `.ico` y el `.icns` con herramientas gratuitas y avisa de las que falten (D2)
- [x] 1.3 Generar y versionar los derivados, con el resumen del maestro, y la comprobación de derivados al día en `build/test.sh` (D3)
- [x] 1.4 Dejar constancia de la procedencia y de las herramientas en `assets/icon/README.md` y en `docs/stack.md`

## 2. En la aplicación

- [x] 2.1 Incrustar el icono en `Arca.Desktop` y asignarlo a la ventana principal (D4)
- [x] 2.2 Asignarlo a todos los diálogos desde la base común de ventanas (formularios, confirmaciones, selectores)
- [x] 2.3 Pruebas: toda ventana que crea la aplicación lleva icono, el recurso está en el ensamblado y no se lee de la base de datos (D6)

## 3. En los paquetes

- [x] 3.1 `ApplicationIcon` con el `.ico` para el ejecutable de Windows (D5)
- [x] 3.2 `package.sh`: `.icns` y `CFBundleIconFile` en `ARCA.app`, y `arca.png` con `arca.desktop` de ejemplo en el paquete de Linux
- [x] 3.3 Comprobar cada paquete generado (contenido y declaración del icono)

## 4. Cierre

- [x] 4.1 Actualizar `docs/firma-de-codigo.md` (el icono no afecta a la firma), `docs/riesgos.md` (marca definitiva pendiente) y el glosario si aparece algún término
- [x] 4.2 `build/test.sh`, `openspec validate --all --strict` y archivar el cambio
