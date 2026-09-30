## 1. Versión en la cabecera

- [ ] 1.1 Mostrar el número de versión en pequeño junto al título en `HeaderView`, con la descripción emergente y claves i18n en catalán (D1, D2)
- [ ] 1.2 Recortar el sufijo `+hash` al leer la versión en `AppInfo`, conservar `-dev` y devolver `?` con registro si no se puede leer (D4, D5)
- [ ] 1.3 Que a ventana estrecha se acorte el nombre y nunca la versión (D3)
- [ ] 1.4 Pruebas: versión visible en cualquier sección, con y sin identidad del centro, a tamaño mínimo de ventana, con `-dev` y sin él, y fallo de lectura

## 2. Versión en Ajustes y fuente única

- [ ] 2.1 Hacer seleccionable y copiable la versión de Ajustes y comprobar que es el mismo valor que el de la cabecera (D6)
- [ ] 2.2 Prueba de que no hay ningún número de versión escrito a mano en el código, los textos ni los scripts salvo `Directory.Build.props`
- [ ] 2.3 Comprobar que el nombre del paquete coincide con la versión que muestra la aplicación

## 3. Cierre

- [ ] 3.1 Documentar dónde se cambia la versión en `docs/` y `openspec validate --all --strict`; archivar el cambio
