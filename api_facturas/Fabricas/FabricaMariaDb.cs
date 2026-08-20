// ============================================================
// FabricaMariaDb — la fábrica del TERCER motor (v5).
//
// La cuenta didáctica de la v4, pagada por segunda vez: agregar
// MariaDB costó ESTA clase y UN case en el switch del ensamblador.
// Ni un cambio por encima de los repositorios.
// ============================================================

using ApiFacturas.Repositorios;

namespace ApiFacturas.Fabricas;

public class FabricaMariaDb : IFabricaRepositorios
{
    private readonly string _cadenaConexion;

    public FabricaMariaDb(string cadenaConexion)
    {
        _cadenaConexion = cadenaConexion;
    }

    public IRepositorioProducto CrearRepositorioProducto() => new RepositorioProductoMariaDb(_cadenaConexion);
    public IRepositorioPersona CrearRepositorioPersona() => new RepositorioPersonaMariaDb(_cadenaConexion);
    public IRepositorioFactura CrearRepositorioFactura() => new RepositorioFacturaMariaDb(_cadenaConexion);
    public IRepositorioEmpresa CrearRepositorioEmpresa() => new RepositorioEmpresaMariaDb(_cadenaConexion);
    public IRepositorioCliente CrearRepositorioCliente() => new RepositorioClienteMariaDb(_cadenaConexion);
    public IRepositorioVendedor CrearRepositorioVendedor() => new RepositorioVendedorMariaDb(_cadenaConexion);
    public IRepositorioUsuario CrearRepositorioUsuario() => new RepositorioUsuarioMariaDb(_cadenaConexion);
    public IRepositorioRol CrearRepositorioRol() => new RepositorioRolMariaDb(_cadenaConexion);
    public IRepositorioRuta CrearRepositorioRuta() => new RepositorioRutaMariaDb(_cadenaConexion);
    public IRepositorioRolUsuario CrearRepositorioRolUsuario() => new RepositorioRolUsuarioMariaDb(_cadenaConexion);
    public IRepositorioRutaRol CrearRepositorioRutaRol() => new RepositorioRutaRolMariaDb(_cadenaConexion);
}
