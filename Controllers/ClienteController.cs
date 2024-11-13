using CapiBeadsSV.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            // Mensaje de éxito al agregar el producto
            TempData["Mensaje"] = "Producto agregado correctamente al carrito.";

            // Redirigir a la vista actual o al carrito
            return RedirectToAction("IndexCliente");
        }
        public IActionResult VerCarrito()
        {
            // Verifica si la sesión contiene el usuario autenticado
            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            if (usuarioSesion == null)
            {
                // Si el usuario no está autenticado, redirigir al inicio de sesión o manejar el caso apropiado
                return RedirectToAction("Login", "Cuenta");
            }

            // Obtener el carrito del usuario actual
            var carrito = _context.carritos.FirstOrDefault(c => c.id_usuario == usuarioSesion.id_usuario);
            if (carrito == null)
            {
                // Si el carrito no existe, mostrar el carrito vacío
                ViewBag.TotalCarrito = 0;
                return View("VerCarrito", new List<dynamic>());
            }

            // Obtén los items del carrito asociados al carrito del usuario actual
            var carritoItems = from ci in _context.carritoItems
                               join p in _context.productos on ci.id_producto equals p.id_producto
                               where ci.id_carrito == carrito.id_carrito
                               select new
                               {
                                   nombreProducto = p.nombreProducto,
                                   cantidad = ci.cantidad,
                                   precio_unitario = ci.precio_unitario,
                                   subtotal = ci.cantidad * ci.precio_unitario
                               };

            // Calcula el total del carrito
            var totalCarrito = carritoItems.Sum(ci => ci.subtotal);
            ViewBag.TotalCarrito = totalCarrito;

            return View("VerCarrito", carritoItems.ToList());
        }
    }
}
