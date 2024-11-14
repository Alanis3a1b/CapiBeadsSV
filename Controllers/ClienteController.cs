using CapiBeadsSV.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CapiBeadsSV.Controllers
{
    public class ClienteController : Controller
    {
        private readonly capibeadsBDContext _context;

        public ClienteController(capibeadsBDContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> IndexCliente()
        {
            // Obtener productos
            var productos = await (from p in _context.productos
                                   join c in _context.categorias on p.id_categoria equals c.id_categoria
                                   select new
                                   {
                                       Id = p.id_producto,
                                       Nombre = p.nombreProducto,
                                       Precio = p.precio,
                                       FotoBase64 = p.imagenProducto != null ? Convert.ToBase64String(p.imagenProducto) : null,
                                       Descripcion = p.descripcion,
                                       Stock = p.stock
                                   }).ToListAsync();

            // Obtener tiendas
            var tiendas = await _context.tiendas
                                        .Select(t => new
                                        {
                                            t.id_tienda,
                                            t.nombreTienda,
                                            t.descripcionTienda,
                                            FotoBase64 = t.imagenFondo != null ? Convert.ToBase64String(t.imagenFondo) : null
                                        })
                                        .ToListAsync();

            // Seleccionar productos aleatorios en el controlador
            var productosAleatorios = productos.OrderBy(p => Guid.NewGuid()).Take(6).ToList();
            ViewBag.Productos = productos;
            ViewBag.ProductosAleatorios = productosAleatorios;
            ViewBag.Tiendas = tiendas;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AgregarAlCarrito(int idProducto)
        {
            // Obtener el usuario en sesión
            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            int usuarioId = usuarioSesion.id_usuario;

            // Obtener o crear el carrito del usuario
            var carrito = await _context.carritos
                .FirstOrDefaultAsync(c => c.id_usuario == usuarioId);
            if (carrito == null)
            {
                carrito = new carritos { id_usuario = usuarioId, total = 0 };
                _context.carritos.Add(carrito);
                await _context.SaveChangesAsync(); // Guardar para obtener el id_carrito
            }

            // Verificar si el producto ya está en el carrito
            var carritoItem = await _context.carritoItems
                .FirstOrDefaultAsync(ci => ci.id_carrito == carrito.id_carrito && ci.id_producto == idProducto);

            if (carritoItem == null)
            {
                // Agregar nuevo item al carrito
                var producto = await _context.productos.FindAsync(idProducto);
                if (producto != null)
                {
                    carritoItem = new carritoItems
                    {
                        id_carrito = carrito.id_carrito,
                        id_producto = idProducto,
                        cantidad = 1,
                        precio_unitario = producto.precio
                    };
                    _context.carritoItems.Add(carritoItem);
                }
            }
            else
            {
                // Incrementar la cantidad del producto si ya existe en el carrito
                carritoItem.cantidad++;
            }

            // Actualizar el total del carrito
            carrito.total += carritoItem.precio_unitario;
            await _context.SaveChangesAsync();

            // Redirigir a la vista actual o al carrito
            return Json(new { success = true, message = "Producto agregado correctamente al carrito." });
        }

        public IActionResult VerCarrito()
        {
            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            var carrito = _context.carritos.FirstOrDefault(c => c.id_usuario == usuarioSesion.id_usuario);

            var carritoItems = _context.carritoItems.Where(ci => ci.id_carrito == carrito.id_carrito)
                .Join(_context.productos, ci => ci.id_producto, p => p.id_producto, (ci, p) => new
                {
                    idCarritoItem = ci.id_carritoItem,
                    nombreProducto = p.nombreProducto,
                    cantidad = ci.cantidad,
                    precio_unitario = ci.precio_unitario,
                    subtotal = ci.cantidad * ci.precio_unitario,
                    imagenProducto = p.imagenProducto != null ? Convert.ToBase64String(p.imagenProducto) : null
                }).ToList();

            ViewBag.TotalCarrito = carritoItems.Sum(ci => ci.subtotal);
            ViewBag.IdCarrito = carrito?.id_carrito; // Pasar el ID del carrito al ViewBag

            return View("VerCarrito", carritoItems);
        }

        [HttpPost]
        public async Task<IActionResult> IncrementarCantidad(int idCarritoItem)
        {
            var carritoItem = await _context.carritoItems.FindAsync(idCarritoItem);

            if (carritoItem != null)
            {
                carritoItem.cantidad++;
                await _context.SaveChangesAsync();
            }

            var subtotal = carritoItem?.cantidad * carritoItem?.precio_unitario ?? 0;
            var totalCarrito = await _context.carritoItems
                .Where(ci => ci.id_carrito == carritoItem.id_carrito)
                .SumAsync(ci => ci.cantidad * ci.precio_unitario);

            return Json(new { success = true, newQuantity = carritoItem?.cantidad, newSubtotal = subtotal, newTotal = totalCarrito });
        }
        [HttpPost]
        public async Task<IActionResult> DisminuirCantidad(int idCarritoItem)
        {
            var carritoItem = await _context.carritoItems.FindAsync(idCarritoItem);
            bool removeItem = false;

            if (carritoItem != null)
            {
                if (carritoItem.cantidad > 1)
                {
                    carritoItem.cantidad--;
                }
                else
                {
                    _context.carritoItems.Remove(carritoItem);
                    removeItem = true; // Indicar que el producto será eliminado
                }

                await _context.SaveChangesAsync();
            }

            // Verificar si el carrito está vacío
            bool carritoVacio = carritoItem != null && !_context.carritoItems.Any(ci => ci.id_carrito == carritoItem.id_carrito);

            // Calcular el total del carrito
            var totalCarrito = carritoItem != null
                ? await _context.carritoItems
                    .Where(ci => ci.id_carrito == carritoItem.id_carrito)
                    .SumAsync(ci => ci.cantidad * ci.precio_unitario)
                : 0;

            // Obtener los nuevos subtotales y cantidades
            var nuevoSubtotal = carritoItem != null ? carritoItem.cantidad * carritoItem.precio_unitario : 0;

            return Json(new
            {
                success = true,
                newQuantity = carritoItem != null ? carritoItem.cantidad : 0,
                newSubtotal = nuevoSubtotal,
                removeItem, // Indica si el producto fue eliminado
                carritoVacio,
                newTotal = totalCarrito
            });
        }
        // Método que se ejecuta antes de cada acción
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Cargar las categorías y almacenarlas en ViewBag
            var categorias = _context.categorias.ToList();
            ViewBag.Categorias = categorias;

            base.OnActionExecuting(context);
        }

        public async Task<IActionResult> TodosLosProductos(int? idCategoria = null)
        {
            // Obtener todos los productos o filtrar por categoría si se proporciona idCategoria
            var productos = await _context.productos
                .Where(p => !idCategoria.HasValue || p.id_categoria == idCategoria)
                .Select(p => new
                {
                    Id = p.id_producto,
                    Nombre = p.nombreProducto,
                    Precio = p.precio,
                    FotoBase64 = p.imagenProducto != null ? Convert.ToBase64String(p.imagenProducto) : null,
                    Descripcion = p.descripcion,
                    Stock = p.stock,
                    IdCategoria = p.id_categoria
                }).ToListAsync();

            ViewBag.Productos = productos;
            ViewBag.CategoriaSeleccionada = idCategoria;

            return View();
        }


        [HttpGet]
        public IActionResult ProcesarCompra(int id_carrito, decimal totalCarrito)
        {
            // Pasa los valores necesarios a la vista
            ViewBag.IdCarrito = id_carrito;
            ViewBag.TotalCarrito = totalCarrito;

            // Devuelve la vista para confirmar la compra
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarCompraConfirmacion(int id_carrito, decimal totalCarrito, string direccion)
        {
            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));

            // Crear la nueva orden
            var nuevaOrden = new ordenes
            {
                id_usuario = usuarioSesion.id_usuario,
                direccion = direccion,
                total = totalCarrito,
                fechaOrden = DateTime.Now,
                id_estadoPedido = 1 // Asume que 1 es el estado inicial para "pendiente" o similar
            };

            _context.ordenes.Add(nuevaOrden);
            await _context.SaveChangesAsync();

            // Redirigir a una vista de confirmación o al historial de pedidos
            return RedirectToAction("OrdenConfirmada", new { id = nuevaOrden.id_orden });
        }

        [HttpGet]
        public IActionResult OrdenConfirmada(int id)
        {
            // Obtener la orden creada usando el ID
            var orden = _context.ordenes.FirstOrDefault(o => o.id_orden == id);

            if (orden == null)
            {
                return NotFound("Orden no encontrada.");
            }

            // Pasar los datos de la orden a la vista
            return View(orden);
        }
    }
}
