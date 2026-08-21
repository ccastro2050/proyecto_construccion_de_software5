# Plan — Versión 5: el tercer motor (MariaDB)

> **Nota (agosto de 2026):** el curso adoptó **Dapper** como
> micro-ejecutor en TODOS los repositorios: el SQL sigue escrito a mano
> y parametrizado; cambió el mapeo (`QueryAsync`/`ExecuteAsync` en vez
> del ciclo DataReader) y los SPs se llaman con `DynamicParameters`.
> Las tablas de "calco" entre dialectos siguen valiendo para los
> PROVEEDORES (Npgsql/SqlClient/MySqlConnector) que Dapper usa por debajo.


> Cómo se construye lo especificado en [2_spec.md](2_spec.md).

---

## 1. Inventario de archivos

**Nuevos (12 de código + 1 de BD):**

```
api_facturas/Fabricas/FabricaMariaDb.cs            ← LA clase que cuesta el motor
api_facturas/Repositorios/Repositorio{Producto,Persona,Factura,Empresa,
    Cliente,Vendedor,Usuario,Rol,Ruta,RolUsuario,RutaRol}MariaDb.cs   (11)
db/bdfacturas_mariadb.sql                          ← la MISMA BD, dialecto MariaDB
```

**Crecen:**

| Archivo | Qué crece |
|---|---|
| `ApiFacturas.csproj` | ★ paquete **MySqlConnector** |
| `docker-compose.yml` | ★ servicio `mariadb` (11, :13342, se siembra solo) + `ConnectionStrings__MariaDb` |
| `appsettings.json` | ★ cadena `MariaDb` (con `AllowUserVariables=True` — ver §3) |
| `Program.cs` | ★ UN case en el switch de fábricas — la cuenta, pagada |
| `pruebas/Programa.cs` | ★ el tercer dialecto en la prueba de la fábrica |

**Intocables (RNF2):** Controllers/, Servicios/, Peticiones/, Modelos/,
Excepciones/ — e incluso `IFabricaRepositorios` (la interfaz ya
prometía la familia completa; solo llega otra implementación).

## 2. Los 10 moldes: la sorpresa del dialecto

| PostgreSQL (v1–v3) | MariaDB (v5) |
|---|---|
| `using Npgsql` | `using MySqlConnector` |
| `NpgsqlConnection` / `Command` / `DataReader` | `MySqlConnection` / `Command` / `DataReader` |
| `SELECT … LIMIT @limite` | **idéntico** — MariaDB habla el mismo LIMIT |
| Todo lo demás | **idéntico** |

El calco MariaDb sale de los repositorios Postgres cambiando SOLO las
clases del proveedor: ni una línea de SQL se toca. (SQL Server fue el
distinto con su `TOP` — tres motores, dos dialectos de Top-N.)

## 3. El repositorio de factura (tercer mecanismo de OUT)

| Motor | Cómo devuelve el SP su JSON | Cómo avisa el error de negocio |
|---|---|---|
| PostgreSQL | `INOUT` — el CALL lo devuelve como fila | `RAISE EXCEPTION` → P0001 + patrón |
| SQL Server | parámetro `OUTPUT` de SqlClient | `THROW` numerado (50003/50010) |
| MariaDB | `CommandType.StoredProcedure` + `ParameterDirection.Output` (`MySqlDbType.LongText` — el JSON de MariaDB ES LONGTEXT) | `SIGNAL '45000'` → **número 1644** + patrón |

Matiz de conexión: MySqlConnector emula los parámetros OUT con
variables de sesión — la cadena lleva **`AllowUserVariables=True`** o el
CALL falla. (Es el mismo mecanismo `@salida` que el gemelo Python usa a
mano; aquí el conector lo esconde.)

La traducción — código 1644 (ER_SIGNAL_EXCEPTION) para TODOS los SIGNAL,
así que el patrón del mensaje decide (el punto medio entre el THROW
numerado y el P0001):

```csharp
catch (MySqlException e) when (e.Number == 1644 && e.Message.Contains("no existe"))
    { throw new NoEncontradoExcepcion(e.Message); }      // → 404
catch (MySqlException e) when (e.Number == 1644 && e.Message.Contains("anulada"))
    { throw new ConflictoExcepcion(e.Message); }         // → 409
```

## 4. El compose queda completo

- `mariadb:11` se siembra solo (como PostgreSQL): el curso cierra con un
  motor de cada especie — dos que se auto-inicializan y uno con
  contenedor inicializador.
- Puertos publicados: 8042 (API) · 15442 (postgres) · 11442 (sqlserver)
  · **13342 (mariadb)**. Reconstrucción del estudiante: +100 (13442).
- El interruptor: `MOTOR_BD` ∈ {postgres, sqlserver, mariadb}.
