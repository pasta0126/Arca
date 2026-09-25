# Guía para la dirección del centro: contraseña y clave de recuperación

Documento pensado para quien dirige el centro, no para quien usa ARCA cada día. Es la guía de la tarea 6.3 de `acces-i-xifrat`; su contenido esencial también aparece en las pantallas de ARCA, en catalán.

## Qué protege cada cosa

| Qué | Para qué sirve | Quién la conoce |
|-----|----------------|-----------------|
| **Contraseña del centro** | Abre los datos cada vez que se inicia ARCA. La comparten los conserges. | El equipo de conserjería |
| **Clave de recuperación** | Solo sirve si se olvida la contraseña. Son 26 caracteres en cinco grupos, por ejemplo `K7F2P-9XQ4M-ABCDE-FGHJK-MNPQRS`. | Solo la dirección, en papel |

Los datos de los alumnos están cifrados. Sin la contraseña o la clave de recuperación no hay manera de leerlos, ni en el ordenador ni en las copias de seguridad.

## Si se pierden las dos, los datos se pierden

**Nadie puede recuperarlos: ni el equipo que ha hecho ARCA, ni un técnico, ni el proveedor del ordenador.** Es deliberado: quien desarrolla ARCA es responsable del programa, no custodia los datos de ningún centro ni guarda copia de ninguna clave. Por eso ARCA obliga a guardar la clave de recuperación antes de crear los datos.

## Cómo guardar la clave de recuperación

1. Al crear los datos, ARCA muestra la clave **una sola vez**. Imprímela o cópiala y escríbela a mano si hace falta. Después de confirmarla no se puede volver a ver.
2. Guárdala en **papel**, en un **sobre cerrado**, **fuera del ordenador**.
3. Haz **dos copias** y guárdalas en **dos sitios distintos** del centro (por ejemplo, la caja fuerte de dirección y la de secretaría), no en la conserjería.
4. Anota en el sobre qué es y la fecha, sin escribir la contraseña.
5. No la envíes por correo ni la guardes en un fichero del mismo ordenador: quien tenga el fichero de datos y la clave lo puede abrir todo.
6. Si el papel que se ha impreso queda en la impresora, recógelo y destrúyelo si sobra.

## Si algún día se olvida la contraseña

En la pantalla de la contraseña, pulsar «He oblidat la contrasenya», escribir la clave de recuperación (se acepta en minúsculas, sin guiones o con espacios) y elegir una **contraseña nueva**. ARCA muestra entonces una **clave de recuperación nueva** que hay que guardar y confirmar como la primera vez; la anterior deja de valer y hay que destruir sus copias en papel.

## Cambiar la contraseña o la clave

Desde la sección «Seguretat de les dades»:

- **Cambiar la contraseña**: conviene hacerlo cuando alguien deja de trabajar en conserjería. Las copias de seguridad hechas antes conservan la contraseña que tenían.
- **Generar una clave de recuperación nueva**: cuando el sobre se ha perdido o abierto. Pide la contraseña actual. La clave anterior sigue valiendo hasta que se confirma la nueva.

## Buenas prácticas para la contraseña

- Mejor una frase de varias palabras sin relación (`riu cadira blau gos`) que una palabra con símbolos. ARCA exige 12 caracteres como mínimo y rechaza las contraseñas muy habituales.
- No la apuntes en un papel pegado al monitor: quien lo vea podrá abrir los datos.
- Cámbiala si sospechas que la conoce alguien que no debería.

## Copias de seguridad

Cada copia de seguridad lleva dentro su propio fichero de claves: se abre con la contraseña o la clave de recuperación que tenía **cuando se hizo**. Al restaurar una copia, la contraseña del centro pasa a ser la de esa copia (ARCA lo avisa antes de confirmar y guarda los datos anteriores).
