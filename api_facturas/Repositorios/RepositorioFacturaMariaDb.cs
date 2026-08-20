// ============================================================
// RepositorioFacturaMariaDb — la capa de DATOS de factura (v5).
//
// El mismo papel traductor de los gemelos Postgres/SqlServer,
// tercer dialecto:
//
//   1. Los SPs de MariaDB devuelven su JSON por un parámetro OUT.
//      MySqlConnector lo maneja como en SQL Server: CommandType.
//      StoredProcedure + ParameterDirection.Output (por debajo, el
//      conector usa variables de sesión — por eso la cadena lleva
//      AllowUserVariables=True).
//   2. Los errores de negocio son SIGNAL SQLSTATE '45000': llegan
//      como MySqlException con Number == 1644 (ER_SIGNAL_EXCEPTION)
//      — sin número propio por error, así que el filtro es código
//      1644 + patrón del mensaje (el punto medio entre el THROW
//      numerado de SQL Server y el P0001 de PostgreSQL).
// ============================================================

using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApiFacturas.Excepciones;
using ApiFacturas.Modelos;
using MySqlConnector;

namespace ApiFacturas.Repositorios;

public class RepositorioFacturaMariaDb : IRepositorioFactura
{
    private readonly string _cadenaConexion;

    private static readonly JsonSerializerOptions _opcionesJson = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public RepositorioFacturaMariaDb(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    // ------------------------------------------------------------
    // El ayudante central: ejecutar un SP y devolver su JSON
    // ------------------------------------------------------------

    private async Task<string> EjecutarSpAsync(string nombreSp, Action<MySqlParameterCollection> configurar)
    {
        await using var conexion = new MySqlConnection(_cadenaConexion);
        await using var comando = new MySqlCommand(nombreSp, conexion)
        {
            CommandType = CommandType.StoredProcedure,
        };
        configurar(comando.Parameters);

        // El parámetro de SALIDA donde el SP deja su JSON (LONGTEXT):
        var salida = new MySqlParameter("@p_resultado", MySqlDbType.LongText)
        {
            Direction = ParameterDirection.Output,
        };
        comando.Parameters.Add(salida);

        try
        {
            await conexion.OpenAsync();
            await comando.ExecuteNonQueryAsync();
        }
        // 1644 = ER_SIGNAL_EXCEPTION (los SIGNAL '45000' de SPs y
        // triggers). El patrón del mensaje decide cuál excepción:
        catch (MySqlException e) when (e.Number == 1644 && e.Message.Contains("no existe"))
        {
            throw new NoEncontradoExcepcion(e.Message);      // → 404
        }
        catch (MySqlException e) when (e.Number == 1644 && e.Message.Contains("anulada"))
        {
            throw new ConflictoExcepcion(e.Message);         // → 409
        }
        // Lo demás (stock insuficiente del trigger, FK, mínimo de
        // renglones) sube tal cual → 500.

        return (string?)salida.Value ?? "null";
    }

    // El mismo sobre {"factura":{…},"productos":[…]} de los otros motores:
    private class RespuestaFacturaSp
    {
        [JsonPropertyName("factura")]
        public Factura? Factura { get; set; }

        [JsonPropertyName("productos")]
        public List<ProductoDeFactura>? Productos { get; set; }
    }

    private static Factura ArmarFactura(string json)
    {
        var respuesta = JsonSerializer.Deserialize<RespuestaFacturaSp>(json, _opcionesJson)!;
        var factura = respuesta.Factura!;
        factura.Productos = respuesta.Productos ?? new List<ProductoDeFactura>();
        return factura;
    }

    // ------------------------------------------------------------
    // Los 4 métodos del contrato (mismos SPs, mismo JSON)
    // ------------------------------------------------------------

    public async Task<List<Factura>> ListarAsync()
    {
        var json = await EjecutarSpAsync("sp_listar_facturas_y_productosporfactura", _ => { });
        return JsonSerializer.Deserialize<List<Factura>>(json, _opcionesJson) ?? new List<Factura>();
    }

    public async Task<Factura> ConsultarAsync(int numero)
    {
        var json = await EjecutarSpAsync("sp_consultar_factura_y_productosporfactura",
            parametros => parametros.AddWithValue("@p_numero", numero));
        return ArmarFactura(json);
    }

    public async Task<Factura> CrearAsync(int fkidcliente, int fkidvendedor, string productosJson)
    {
        var json = await EjecutarSpAsync("sp_insertar_factura_y_productosporfactura", parametros =>
        {
            parametros.AddWithValue("@p_fkidcliente", fkidcliente);
            parametros.AddWithValue("@p_fkidvendedor", fkidvendedor);
            // En MariaDB el tipo JSON ES LONGTEXT: el detalle viaja como texto:
            parametros.AddWithValue("@p_productos", productosJson);
            parametros.AddWithValue("@p_minimo_detalle", 1);
        });
        return ArmarFactura(json);
    }

    public async Task<string> AnularAsync(int numero)
    {
        return await EjecutarSpAsync("sp_anular_factura",
            parametros => parametros.AddWithValue("@p_numero", numero));
    }
}
