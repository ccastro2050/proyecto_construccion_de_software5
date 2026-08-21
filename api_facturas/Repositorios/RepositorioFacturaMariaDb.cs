// ============================================================
// RepositorioFacturaMariaDb — la capa de DATOS de factura.
//
// La API como TRADUCTORA, tercer dialecto: Dapper con
// CommandType.StoredProcedure y el parámetro OUT recogido por
// DynamicParameters (MySqlConnector lo emula con variables de
// sesión — por eso la cadena lleva AllowUserVariables=True).
// Errores de negocio: SIGNAL '45000' → MySqlException con número
// 1644 (genérico) + PATRÓN del mensaje — el punto medio entre el
// THROW numerado de SQL Server y el P0001 de PostgreSQL.
// ============================================================

using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApiFacturas.Excepciones;
using ApiFacturas.Modelos;
using Dapper;
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

    private MySqlConnection CrearConexion() => new(_cadenaConexion);

    /// <summary>Ejecuta el SP con Dapper y devuelve el JSON del
    /// parámetro OUT p_resultado (LONGTEXT: el JSON de MariaDB).</summary>
    private async Task<string> EjecutarSpAsync(string nombreSp, object? entrada)
    {
        var parametros = entrada == null
            ? new DynamicParameters()
            : new DynamicParameters(entrada);
        parametros.Add("p_resultado", dbType: DbType.String,
                       direction: ParameterDirection.Output, size: -1);

        await using var conexion = CrearConexion();
        try
        {
            await conexion.ExecuteAsync(nombreSp, parametros,
                commandType: CommandType.StoredProcedure);
        }
        // 1644 = ER_SIGNAL_EXCEPTION (todos los SIGNAL '45000'):
        catch (MySqlException e) when (e.Number == 1644 && e.Message.Contains("no existe"))
        {
            throw new NoEncontradoExcepcion(e.Message);      // → 404
        }
        catch (MySqlException e) when (e.Number == 1644 && e.Message.Contains("anulada"))
        {
            throw new ConflictoExcepcion(e.Message);         // → 409
        }
        // Lo demás (stock insuficiente del trigger, FK) sube tal cual → 500.

        return parametros.Get<string?>("p_resultado") ?? "null";
    }

    // El SP de consultar/crear responde {"factura":{…},"productos":[…]}.
    // Esta clase privada calza ese sobre para deserializarlo y devolver
    // UNA Factura con su detalle adentro:
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

    public async Task<List<Factura>> ListarAsync()
    {
        var json = await EjecutarSpAsync("sp_listar_facturas_y_productosporfactura", null);
        return JsonSerializer.Deserialize<List<Factura>>(json, _opcionesJson) ?? new List<Factura>();
    }

    public async Task<Factura> ConsultarAsync(int numero)
    {
        var json = await EjecutarSpAsync("sp_consultar_factura_y_productosporfactura",
            new { p_numero = numero });
        return ArmarFactura(json);
    }

    public async Task<Factura> CrearAsync(int fkidcliente, int fkidvendedor, string productosJson)
    {
        // En MariaDB el tipo JSON ES LONGTEXT: el detalle viaja como texto.
        var json = await EjecutarSpAsync("sp_insertar_factura_y_productosporfactura",
            new { p_fkidcliente = fkidcliente, p_fkidvendedor = fkidvendedor,
                  p_productos = productosJson, p_minimo_detalle = 1 });
        return ArmarFactura(json);
    }

    public async Task<string> AnularAsync(int numero)
    {
        return await EjecutarSpAsync("sp_anular_factura", new { p_numero = numero });
    }
}
