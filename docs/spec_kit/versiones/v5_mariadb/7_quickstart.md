# Quickstart — Versión 5: arranque y la regresión TRIPLE

> **Versión 5** · Validación rápida de la versión ya construida. Si aún no
> hay nada construido, empiece por [8_tasks.md](8_tasks.md).

---

## 1. Arrancar TODO (ahora con tres motores)

```powershell
docker compose up -d --build
```

Al final: `postgres` (healthy), `sqlserver` (healthy), `sqlserver-init`
(Exited 0), **`mariadb` (healthy — también se siembra solo)** y
`api-facturas` arriba.

> ⚠️ SQL Server sigue pidiendo ~2 GB de RAM; MariaDB suma ~200 MB.
> En máquinas justas: `docker compose stop sqlserver sqlserver-init`
> mientras trabaja con los otros dos.

## 2. La regresión TRIPLE (criterios 1-3 — el corazón de la v5)

```powershell
curl.exe http://localhost:8042/     # → "version":"v5", "motor":"postgres"
# → smoke tests COMPLETOS de v1 §2, v2 §3 y v3 §3: pasan tal cual

$env:MOTOR_BD = "sqlserver"
docker compose up -d api-facturas
curl.exe http://localhost:8042/     # → "motor":"sqlserver"
# → la MISMA regresión completa: pasa igual
# (para el estado semilla exacto entre motores: docker compose down -v && up -d)

$env:MOTOR_BD = "mariadb"
docker compose up -d api-facturas
curl.exe http://localhost:8042/     # → "motor":"mariadb"
# → la MISMA regresión completa, tercera vez. Ni una línea de código
#   cambió entre las tres pasadas: ESO es Liskov entre repositorios.

Remove-Item Env:MOTOR_BD            # volver al default (postgres)
docker compose up -d api-facturas
```

## 3. Los errores de negocio en el motor nuevo (criterio 3)

```powershell
curl.exe -i http://localhost:8042/api/factura/999                 # → 404 (SIGNAL 1644 traducido)
curl.exe -i -X POST http://localhost:8042/api/factura -H "Content-Type: application/json" -d "{\"fkidcliente\":1,\"fkidvendedor\":1,\"productos\":[{\"codigo\":\"PR001\",\"cantidad\":9999}]}"   # → 500 "Stock insuficiente…"
# (anule dos veces cualquier factura suya: la segunda → 409)
```

## 4. La frontera del diff (criterio 4)

```powershell
git diff v4 --stat
```

NADA de `Controllers/`, `Servicios/`, `Peticiones/`, `Modelos/` ni
`Excepciones/`. Y en `Program.cs` el diff es UN case — la cuenta de la
fábrica, pagada por segunda vez.

## 5. La prueba de capas (criterio 5)

```powershell
docker compose exec api-facturas dotnet run --project pruebas
# → … CRITERIO 5 OK: cada fábrica entrega los repositorios de su motor, sin abrir conexiones
```

## 6. Si algo falla

| Síntoma | Causa probable |
|---|---|
| Los de v1/v2/v3/v4 | Aplican todos igual (sus quickstarts) |
| `mariadb` no queda healthy | Puerto 13342 ocupado, o volumen de un intento fallido: `docker compose down -v` y de nuevo |
| Todo 500 con `motor=mariadb` | ¿El script corrió? Solo se auto-ejecuta con el volumen VACÍO — `docker compose down -v && up -d` |
| "Parameter '@salida'…" o error de variables | La cadena de MariaDB perdió `AllowUserVariables=True` — [3_plan.md](3_plan.md) §3 |
| Factura da 500 en vez de 404/409 en mariadb | El repositorio no está filtrando 1644 + patrón — [3_plan.md](3_plan.md) §3 |
| "Motor desconocido: …" | Valor inválido en `MOTOR_BD` (postgres · sqlserver · mariadb) |
