using Microsoft.EntityFrameworkCore;
namespace CapiBeadsSV.Models
{
    public class capibeadsBDContext : DbContext
    {
        public capibeadsBDContext(DbContextOptions options) : base(options)
        {

        }

        //Aquí poner los contextos de las tablas a utilizar:
        //Se pueden borrar las que no se ocuparan pero por si acaso las dejo todas.
        public DbSet<rol> rol { get; set; }

        //Referencia del modelado de la tabla para la autenticación de los usuarios
        public DbSet<usuarios> usuarios { get; set; }

        public DbSet<tiendas> tiendas { get; set; }
        public DbSet<categorias> categorias { get; set; }
        public DbSet<estados> estados { get; set; }
        public DbSet<productos> productos { get; set; }
        public DbSet<carritos> carritos { get; set; }
        public DbSet<carritoItems> carritoItems { get; set; } // Nueva tabla
        public DbSet<estadosPedidos> estadosPedidos { get; set; }
        public DbSet<ordenes> ordenes { get; set; }
        public DbSet<ordenItems> ordenItems { get; set; }

    }
}
