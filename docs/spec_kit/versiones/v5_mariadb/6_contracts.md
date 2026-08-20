# Contratos — Versión 5: CERO endpoints nuevos (tercera vez, misma gracia)

> Los 51 endpoints de [v1](../v1_producto_postgres/6_contracts.md),
> [v2](../v2_persona_factura/6_contracts.md) y
> [v3](../v3_resto_entidades/6_contracts.md) siguen vigentes **tal cual,
> con LOS TRES motores**. La única línea que cambia:

```
GET /
→ 200 { "mensaje": "API Facturas funcionando", "version": "v5",
        "motor": "postgres" | "sqlserver" | "mariadb",
        "contratos": "docs/spec_kit/versiones/v5_mariadb/6_contracts.md" }
```

## Lo que el criterio 3 verifica, motor por motor

| Grupo | postgres | sqlserver | mariadb |
|---|---|---|---|
| producto (6) · persona (6) · moldes v3 (30) | idénticos | idénticos | idénticos |
| factura (4) | CALL/INOUT | StoredProcedure/OUTPUT | StoredProcedure/OUT (emulado) |
| usuario + verificar-contrasena (7) · puentes (10) | idénticos (mismo BCrypt) | idénticos | idénticos |
| 404 / 409 / 422 / 500 | idénticos | idénticos | idénticos |

**Matices honestos (los de siempre):** el `detalle` de los 500 redacta
según el motor, y la serialización de decimales puede variar en
decimales visibles — el VALOR es contrato, el formato del número no.
