using CapiBeadsSV.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
            var productos = (from p in _context.productos
                             join c in _context.categorias on p.id_categoria equals c.id_categoria
                             select new
                             {
                                 Id = p.id_producto,
                                 Nombre = p.nombreProducto,
                                 Precio = p.precio,
                                 FotoBase64 = p.imagenProducto != null ? Convert.ToBase64String(p.imagenProducto) : null,
                                 Descripcion = p.descripcion
                             }).ToList();

            // Obtener tiendas
            var tiendas = _context.tiendas
                            .Select(t => new
                            {
                                t.id_tienda,
                                t.nombreTienda,
                                t.descripcionTienda,
                                FotoBase64 = t.imagenFondo != null ? Convert.ToBase64String(t.imagenFondo) : null
                            })
                            .ToList();

            // Seleccionar productos aleatorios en el controlador
            var productosAleatorios = productos.OrderBy(p => Guid.NewGuid()).Take(6).ToList();
            ViewBag.Productos = productos;
            ViewBag.ProductosAleatorios = productosAleatorios;
            ViewBag.Tiendas = tiendas;

            return View();
        }
        public async Task<IActionResult> AgregarAlCarrito(int idProducto, int cantidad)
        {
            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            if (usuarioSesion == null)
            {
                return Unauthorized("Debe iniciar sesión.");
            }

            // Obtener o crear el carrito del usuario
            var carrito = _context.carritos.FirstOrDefault(c => c.id_usuario == usuarioSesion.id_usuario);
            if (carrito == null)
            {
                carrito = new carritos
                {
                    id_usuario = usuarioSesion.id_usuario,
                    total = 0 // Inicializar el total en 0
                };
                _context.carritos.Add(carrito);
                await _context.SaveChangesAsync();
            }

            // Verificar si el producto existe
            var producto = await _context.productos.FindAsync(idProducto);
            if (producto == null)
            {
                return NotFound("Producto no encontrado.");
            }

            // Agregar o actualizar el producto en carritoItems
            var itemCarrito = _context.carritoItems.FirstOrDefault(ci => ci.id_carrito == carrito.id_carrito && ci.id_producto == idProducto);
            if (itemCarrito != null)
            {
                // Si el producto ya está en el carrito, incrementar la cantidad
                itemCarrito.cantidad += cantidad;
            }
            else
            {
                // Si no está, añadirlo como nuevo ítem
                itemCarrito = new carritoItems
                {
                    id_carrito = carrito.id_carrito,
                    id_producto = idProducto,
                    cantidad = cantidad,
                    precio_unitario = producto.precio
                };
                _context.carritoItems.Add(itemCarrito);
            }

            // Actualizar el total del carrito
            carrito.total += cantidad * producto.precio;

            await _context.SaveChangesAsync();

            return Ok("Producto agregado al carrito.");
        }

        // GET: ClienteController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ClienteController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ClienteController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ClienteController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ClienteController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ClienteController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ClienteController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
