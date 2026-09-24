# Resultados del spike técnico

Punto 1 de `docs/preparacion-desarrollo.md`. El código es desechable y vive en la rama `spike/tecnico` (no se fusiona), carpeta `spike/`. Aquí solo queda lo aprendido. Decisión D2: rama descartable.

## Bloque 1: base de datos, copia, bloqueo, firma y texto (sin interfaz)

Ejecutado el **24 de septiembre de 2026** en **macOS 27 arm64, .NET 10.0.10**. Comando: `dotnet run` en `spike/Db`. **Linux: descartado por ahora. Windows: solo antes de salir a producción** (decisión de 2026-09-24, ver `docs/stack.md`).

| Supuesto | Resultado en macOS arm64 | Detalle |
|----------|--------------------------|---------|
| EF Core + SQLite3 Multiple Ciphers en formato SQLCipher 4 | **Correcto** | `Microsoft.EntityFrameworkCore.Sqlite.Core` + `SQLite3MC.PCLRaw.bundle` 2.4.0. Se crea, escribe y reabre. Con clave incorrecta o sin clave falla. Cabecera del fichero sin texto en claro. |
| `PRAGMA integrity_check` | **Correcto** | Devuelve `ok` con `cipher=sqlcipher` y `legacy=4`. |
| Migración de esquema en base cifrada | **Correcto** | `ALTER TABLE` y `user_version` dentro de una transacción. |
| Copia en línea con la misma clave | **Correcto** | `SqliteConnection.BackupDatabase` sobre una base cifrada, con un hilo insertando: la copia pasa `integrity_check` y se abre con la clave. |
| Bloqueo de instancia única | **Correcto** | `FileStream` con `FileShare.None` sobre un fichero de bloqueo: un segundo proceso es rechazado y, al liberarlo, puede obtenerlo. |
| Ed25519 con NSec | **Correcto** | Verifica una firma externa (vector de la RFC 8032) y rechaza una alterada. |
| Argon2id + XChaCha20-Poly1305 (`acces-i-xifrat`) | **Correcto** | Envolver y desenvolver la llave; texto alterado rechazado. Argon2id con 64 MiB y 3 pasadas tarda unos 100 ms en este equipo (**calibrar en equipos de gama baja**: objetivo menos de 2 s). |
| Texto catalán | **Correcto** | Orden `ca-ES` con ICU: Cabanes, Çaragol, Colomer, Ll, L·l, Llop…; «García» = «garcia» y «Núria» = «NURIA» sin acentos ni mayúsculas. |

### Cómo se configura el cifrado (para `arquitectura-base`)

Al abrir cada conexión, antes de cualquier otra sentencia, y con la llave de 256 bits en hexadecimal directo (sin derivación en la base):

```
PRAGMA cipher='sqlcipher';
PRAGMA legacy=4;
PRAGMA key="x'<64 hex>'";
```

En EF Core se hace con un interceptor de conexión (`ConnectionOpened`) y `Pooling=False`. Sin la selección de `cipher`, SQLite3MC usa su cifrado por defecto (ChaCha20) y **no** es compatible con SQLCipher.

### Binarios nativos por sistema

Comprobado en el contenido de los paquetes NuGet (no ejecutado):

| Paquete | `win-x64` | `linux-x64` | `linux-arm64` | `osx-x64` | `osx-arm64` |
|---------|:---------:|:-----------:|:-------------:|:---------:|:-----------:|
| `SQLite3MC.PCLRaw.bundle` (`sqlite3mc`) | sí | sí | sí | sí | sí |
| `NSec.Cryptography` (`libsodium`) | sí | sí | sí | sí | sí |

## Pendiente de verificar

- **Linux**: descartado por ahora.
- **Windows**: repetir el bloque 1 antes de producción, en especial el bloqueo de instancia única y las rutas.
- **Bloque 2 (interfaz, Avalonia 12)**: arrastrar y soltar con resalte y Escape, lista virtualizada con 300 taquillas y actualización de una sola, renderizado sin ventana con prueba de enlace y foco, y tabla con orden y selección múltiple sin componentes de pago (T2 y T4).
- Verificar el formato con una **herramienta SQLCipher externa** (por ejemplo, abrir la base con el intérprete `sqlcipher`): aquí solo se comprueba con el propio SQLite3MC.
- Calibrar Argon2id en un equipo de gama baja.
