# Tareas — Versión 5: orden de construcción por fases verificables

> Cada fase termina en un estado COMPROBABLE. No avance con una fase en
> rojo. El detalle de diseño está en [3_plan.md](3_plan.md).

---

## Fase 0 — Punto de partida

- [ ] La v4 corre y pasa su regresión doble (tag `v4` presente).

**Verificar:** `curl http://localhost:8042/` → `"version":"v4"`.

## Fase 1 — El motor nuevo en el compose (sin tocar la API)

- [ ] `db/bdfacturas_mariadb.sql` (cópielo del proyecto del curso — es
      dato: mismas semillas o la regresión no será comparable).
- [ ] `docker-compose.yml`: servicio `mariadb` (11, :13342, volumen
      `mariadbdata`, script montado en `/docker-entrypoint-initdb.d/`,
      healthcheck).
- [ ] `docker compose up -d` — la API sigue en v4: nada se rompe.

**Verificar:**
```powershell
docker compose exec mariadb mariadb -uroot -pConstruccion123! bdfacturas_mariadb_local -e "SHOW TABLES; SELECT COUNT(*) FROM producto;"   # 12 tablas · 8
```

## Fase 2 — Los repositorios MariaDb

- [ ] **MySqlConnector** en el csproj (+ recrear el contenedor).
- [ ] Los 10 calcados de los Postgres: solo cambian las clases del
      proveedor — el SQL es idéntico ([plan §2](3_plan.md)).
- [ ] `RepositorioFacturaMariaDb`: StoredProcedure + OUT (LongText) +
      traducción 1644 + patrón ([plan §3](3_plan.md)).

**Verificar:** compila (`docker compose logs api-facturas` sin errores).

## Fase 3 — La clase y el case

- [ ] `Fabricas/FabricaMariaDb.cs` (la interfaz NO cambia).
- [ ] `Program.cs`: el case `"mariadb"` + cadena `MariaDb`.
- [ ] `appsettings.json` y compose: cadena con `AllowUserVariables=True`.
- [ ] `pruebas/Programa.cs`: el tercer dialecto en la prueba de la
      fábrica.

**Verificar:** `GET /` → `"version":"v5"` · pruebas en verde · con
`$env:MOTOR_BD="mariadb"` y recrear la API → `"motor":"mariadb"`.

## Fase 4 — Verificación total y cierre

- [ ] **Regresión TRIPLE** ([7_quickstart.md](7_quickstart.md) §2):
      v1+v2+v3 contra postgres → sqlserver → mariadb, sin recompilar.
- [ ] `git diff v4 --stat` respeta la frontera (criterio 4).
- [ ] Postman: nota de la v5 (tercer motor) · mapa y README · commit +
      tag `v5` + push.

**Verificar:** los 5 criterios de [2_spec.md](2_spec.md) §5 en verde.
