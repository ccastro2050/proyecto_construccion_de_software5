# Research — Versión 5: decisiones y alternativas

> Lectura opcional: el PORQUÉ de cada decisión del [plan](3_plan.md).

---

## D1 — MySqlConnector (y no MySql.Data)

Dos proveedores ADO.NET compiten para MySQL/MariaDB: el histórico
`MySql.Data` (Oracle) y **`MySqlConnector`** (MIT, async de verdad,
mantenido por la comunidad). **Decisión: MySqlConnector** — es el
recomendado por la documentación de .NET moderno, su async no es
"fingido" (MySql.Data bloquea por debajo) y su licencia es limpia para
un curso.

## D2 — OUT con `CommandType.StoredProcedure` (y AllowUserVariables)

**Alternativas:** (a) `CALL sp(…, @salida)` + `SELECT @salida` a mano
(dos comandos, como hace el gemelo Python) · (b) parámetro
`ParameterDirection.Output` y que el conector haga el trabajo.

**Decisión: (b)** — es el idioma de ADO.NET que el curso ya habla desde
SqlClient. El matiz honesto: MySqlConnector implementa (b) HACIENDO (a)
por debajo (variables de sesión) — por eso la cadena de conexión lleva
`AllowUserVariables=True`. La abstracción es azúcar, no magia; el plan
§3 lo deja dicho.

## D3 — Traducción por 1644 + patrón (el punto medio)

El curso ya vio dos extremos: SQL Server numera cada THROW (filtro
preciso por número) y PostgreSQL no numera nada (filtro por patrón).
MariaDB queda en el medio: TODOS los `SIGNAL '45000'` llegan con el
código genérico **1644** (ER_SIGNAL_EXCEPTION), así que el número filtra
"es un error de negocio" y el patrón decide cuál. Tres motores, tres
señales, UNA frontera que las normaliza — la tabla comparativa del plan
§3 es de las páginas más valiosas del curso.

## D4 — Sin inicializador (y la colección completa de especies)

MariaDB ejecuta `/docker-entrypoint-initdb.d/` la primera vez, como
PostgreSQL. El compose cierra con un ejemplar de cada especie: dos
motores que se siembran solos y uno (SQL Server) con contenedor
inicializador. Ver los tres conviviendo en UN archivo ES la lección de
orquestación del curso.

## D5 — Puerto 13346

La familia de este curso termina en 42: API 8046, PostgreSQL 15446,
SQL Server 11446, **MariaDB 13346** (libre entre los 133xx/134xx de los
otros cursos). Reconstrucción del estudiante: 13446.

## D6 — Semillas idénticas, tercera copia

`db/bdfacturas_mariadb.sql` viene del ecosistema de cursos gemelos
(misma BD en PHP y Python): mismos ids (`ALTER TABLE … AUTO_INCREMENT`
es el `setval`/`IDENTITY_INSERT` de MariaDB), mismos stocks, mismos
hashes de usuario. El smoke test corre IGUAL en los tres motores — y los
AUTO_INCREMENT de InnoDB también se consumen en inserts fallidos, así
que hasta la nota del quickstart v3 aplica idéntica.
