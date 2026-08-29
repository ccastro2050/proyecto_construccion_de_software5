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
| `docker-compose.yml` | ★ servicio `mariadb` (11, :13346, se siembra solo) + `ConnectionStrings__MariaDb` |
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
- Puertos publicados: 8046 (API) · 15446 (postgres) · 11446 (sqlserver)
  · **13346 (mariadb)**. Reconstrucción del estudiante: +100 (13446).
- El interruptor: `MOTOR_BD` ∈ {postgres, sqlserver, mariadb}.

## 5. Chequeo de constitución

> **La compuerta 2** del método (ver [SDD_SPECKIT](../../../SDD_SPECKIT.md)):
> antes de pasar a `8_tasks.md` se revisa la
> [constitución](../../1_constitution.md) **artículo por artículo**. Si algo
> no cumple, o se corrige el plan, o se enmienda la constitución. Nunca se
> deja pasar "por esta vez".

| Artículo | Cómo lo cumple esta versión |
|---|---|
| **1** — El curso es POR VERSIONES y la especificación manda | El alcance de esta versión es el que declara [2_spec.md](2_spec.md) §2, y **no anticipa** nada de las siguientes. Cierra con commit y tag. |
| **2** — Stack: C# y ASP.NET Core, con el SQL a la vista | C# sobre ASP.NET Core, SQL escrito a mano y **siempre parametrizado**, sin ORM de entidades. Los paquetes son los que el artículo permite (§1 de este plan). |
| **3** — Arquitectura en capas con interfaces, desde el día 1 | Controlador → interfaz de servicio → interfaz de repositorio → repositorio (§3 de este plan). Solo el ensamblador conoce clases concretas. |
| **4** — Un solo comando | `docker compose up -d --build` deja la versión funcionando (§5 de este plan). |
| **5** — La base de datos viene DADA | La BD `bdfacturas` viene dada por los scripts de `db/`; esta versión solo nombra las tablas que su alcance le permite ([5_data_model.md](5_data_model.md)). |
| **6** — Todo en español, comentado para principiantes | Nombres, rutas y mensajes en español, con comentarios línea a línea en el código. |
| **7** — Contratos exactos | [6_contracts.md](6_contracts.md) fija verbos, rutas, códigos y formatos exactos, incluidos los desenlaces de error. |
| **8** — Convenciones fijas | Puertos, rutas, sobre de respuesta y catálogo de errores, tal como los fija el artículo. |

**Complejidad justificada:** si esta versión se desvía de algún artículo,
la desviación va aquí, con la alternativa más simple que se descartó y por
qué no sirvió. Sin desviaciones anotadas, se entiende que no las hay.
