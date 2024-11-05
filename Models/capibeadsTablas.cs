using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CapiBeadsSV.Models
{
    public class capibeadsTablas
    {
    }

    public class rol
    {
        [Key]
        public int id_rol { get; set; }
        public string nombre_rol { get; set; }
    }

    public class usuarios
    {
        [Key]
        public int id_usuario { get; set; }
        public string nombre { get; set; }
        public string correo { get; set; }
        public int id_rol { get; set; }
        public string telefono_contacto { get; set; }
        public string usuario { get; set; }
        [JsonIgnore]
        public string contrasenya { get; set; }
        public byte[]? foto { get; set; } // Añadido para la foto

        [NotMapped]
        public IFormFile PhotoUpload { get; set; }
    }

    public class tiendas
    {
        [Key]
        public int id_tienda { get; set; }
        public int id_usuario { get; set; }
        public string nombreTienda { get; set; }

        public byte[]? imagenFondo { get; set; } // Añadido para la foto
        public byte[]? colorFondo { get; set; }

        [NotMapped]
        public IFormFile PhotoUpload { get; set; }
    }

    public class categorias
    {
        [Key]
        public int id_categoria { get; set; }
        public string nombreCategoria { get; set; }

    }

    public class estados
    {
        [Key]
        public int id_estadoProducto { get; set; }
        public string nombreEstado { get; set; }

    }

    public class productos
    {
        [Key]
        public int id_producto { get; set; }
        public int id_tienda { get; set; }
        public int id_categoria { get; set; }
        public string nombreProducto { get; set; }
        public string descripcion { get; set; }
        public decimal precio { get; set; }
        public int stock { get; set; }
        public int id_estadoProducto { get; set; }
        public byte[]? imagenProducto { get; set; } // Añadido para la foto

        [NotMapped]
        public IFormFile PhotoUpload { get; set; }
    }

    public class carritos
    {
        [Key]
        public int id_carrito { get; set; }
        public int id_usuario { get; set; }
        public int id_producto { get; set; }
        public int cantidad { get; set; }
        public decimal precio_unitario { get; set; }
    }

    public class estadosPedidos
    {
        [Key]
        public int id_estadoPedido { get; set; }
        public string nombreEstadoPedido { get; set; }

    }

    public class ordenes
    {
        [Key]
        public int id_orden { get; set; }
        public int id_carrito { get; set; }
        public string direccion { get; set; }
        public decimal total { get; set; }
        //Esto me creará las fechas de forma automática
        public DateTime fechaOrden { get; set; } = DateTime.Now;
        public int id_estadoPedido { get; set; }

    }

}
