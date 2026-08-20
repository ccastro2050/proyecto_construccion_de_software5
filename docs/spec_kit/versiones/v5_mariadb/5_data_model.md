# Modelo de datos — Versión 5: la MISMA bdfacturas, en MariaDB

> Tercera y última copia del dato: `db/bdfacturas_mariadb.sql` crea en
> MariaDB la misma base que ya vive en PostgreSQL y SQL Server — 12
> tablas, triggers, SPs de factura y semillas idénticas. La BD se llama
> `bdfacturas_mariadb_local`. El script es idéntico al de los cursos
> gemelos (PHP y Python): misma BD, otras APIs.

---

## 1. Equivalencias de dialecto (la tercera columna de la tabla mental)

| Concepto | PostgreSQL | SQL Server | MariaDB |
|---|---|---|---|
| Autonumérico | `SERIAL` | `INT IDENTITY` | `INT AUTO_INCREMENT` |
| Ids explícitos en semillas | `setval()` | `IDENTITY_INSERT` | `ALTER TABLE … AUTO_INCREMENT = n` |
| Error de negocio | `RAISE EXCEPTION` (P0001) | `THROW 5000x` (numerado) | `SIGNAL SQLSTATE '45000'` (código 1644) |
| SP con salida | `INOUT` (fila del CALL) | `OUTPUT` de SqlClient | `OUT` (el conector lo emula con `@variables`) |
| Abrir JSON de entrada | `json_array_elements` | `OPENJSON` | `JSON_EXTRACT` + `WHILE` |
| Armar JSON de salida | `json_build_object/agg` | `FOR JSON PATH` | `JSON_OBJECT` + `GROUP_CONCAT` |
| Top-N | `LIMIT @n` | `TOP (@n)` | **`LIMIT @n`** (igual que PostgreSQL) |
| El trigger | UNA función para I/U/D | 3 triggers | **6 triggers** (BEFORE/AFTER × I/U/D — MariaDB no permite OR) |
| Tipo JSON | tipo real | NVARCHAR | **alias de LONGTEXT** |
| Auto-ejecuta scripts montados | sí | NO (init) | **sí** |

Las 12 tablas, PKs, FKs, `UNIQUE(ruta)`, defaults y el `ON DELETE
CASCADE` — **idénticos** en estructura y nombre.

## 2. Los mismos actores, tercer acento

- **Triggers** `trg_prodfact_*`: mismos papeles (validar stock, calcular
  subtotal, mover stock, recalcular total), repartidos en 6 por las
  reglas de MariaDB.
- **SPs de factura**: mismos nombres y semántica; los mensajes que la
  API traduce son LOS MISMOS textos de los otros motores («Factura N no
  existe», «Factura N ya está anulada», «Stock insuficiente…») — por eso
  los patrones del repositorio son uniformes.
- Los SPs de usuarios/roles/permisos también viajan (paridad con los
  gemelos — terreno de la v6).
- Detalle del script: las facturas semilla se insertan con los triggers
  temporalmente eliminados (MariaDB no tiene `DISABLE TRIGGER`) y se
  recrean idénticos después.

## 3. Semillas (idénticas — RNF3, por tercera vez)

| Tabla | Filas | Los números de la regresión |
|---|---|---|
| producto | 8 | PR001 stock 17 · PR003 stock 42 |
| persona · empresa | 6 · 3 | P001 Ana Torres · E001/E002/E999 |
| cliente · vendedor | 4 (ids 1,2,3,5) · 3 | AUTO_INCREMENT queda en 6 y 4 |
| factura | 6 (+12 renglones) | AUTO_INCREMENT queda en 7 |
| rol · ruta · usuario · puentes | 5 · 15 · 8 · 21+25 | mismas filas |
