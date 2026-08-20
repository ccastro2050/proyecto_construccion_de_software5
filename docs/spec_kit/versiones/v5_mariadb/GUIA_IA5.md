# Cómo construir la VERSIÓN 5 con IA — sobre su proyecto de la v4

> Guía de la **v5** (acumulativa: se construye encima de su proyecto con
> v1 a v4 terminadas). El método general es el de la
> [guía de la v1](../v1_producto_postgres/GUIA_IA1.md); aquí está SOLO
> lo propio de la v5.

---

## 0. Punto de partida

Su proyecto con la **v4 funcionando** (la regresión doble pasa con sus
puertos +100). La v5 repite el ejercicio de la v4 con el TERCER motor —
si la v4 le quedó bien, esta es la versión más corta del curso: esa
brevedad ES la lección (la fábrica ya pagó la infraestructura).

**Sus puertos:** API 8142 · PostgreSQL 15542 · SQL Server 11542 ·
**MariaDB 13442**.

## A.1 Qué subirle al chat

`1_constitution.md` + los 7 documentos de `v5_mariadb/` + DOS archivos
suyos: `Program.cs` (le agregará un case) y un repositorio Postgres
cualquiera (el molde del calco).

## A.2 Prepare su proyecto (comandos PowerShell)

1. Specs y BD (del clon del curso; ajuste la primera ruta):

   ```powershell
   mkdir docs\spec_kit\versiones\v5_mariadb
   Copy-Item ..\proyecto_construccion_de_software5\docs\spec_kit\versiones\v5_mariadb\* docs\spec_kit\versiones\v5_mariadb\
   Copy-Item ..\proyecto_construccion_de_software5\db\bdfacturas_mariadb.sql db\
   ```

2. Los ARCHIVOS VACÍOS nuevos (12):

   ```powershell
   New-Item api_facturas\Fabricas\FabricaMariaDb.cs, api_facturas\Repositorios\RepositorioProductoMariaDb.cs, api_facturas\Repositorios\RepositorioPersonaMariaDb.cs, api_facturas\Repositorios\RepositorioFacturaMariaDb.cs, api_facturas\Repositorios\RepositorioEmpresaMariaDb.cs, api_facturas\Repositorios\RepositorioClienteMariaDb.cs, api_facturas\Repositorios\RepositorioVendedorMariaDb.cs, api_facturas\Repositorios\RepositorioUsuarioMariaDb.cs, api_facturas\Repositorios\RepositorioRolMariaDb.cs, api_facturas\Repositorios\RepositorioRutaMariaDb.cs, api_facturas\Repositorios\RepositorioRolUsuarioMariaDb.cs, api_facturas\Repositorios\RepositorioRutaRolMariaDb.cs
   ```

3. Los que CRECEN: `Program.cs` (UN case), `ApiFacturas.csproj`
   (MySqlConnector), `docker-compose.yml` (servicio mariadb + cadena),
   `appsettings.json` (cadena MariaDb con `AllowUserVariables=True`) y
   `pruebas/Programa.cs`.

## A.3 El prompt (los cambios sobre el de la v4)

- CONTEXTO CLAVE: *"Mi proyecto YA ES bi-motor (v4 con fábrica); la v5
  agrega MariaDB: UNA clase FabricaMariaDb + UN case + 11 repositorios
  MariaDb calcados de los Postgres (MySqlConnector; el SQL de los moldes
  NO cambia — MariaDB también usa LIMIT). El de factura usa
  CommandType.StoredProcedure con OUT (MySqlDbType.LongText) y traduce
  por número 1644 + patrón. La cadena lleva AllowUserVariables=True.
  NO toques Controllers/, Servicios/, Peticiones/, Modelos/,
  Excepciones/ ni IFabricaRepositorios."*
- Puertos +100: *"MariaDB publica en 13442 (no 13342)"*.

## A.4 La alarma de siempre

Si la IA toca algo por encima de los repositorios, o propone un ORM para
"no repetir": recháselo — el criterio 4 exige la frontera limpia, y el
calco ES la lección.

## Cierre

La regresión TRIPLE ([7_quickstart.md](7_quickstart.md) §2, con sus
puertos +100) → tag `v5`.
