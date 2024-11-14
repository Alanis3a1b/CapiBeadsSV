using Microsoft.AspNetCore.Mvc;
using CapiBeadsSV.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using CapiBeadsSV.Serv;

namespace CapiBeadsSV.Controllers
{
    public class AdminController : Controller
    {
        private readonly capibeadsBDContext _capibeadsDBContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IUserService _userService;


        public AdminController(capibeadsBDContext capibeadsDBContext, IWebHostEnvironment webHostEnvironment, IUserService userService)
        {
            _capibeadsDBContext = capibeadsDBContext;
            _webHostEnvironment = webHostEnvironment;
            _userService = userService;
        }

        // GET: Admin/Usuarios
        public async Task<IActionResult> IndexAdmin()
        {
            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
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

            ViewBag.Usuario = usuarioSesion;
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
                                       p.stock,
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
                                     Productos = (from oi in _capibeadsDBContext.carritoItems
                                                  join p in _capibeadsDBContext.productos on oi.id_producto equals p.id_producto
                                                  join t in _capibeadsDBContext.tiendas on p.id_tienda equals t.id_tienda
                                                  where oi.id_carrito == (from c in _capibeadsDBContext.carritos
                                                                          where c.id_usuario == o.id_usuario
                                                                          select c.id_carrito).FirstOrDefault()
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


        public async Task<IActionResult> EditOrden(int id)
        {
            var orden = await (from o in _capibeadsDBContext.ordenes
                               join ep in _capibeadsDBContext.estadosPedidos on o.id_estadoPedido equals ep.id_estadoPedido
                               where o.id_orden == id
                               select new
                               {
                                   o.id_orden,
                                   o.direccion,
                                   o.total,
                                   o.fechaOrden,
                                   EstadoPedido = ep.nombreEstadoPedido,
                                   o.id_estadoPedido
                               }).FirstOrDefaultAsync();

            if (orden == null)
            {
                return NotFound();
            }

            // Cargar los estados de pedido en ViewData para el dropdown en la vista
            ViewData["Estados"] = new SelectList(_capibeadsDBContext.estadosPedidos, "id_estadoPedido", "nombreEstadoPedido", orden.id_estadoPedido);

            return View(orden);
        }

        [HttpPost]
        public async Task<IActionResult> EditOrden(int id, int id_estadoPedido)
        {
            var orden = await _capibeadsDBContext.ordenes.FindAsync(id);
            if (orden == null)
            {
                return NotFound();
            }

            // Actualizar el estado del pedido
            orden.id_estadoPedido = id_estadoPedido;
            _capibeadsDBContext.Update(orden);
            await _capibeadsDBContext.SaveChangesAsync();

            return RedirectToAction(nameof(Pedidos));
        }
        public async Task<IActionResult> DeleteOrden(int id)
        {
            var orden = await _capibeadsDBContext.ordenes.FindAsync(id);

            if (orden == null)
            {
                return NotFound();
            }

            // Establecer el estado de la orden a "Cancelada" en lugar de eliminarla
            var estadoCancelado = await _capibeadsDBContext.estadosPedidos
                                    .FirstOrDefaultAsync(e => e.nombreEstadoPedido == "Cancelado");

            if (estadoCancelado != null)
            {
                orden.id_estadoPedido = estadoCancelado.id_estadoPedido;
                _capibeadsDBContext.ordenes.Update(orden);
                await _capibeadsDBContext.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Pedidos));  // Redirige a la vista de órdenes
        }

        //AA: Aqui involutra TODO lo que tiene que ver con el perfil
        public IActionResult Perfil()
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            if (datosUsuario != null)
            {
                ViewBag.NombreUsuario = datosUsuario.nombre;
                ViewBag.CorreoUsuario = datosUsuario.correo;
                ViewBag.FotoUsuario = datosUsuario.foto != null
                    ? $"data:image/png;base64,{Convert.ToBase64String(datosUsuario.foto)}"
                    : null;
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CambiarFotoPerfil(IFormFile photoUpload)
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));

            if (datosUsuario == null || photoUpload == null || photoUpload.Length == 0)
            {
                TempData["Message"] = "Error al cargar la imagen.";
                return RedirectToAction("Perfil");
            }

            try
            {
                // Consultar el usuario de la base de datos para obtener todos sus campos
                var usuario = await _capibeadsDBContext.usuarios
                    .FirstOrDefaultAsync(u => u.id_usuario == datosUsuario.id_usuario);

                if (usuario == null)
                {
                    TempData["Message"] = "Usuario no encontrado.";
                    return RedirectToAction("Perfil");
                }

                // Guardar la foto en el servidor
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoUpload.FileName);
                string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "ProfileImg");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                string filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await photoUpload.CopyToAsync(stream);
                }

                // Actualizar la foto en el objeto usuario
                usuario.foto = System.IO.File.ReadAllBytes(filePath);
                _capibeadsDBContext.usuarios.Update(usuario);
                await _capibeadsDBContext.SaveChangesAsync();

                // Actualizar sesión con los datos completos
                HttpContext.Session.SetString("user", JsonSerializer.Serialize(usuario));

                TempData["Message"] = "Foto actualizada correctamente.";
            }
            catch (Exception)
            {
                TempData["Message"] = "Hubo un error al guardar la imagen.";
            }

            return RedirectToAction("Perfil");
        }

        [HttpPost]
        public async Task<IActionResult> CambiarContrasena(string currentPassword, string newPassword, string confirmNewPassword)
        {
            // Obtenemos los datos del usuario desde la sesión
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            if (datosUsuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction("Perfil");
            }

            // Verificar si la contraseña actual coincide
            var usuario = await _capibeadsDBContext.usuarios
                                    .FirstOrDefaultAsync(u => u.id_usuario == datosUsuario.id_usuario && u.contrasenya == currentPassword);

            if (usuario == null)
            {
                TempData["ErrorMessage"] = "La contraseña actual es incorrecta.";
                return RedirectToAction("Perfil");
            }

            // Validar que la nueva contraseña y la confirmación coincidan
            if (newPassword != confirmNewPassword)
            {
                TempData["ErrorMessage"] = "Las contraseñas nuevas no coinciden.";
                return RedirectToAction("Perfil");
            }

            // Actualizar la contraseña en la base de datos
            usuario.contrasenya = newPassword;
            _capibeadsDBContext.usuarios.Update(usuario);
            await _capibeadsDBContext.SaveChangesAsync();

            // Actualizar la sesión con los nuevos datos del usuario
            HttpContext.Session.SetString("user", JsonSerializer.Serialize(usuario));

            TempData["SuccessMessage"] = "Contraseña actualizada correctamente.";
            return RedirectToAction("Perfil");
        }
    }
}
