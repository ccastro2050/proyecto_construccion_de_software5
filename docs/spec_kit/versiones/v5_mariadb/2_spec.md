# Especificación — Versión 5: el tercer motor (MariaDB) y el compose completo

> **Versión 5** del desarrollo incremental ([mapa de versiones](../0_mapa_versiones.md)).
> Rige la constitución: [../../1_constitution.md](../../1_constitution.md).
> **Acumulativa:** contiene TODO lo de v1 a v4 — los 51 endpoints
> existentes no se tocan y sus contratos siguen vigentes tal cual.
>
> | Documento de esta versión | Contenido |
> |---|---|
> | **2_spec.md** (este) | QUÉ agrega la v5 y sus criterios de aceptación |
> | [3_plan.md](3_plan.md) | CÓMO: la clase y el case que cuesta el tercer motor |
> | [4_research.md](4_research.md) | Decisiones y alternativas *(lectura opcional)* |
> | [5_data_model.md](5_data_model.md) | La MISMA bdfacturas, ahora en dialecto MariaDB |
> | [6_contracts.md](6_contracts.md) | CERO endpoints nuevos — tercera vez, misma gracia |
> | [7_quickstart.md](7_quickstart.md) | La regresión TRIPLE: todo pasa en los tres motores |
> | [8_tasks.md](8_tasks.md) | Orden de construcción por fases verificables |
> | [GUIA_IA5.md](GUIA_IA5.md) | Construirla con IA, sobre su proyecto v4 |

---

## 1. Propósito de la v5

**Cobrar la factura de la fábrica — y comprobar Liskov a escala.** La v4
prometió que agregar un motor costaría "una clase y un case". La v5 lo
mide: entra **MariaDB** — tercer dialecto, tercer proveedor ADO.NET,
tercera forma de entregar los OUT de los SPs — y el diff del ensamblador
es exactamente `FabricaMariaDb.cs` + un case. Los tres repositorios de
cada entidad son **indistinguibles desde el servicio**: eso ES la
sustitución de Liskov, verificada con la MISMA batería tres veces.

Con esto el compose queda COMPLETO en infraestructura de datos: las 12
tablas de bdfacturas viven idénticas en LOS TRES motores — el terreno
que pisará el front (v6).

## 2. Alcance

**Incluye:** servicio `mariadb` en el compose (misma BD semilla, se
siembra solo) · los 11 repositorios en dialecto MySqlConnector ·
`FabricaMariaDb` + el tercer case del interruptor (`MOTOR_BD` acepta
`mariadb`) · diagnóstico pasa a `"version": "v5"` · la prueba de capas
crece con el tercer dialecto.

**No incluye (deliberado):**
- El front (v6): es la versión siguiente.
- Selección de motor por petición: sigue siendo UNA vez, al arrancar.
- Cambios de contrato: ninguno.

## 3. Requisitos funcionales

### RF1 — El tercer motor completo
- `mariadb` (MariaDB 11) en el compose, puerto publicado **13346**, con
  `db/bdfacturas_mariadb.sql`: las MISMAS 12 tablas y semillas (mismos
  ids, `AUTO_INCREMENT` alineado), los triggers de totales/stock y los
  SPs de factura — en dialecto MariaDB ([5_data_model](5_data_model.md)).
- MariaDB también ejecuta los scripts montados la primera vez (como
  PostgreSQL): sin contenedor inicializador — el curso ya tiene un motor
  de cada especie.
- Los 11 `RepositorioXMariaDb` (MySqlConnector): mismos contratos. La
  sorpresa didáctica: el SQL de los moldes es **idéntico al de
  PostgreSQL** (`LIMIT @limite`) — dialectos hay menos de los que uno
  teme.
- El de factura: `CommandType.StoredProcedure` + parámetro OUT (como
  SqlClient), y errores por **SIGNAL '45000'** → `MySqlException` con
  número **1644** + patrón del mensaje ("no existe" → 404 · "ya está
  anulada" → 409 · resto → 500).

### RF2 — La fábrica acepta el tercer valor
- `MOTOR_BD`: `postgres` (default) | `sqlserver` | `mariadb`.
- El costo queda a la vista en el diff: UNA clase + UN case.

### RF3 — Diagnóstico
`GET /` → `"version": "v5"` con el `motor` de siempre (ahora puede decir
`mariadb`).

## 4. Requisitos no funcionales

- **RNF1 — Los de v1 a v4 siguen todos.**
- **RNF2 — La frontera es el repositorio:** el diff NO toca
  Controllers/, Servicios/, Peticiones/, Modelos/ ni Excepciones/.
- **RNF3 — Paridad de semillas en LOS TRES motores:** el smoke test es
  EL MISMO, tres veces.
- **RNF4 — Sin anticipación:** nada del front (v6).

## 5. Criterios de aceptación

1. **Regresión total contra PostgreSQL** (default): smoke tests
   completos de v1+v2+v3 pasan tal cual (`"version":"v5"`).
2. **Regresión total contra SQL Server** (`MOTOR_BD=sqlserver`): idéntica.
3. **Regresión total contra MariaDB** (`MOTOR_BD=mariadb`): idéntica —
   mismos ids, mismos stocks, mismos 404/409/422/500. La TRIPLE
   regresión sin recompilar es el criterio estrella.
4. **El diff respeta la frontera y la cuenta de la fábrica:** `git diff
   v4 --stat` solo toca `Repositorios/*MariaDb.cs`,
   `Fabricas/FabricaMariaDb.cs`, `Program.cs` (un case),
   `ApiFacturas.csproj`, `docker-compose.yml`, `appsettings.json`,
   `db/`, `pruebas/`, `postman/` y `docs/`.
5. **Prueba de capas ampliada:** la fábrica entrega el dialecto MariaDb
   sin abrir conexiones.

## 6. Definición de TERMINADA

Los 5 criterios pasan → commit + tag `v5` → la API es tri-motor y la
infraestructura de datos está completa → recién entonces se especifica
la v6 (el front Flask + Jinja2 sobre la API tri-motor).

## 7. Clarificaciones

> **Qué es esta sección:** el registro de las ambigüedades detectadas ANTES
> de planear, con la respuesta que se acordó y su razón. Es **la compuerta
> 1** del método (ver [SDD_SPECKIT](../../../SDD_SPECKIT.md)): mientras
> quede un `[NECESITA ACLARACIÓN: …]` en los requisitos de arriba, esta
> versión no pasa a la planeación.
>
> Las entradas de abajo se reconstruyeron **al cerrar la versión**, a
> partir de las decisiones que sus propios contratos ya dejaban fijadas.
> De aquí en adelante esta sección se llena **en vivo**, antes del
> `3_plan.md` — que es como debe ser.

| # | La pregunta | La respuesta acordada, con su razón | Dónde quedó |
|---|---|---|---|
| C1 | Anular dos veces la misma factura, ¿qué responde? | **409**: el conflicto es de estado, no de forma ni de existencia. La factura existe (no es 404) y el body está bien (no es 422). | Contrato de anular |
| C2 | La contraseña, ¿viaja y se guarda en claro? | Nunca: se guarda **hasheada con BCrypt** y la comparación se hace con un endpoint de verificación. La API jamás devuelve el hash. | RF de usuario · modelo de datos |

**Cómo se escribe una entrada nueva:** la pregunta tal como se hizo (no
"revisar el borrado", sino "¿físico o lógico?"), la respuesta **con su
razón**, y el documento donde quedó plasmada. Si la respuesta cambia un
requisito, se corrige el requisito allá arriba: esta sección lo registra,
no lo reemplaza.
