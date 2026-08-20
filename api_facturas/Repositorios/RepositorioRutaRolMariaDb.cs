// RepositorioRutaRolMariaDb — la capa de DATOS del puente rutarol (v5).
// El DELETE filtra por LAS DOS columnas: borra una pareja exacta,
// nunca "todo lo del usuario/la ruta" (regla dura de la spec).

using ApiFacturas.Modelos;
using MySqlConnector;

namespace ApiFacturas.Repositorios;

public class RepositorioRutaRolMariaDb : IRepositorioRutaRol
{
    private readonly string _cadenaConexion;

    public RepositorioRutaRolMariaDb(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    private async Task<MySqlConnection> AbrirConexionAsync()
    {
        var conexion = new MySqlConnection(_cadenaConexion);
        await conexion.OpenAsync();
        return conexion;
    }

    private static RutaRol Armar(MySqlDataReader lector)
    {
        return new RutaRol
        {
            Fkidruta = lector.GetInt32(0),
            Fkidrol = lector.GetInt32(1),
        };
    }

    private async Task<List<RutaRol>> ConsultarAsync(string sql, Action<MySqlParameterCollection> configurar)
    {
        await using var conexion = await AbrirConexionAsync();
        await using var comando = new MySqlCommand(sql, conexion);
        configurar(comando.Parameters);
        await using var lector = await comando.ExecuteReaderAsync();
        var lista = new List<RutaRol>();
        while (await lector.ReadAsync()) { lista.Add(Armar(lector)); }
        return lista;
    }

    public Task<List<RutaRol>> ObtenerTodosAsync(int limite)
    {
        return ConsultarAsync(
            @"SELECT fkidruta, fkidrol FROM rutarol ORDER BY fkidruta, fkidrol LIMIT @limite",
            p => p.AddWithValue("@limite", limite));
    }

    public Task<List<RutaRol>> ObtenerPorRutaAsync(int fkidruta)
    {
        return ConsultarAsync(
            @"SELECT fkidruta, fkidrol FROM rutarol WHERE fkidruta = @a",
            p => p.AddWithValue("@a", fkidruta));
    }

    public Task<List<RutaRol>> ObtenerPorRolAsync(int fkidrol)
    {
        return ConsultarAsync(
            @"SELECT fkidruta, fkidrol FROM rutarol WHERE fkidrol = @b",
            p => p.AddWithValue("@b", fkidrol));
    }

    public async Task CrearAsync(RutaRol asignacion)
    {
        const string sql = @"INSERT INTO rutarol (fkidruta, fkidrol) VALUES (@a, @b)";
        await using var conexion = await AbrirConexionAsync();
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@a", asignacion.Fkidruta);
        comando.Parameters.AddWithValue("@b", asignacion.Fkidrol);
        // Duplicado → viola la PK compuesta → MySqlException → 500:
        await comando.ExecuteNonQueryAsync();
    }

    public async Task<int> EliminarAsync(int fkidruta, int fkidrol)
    {
        // LA PAREJA EXACTA: las dos columnas en el WHERE.
        const string sql = @"DELETE FROM rutarol WHERE fkidruta = @a AND fkidrol = @b";
        await using var conexion = await AbrirConexionAsync();
        await using var comando = new MySqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@a", fkidruta);
        comando.Parameters.AddWithValue("@b", fkidrol);
        return await comando.ExecuteNonQueryAsync();
    }
}
