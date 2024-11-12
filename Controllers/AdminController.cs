using Microsoft.AspNetCore.Mvc;
using CapiBeadsSV.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Sockets;

namespace CapiBeadsSV.Controllers
{
    public class AdminController : Controller
    {
        private readonly capibeadsBDContext _capibeadsDBContext;

        public AdminController(capibeadsBDContext capibeadsDBContext)
        {
            _capibeadsDBContext = capibeadsDBContext;
        }

        // GET: Admin/Usuarios
        public async Task<IActionResult> IndexAdmin()
        {
            var usuarios = (from m in _capibeadsDBContext.usuarios
                            join r in _capibeadsDBContext.rol on m.id_rol equals r.id_rol
                            select new
                            {
                                m.id_usuario,
                                m.nombre,
                                m.correo,
                                rol = r.nombre_rol,
                                m.telefono_contacto,
                                m.usuario,
                                FotoBase64 = m.foto != null ? Convert.ToBase64String(m.foto) : null,  // Convertir a base64
                                m.contrasenya
                            }).ToList();

            ViewBag.usuarios = usuarios;

            return View();
        }

        public IActionResult CreateUsuarioAdmin()
        {
            //Lista de los roles
            var listaDeRoles = (from m in _capibeadsDBContext.rol
                                select m).ToList();
            ViewData["listadoDeRoles"] = new SelectList(listaDeRoles, "id_rol", "nombre_rol");

            return View();
        }

        //AA: Funcion para agregar los usuarios
        public IActionResult CreateUsuarios(usuarios usuarioNuevo)
        {
            _capibeadsDBContext.Add(usuarioNuevo);
            _capibeadsDBContext.SaveChanges();
            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }

        //AA: Funciones para editar usuarios
        public IActionResult EditUsuario(int? id)
        {
            // Obtenemos la lista de roles y seleccionamos el rol del usuario actual
            var listaDeRoles = (from m in _capibeadsDBContext.rol
                                select m).ToList();

            var usuario = (from m in _capibeadsDBContext.usuarios
                           join r in _capibeadsDBContext.rol on m.id_rol equals r.id_rol
                           where m.id_usuario == id
                           select new
                           {
                               m.id_usuario,
                               m.nombre,
                               m.correo,
                               rol = r.nombre_rol,
                               m.telefono_contacto,
                               m.usuario,
                               m.contrasenya,
                               m.foto,
                               m.id_rol  // Incluimos id_rol para seleccionarlo en la lista
                           }).FirstOrDefault();

            // Configuramos el SelectList con el rol seleccionado
            ViewData["listadoDeRoles"] = new SelectList(listaDeRoles, "id_rol", "nombre_rol", usuario.id_rol);

            ViewBag.usuario = usuario;

            return View();
        }


        public IActionResult Editarusuario(int? id, usuarios usuarioModificar)
        {
            if (id == null || usuarioModificar == null)
                return NotFound();

            var usuarioActual = (from m in _capibeadsDBContext.usuarios
                                 where m.id_usuario == id
                                 select m).FirstOrDefault();

            if (usuarioActual == null)
                return NotFound();

            usuarioActual.nombre = usuarioModificar.nombre;
            usuarioActual.correo = usuarioModificar.correo;
            usuarioActual.telefono_contacto = usuarioModificar.telefono_contacto;
            usuarioActual.id_rol = usuarioModificar.id_rol;

            _capibeadsDBContext.Entry(usuarioActual).State = EntityState.Modified;
            _capibeadsDBContext.SaveChanges();

            return RedirectToAction("SuccessModificar");
        }

        public IActionResult SuccessModificar()
        {
            return View();
        }

        //AA: Funcion para eliminar
        public IActionResult DeleteUsuario(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = (from m in _capibeadsDBContext.usuarios
                           join r in _capibeadsDBContext.rol on m.id_rol equals r.id_rol
                           where m.id_usuario == id
                           select new
                           {
                               m.id_usuario,
                               m.nombre,
                               m.correo,
                               m.usuario,
                               m.telefono_contacto,
                               m.id_rol,
                               NombreRol = r.nombre_rol // Obtener el nombre del rol
                           }).FirstOrDefault();

            if (usuario == null)
            {
                return NotFound();
            }

            ViewBag.Usuario = usuario;
            return View();
        }


        public IActionResult ConfirmDelete(int? id)
        {
            var usuario = _capibeadsDBContext.usuarios.FirstOrDefault(m => m.id_usuario == id);

            if (usuario == null)
                return NotFound();

            _capibeadsDBContext.usuarios.Remove(usuario);
            _capibeadsDBContext.SaveChanges();

            TempData["Mensaje"] = "Usuario eliminado correctamente.";
            return RedirectToAction("IndexAdmin");
        }

        //AA: Las demas vistas que faltaban
        public IActionResult Dashboard()
        {

            return View();
        }

        // Acción para mostrar el listado de categorías
        public async Task<IActionResult> Categorias()
        {
            var categorias = await _capibeadsDBContext.categorias.ToListAsync();
            ViewBag.categorias = categorias;
            return View(categorias);
        }
        // Vista para crear categoría
        public IActionResult CreateCategoria()
        {
            return View();
        }

        // Crear categoría (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategoria(categorias categoria)
        {
            if (ModelState.IsValid)
            {
                _capibeadsDBContext.Add(categoria);
                await _capibeadsDBContext.SaveChangesAsync();
                return RedirectToAction(nameof(Categorias));
            }
            return View(categoria);
        }

        // Vista para editar categoría
        public async Task<IActionResult> EditCategoria(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _capibeadsDBContext.categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria);
        }

        // Editar categoría (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategoria(int id, categorias categoria)
        {
            if (id != categoria.id_categoria)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _capibeadsDBContext.Update(categoria);
                await _capibeadsDBContext.SaveChangesAsync();
                return RedirectToAction(nameof(Categorias));
            }

            return View(categoria);
        }

        // Vista para eliminar categoría (GET)
        public async Task<IActionResult> DeleteCategoria(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var categoria = await _capibeadsDBContext.categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }

            return View(categoria); // Pasamos la categoría a la vista
        }

        // Eliminar categoría (POST)
        [HttpPost, ActionName("DeleteCategoria")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategoriaConfirmed(int id)
        {
            var categoria = await _capibeadsDBContext.categorias.FindAsync(id);

            if (categoria == null)
            {
                return NotFound();
            }

            _capibeadsDBContext.categorias.Remove(categoria); // Eliminamos la categoría
            await _capibeadsDBContext.SaveChangesAsync(); // Guardamos los cambios

            return RedirectToAction(nameof(Categorias)); // Redirigimos a la lista de categorías
        }
        public IActionResult Tiendas()
        {
            var tiendas = (from t in _capibeadsDBContext.tiendas
                           join u in _capibeadsDBContext.usuarios on t.id_usuario equals u.id_usuario
                           select new
                           {
                               t.id_tienda,
                               t.nombreTienda,
                               t.descripcionTienda,
                               t.imagenFondo,
                               UsuarioNombre = u.nombre
                           }).ToList();

            ViewBag.tiendas = tiendas;
            return View();
        }
        // GET: Admin/CreateTienda
        public IActionResult CreateTienda()
        {
            // Filtramos los usuarios con id_rol = 2
            var listaDeUsuarios = _capibeadsDBContext.usuarios
                                                    .Where(u => u.id_rol == 2)
                                                    .ToList();

            // Asignamos la lista filtrada al ViewData para ser usada en la vista
            ViewData["Usuarios"] = new SelectList(listaDeUsuarios, "id_usuario", "nombre");

            return View();
        }


        // POST: Admin/CreateTienda
        [HttpPost]
        public async Task<IActionResult> CreateTienda(tiendas nuevaTienda, IFormFile imagenFondo)
        {
            if (imagenFondo != null && imagenFondo.Length > 0)
            {
                using (var ms = new System.IO.MemoryStream())
                {
                    imagenFondo.CopyTo(ms);
                    nuevaTienda.imagenFondo = ms.ToArray();
                }
            }

            _capibeadsDBContext.tiendas.Add(nuevaTienda);
            await _capibeadsDBContext.SaveChangesAsync();
            return RedirectToAction("SuccessTienda");
        }

        public IActionResult SuccessTienda()
        {
            return View();
        }

        // GET: Admin/EditTienda/5
        public async Task<IActionResult> EditTienda(int? id)
        {
            if (id == null) return NotFound();

            var tienda = await _capibeadsDBContext.tiendas.FindAsync(id);
            if (tienda == null) return NotFound();

            // Obtener la lista de usuarios con el rol deseado
            var usuarios = await _capibeadsDBContext.usuarios
                .Where(u => u.id_rol == 2)
                .Select(u => new { u.id_usuario, u.nombre })
                .ToListAsync();

            // Crear el SelectList y pasar la tienda actual como valor seleccionado
            ViewBag.Usuarios = new SelectList(usuarios, "id_usuario", "nombre", tienda.id_usuario);
            ViewBag.Tienda = tienda;

            return View(tienda);
        }

        // POST: Admin/EditTienda/5
        [HttpPost]
        public async Task<IActionResult> EditTienda(int id, tiendas tiendaModificada, IFormFile imagenFondo)
        {
            if (id != tiendaModificada.id_tienda) return NotFound();

            var tienda = await _capibeadsDBContext.tiendas.FindAsync(id);
            if (tienda == null) return NotFound();

            // Actualizar los datos de la tienda
            tienda.nombreTienda = tiendaModificada.nombreTienda;
            tienda.descripcionTienda = tiendaModificada.descripcionTienda;
            tienda.id_usuario = tiendaModificada.id_usuario;

            // Verificar y actualizar la imagen de fondo si se proporciona una nueva
            if (imagenFondo != null && imagenFondo.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    imagenFondo.CopyTo(ms);
                    tienda.imagenFondo = ms.ToArray();
                }
            }

            _capibeadsDBContext.Entry(tienda).State = EntityState.Modified;
            await _capibeadsDBContext.SaveChangesAsync();

            return RedirectToAction("SuccessModificarTienda");
        }
        public IActionResult SuccessModificarTienda()
        {
            return View();
        }

        public async Task<IActionResult> DeleteTienda(int? id)
        {
            if (id == null) return NotFound();

            var tienda = await (from t in _capibeadsDBContext.tiendas
                                join u in _capibeadsDBContext.usuarios on t.id_usuario equals u.id_usuario
                                where t.id_tienda == id
                                select new
                                {
                                    t.id_tienda,
                                    t.nombreTienda,
                                    t.descripcionTienda,
                                    UsuarioNombre = u.nombre
                                }).FirstOrDefaultAsync();

            if (tienda == null) return NotFound();

            ViewBag.Tienda = tienda;
            return View();
        }

        // POST: Admin/ConfirmDeleteTienda/5
        [HttpPost, ActionName("DeleteTienda")]
        public async Task<IActionResult> ConfirmDeleteTienda(int id)
        {
            var tienda = await _capibeadsDBContext.tiendas.FindAsync(id);
            if (tienda == null) return NotFound();

            _capibeadsDBContext.tiendas.Remove(tienda);
            await _capibeadsDBContext.SaveChangesAsync();

            TempData["Mensaje"] = "Tienda eliminada correctamente.";
            return RedirectToAction("ConfirmDeleteTienda");
        }

        // Vista para mostrar mensaje de éxito tras eliminar la tienda
        public IActionResult ConfirmDeleteTienda()
        {
            return View();
        }

        // Listado de productos
        public async Task<IActionResult> Productos()
        {
            var productos = await (from p in _capibeadsDBContext.productos
                                   join t in _capibeadsDBContext.tiendas on p.id_tienda equals t.id_tienda
                                   join e in _capibeadsDBContext.estados on p.id_estadoProducto equals e.id_estadoProducto
                                   join c in _capibeadsDBContext.categorias on p.id_categoria equals c.id_categoria // Join con categorías
                                   select new
                                   {
                                       p.id_producto,
                                       p.nombreProducto,
                                       p.descripcion,
                                       p.precio,
                                       p.imagenProducto,
                                       CategoriaNombre = c.nombreCategoria,
                                       TiendaNombre = t.nombreTienda,
                                       EstadoProducto = e.nombreEstado
                                   }).ToListAsync();

            ViewBag.Productos = productos;
            return View();
        }

        // Crear producto
        public IActionResult CreateProducto()
        {
            ViewData["Tiendas"] = new SelectList(_capibeadsDBContext.tiendas, "id_tienda", "nombreTienda");
            ViewData["Estados"] = new SelectList(_capibeadsDBContext.estados, "id_estadoProducto", "nombreEstado");
            ViewData["Categorias"] = new SelectList(_capibeadsDBContext.categorias, "id_categoria", "nombreCategoria");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProducto(productos nuevoProducto, IFormFile imagenProducto)
        {
            if (imagenProducto != null && imagenProducto.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    imagenProducto.CopyTo(ms);
                    nuevoProducto.imagenProducto = ms.ToArray();
                }
            }

            _capibeadsDBContext.productos.Add(nuevoProducto);
            await _capibeadsDBContext.SaveChangesAsync();
            return RedirectToAction("SuccessProducto");
        }

        public IActionResult SuccessProducto()
        {
            return View();
        }

        // GET: Productos/EditProducto/5
        public async Task<IActionResult> EditProducto(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _capibeadsDBContext.productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            // Load data for dropdown lists
            ViewData["Tiendas"] = new SelectList(_capibeadsDBContext.tiendas, "id_tienda", "nombreTienda", producto.id_tienda);
            ViewData["Categorias"] = new SelectList(_capibeadsDBContext.categorias, "id_categoria", "nombreCategoria", producto.id_categoria);
            ViewData["Estados"] = new SelectList(_capibeadsDBContext.estados, "id_estadoProducto", "nombreEstado", producto.id_estadoProducto);

            return View(producto);
        }

        [HttpPost]
        public async Task<IActionResult> EditProducto(int id, productos productoEditado, IFormFile imagenProducto)
        {
            if (id != productoEditado.id_producto)
            {
                return BadRequest();
            }

            if (imagenProducto != null && imagenProducto.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    imagenProducto.CopyTo(ms);
                    productoEditado.imagenProducto = ms.ToArray();
                }
            }
            else
            {
                var productoExistente = await _capibeadsDBContext.productos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.id_producto == id);
                productoEditado.imagenProducto = productoExistente?.imagenProducto;
            }

            _capibeadsDBContext.Entry(productoEditado).State = EntityState.Modified;

            await _capibeadsDBContext.SaveChangesAsync();
            return RedirectToAction("SuccessModificarProducto");
        }

        // GET: Productos/SuccessModificarProducto
        public IActionResult SuccessModificarProducto()
        {
            return View();
        }

        // Eliminar producto
        // GET: Productos/DeleteProducto/5
        public async Task<IActionResult> DeleteProducto(int? id)
        {
            if (id == null)
                return NotFound();

            var producto = await (from p in _capibeadsDBContext.productos
                                  join t in _capibeadsDBContext.tiendas on p.id_tienda equals t.id_tienda
                                  where p.id_producto == id
                                  select new
                                  {
                                      p.id_producto,
                                      p.nombreProducto,
                                      p.descripcion,
                                      p.precio,
                                      TiendaNombre = t.nombreTienda
                                  }).FirstOrDefaultAsync();

            if (producto == null)
                return NotFound();

            ViewBag.Producto = producto;
            return View();
        }

        // POST: Productos/DeleteProducto/5
        [HttpPost, ActionName("DeleteProducto")]
        public async Task<IActionResult> ConfirmDeleteProducto(int id)
        {
            var producto = await _capibeadsDBContext.productos.FindAsync(id);
            if (producto == null)
                return NotFound();

            _capibeadsDBContext.productos.Remove(producto);
            await _capibeadsDBContext.SaveChangesAsync();

            TempData["Mensaje"] = "Producto eliminado correctamente.";
            return RedirectToAction("Productos");
        }

        // Success View for Deletion
        public IActionResult SuccessEliminarProducto()
        {
            return View();
        }

        public async Task<IActionResult> Pedidos()
        {
            var ordenes = await (from o in _capibeadsDBContext.ordenes
                                 join ep in _capibeadsDBContext.estadosPedidos on o.id_estadoPedido equals ep.id_estadoPedido
                                 join u in _capibeadsDBContext.usuarios on o.id_usuario equals u.id_usuario
                                 select new
                                 {
                                     o.id_orden,
                                     o.direccion,
                                     o.total,
                                     o.fechaOrden,
                                     EstadoPedido = ep.nombreEstadoPedido,
                                     Cliente = u.nombre,
                                     Productos = (from oi in _capibeadsDBContext.ordenItems
                                                  join p in _capibeadsDBContext.productos on oi.id_producto equals p.id_producto
                                                  join t in _capibeadsDBContext.tiendas on p.id_tienda equals t.id_tienda
                                                  where oi.id_orden == o.id_orden
                                                  select new
                                                  {
                                                      Producto = p.nombreProducto,
                                                      Tienda = t.nombreTienda,
                                                      Cantidad = oi.cantidad,
                                                      PrecioUnitario = oi.precio_unitario,
                                                      ImagenProducto = p.imagenProducto
                                                  }).ToList()
                                 }).ToListAsync();

            return View(ordenes);
        }
        public IActionResult CreateOrden()
        {
            return View();
        }

        // Método para crear una nueva orden (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrden([Bind("id_carrito,direccion,total,id_estadoPedido")] ordenes orden)
        {
            if (ModelState.IsValid)
            {
                orden.fechaOrden = DateTime.Now; // Establece la fecha actual
                _capibeadsDBContext.Add(orden);
                await _capibeadsDBContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(orden);
        }


        public IActionResult Perfil()
        {

            return View();
        }
    }
}
