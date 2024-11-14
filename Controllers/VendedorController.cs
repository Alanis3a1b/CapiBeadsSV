using Microsoft.AspNetCore.Mvc;
using CapiBeadsSV.Models;
using CapiBeadsSV.Serv;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CapiBeadsSV.Controllers
{
    public class VendedorController : Controller
    {
        private readonly capibeadsBDContext _capibeadsDBContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IUserService _userService;

        public VendedorController(capibeadsBDContext capibeadsDBContext, IWebHostEnvironment webHostEnvironment, IUserService userService)
        {
            _capibeadsDBContext = capibeadsDBContext;
            _webHostEnvironment = webHostEnvironment;
            _userService = userService;
        }

        public ActionResult IndexVendedor()
        {
            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            var tiendasUsuario = (from t in _capibeadsDBContext.tiendas
                                  join u in _capibeadsDBContext.usuarios on t.id_usuario equals u.id_usuario
                                  where u.usuario == usuarioSesion.usuario
                                  select new
                                  {
                                      t.id_tienda,
                                      t.nombreTienda,
                                      t.descripcionTienda,
                                      u.usuario

                                  }).Take(5).ToList(); //Máximo de filas (tickets) a mostrar en el Home de Cliente

            ViewBag.Usuario = usuarioSesion;
            ViewBag.Tiendas = tiendasUsuario;
            return View();
        }

        public async Task<IActionResult> Productos()
        {
            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            var productoUsuario = (from p in _capibeadsDBContext.productos
                                   join t in _capibeadsDBContext.tiendas on p.id_tienda equals t.id_tienda
                                   join e in _capibeadsDBContext.estados on p.id_estadoProducto equals e.id_estadoProducto
                                   join c in _capibeadsDBContext.categorias on p.id_categoria equals c.id_categoria
                                   where t.id_usuario == usuarioSesion.id_usuario
                                   select new
                                   {
                                       p.id_producto,
                                       p.nombreProducto,
                                       p.descripcion,
                                       p.precio,
                                       p.imagenProducto,
                                       CategoriaNombre = c.nombreCategoria,
                                       t.nombreTienda,
                                       EstadoProducto = e.nombreEstado
                                   }).ToList();

            ViewBag.Productos = productoUsuario;
            return View();
        }

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

        public async Task<IActionResult> DeleteTienda(int? id)
        {
            if (id == null) return NotFound();

            var tienda = await (from t in _capibeadsDBContext.tiendas                                  
                                where t.id_tienda == id
                                  select new
                                  {
                                      t.id_tienda,
                                      t.id_usuario,
                                      t.nombreTienda,
                                      t.imagenFondo,
                                      t.descripcionTienda
                                  }).FirstOrDefaultAsync();

            if (tienda == null) return NotFound();

            ViewBag.tiendas = tienda;
            return View();
        }

        //Confirmar la eliminacion de una tienda
        public IActionResult ConfirmDelete(int? id)
        {
            var tienda = _capibeadsDBContext.tiendas.FirstOrDefault(m => m.id_tienda == id);

            if (tienda == null)
                return NotFound();

            _capibeadsDBContext.tiendas.Remove(tienda);
            _capibeadsDBContext.SaveChanges();

            TempData["Mensaje"] = "Tienda eliminada correctamente.";
            return RedirectToAction("IndexVendedor");
        }

        //Crear tienda para los vendedores
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

        public async Task<IActionResult> VerTienda(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Consulta para obtener la tienda y sus productos
            var tienda = await _capibeadsDBContext.tiendas
                .Where(t => t.id_tienda == id)
                .Select(t => new
                {
                    t.id_tienda,
                    t.nombreTienda,
                    t.imagenFondo,
                    t.descripcionTienda,
                    Productos = _capibeadsDBContext.productos
                        .Where(p => p.id_tienda == t.id_tienda)
                        .Select(p => new
                        {
                            p.id_producto,
                            p.nombreProducto,
                            p.precio,
                            p.imagenProducto,
                            CategoriaNombre = _capibeadsDBContext.categorias
                                .Where(c => c.id_categoria == p.id_categoria)
                                .Select(c => c.nombreCategoria)
                                .FirstOrDefault()
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (tienda == null)
            {
                return NotFound();
            }

            ViewBag.Tienda = tienda;
            return View();
        }

        public IActionResult CreateProducto()
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            if (datosUsuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction("Perfil");
            }

            var listaTiendasporUsuario = (from m in _capibeadsDBContext.tiendas
                                          where m.id_usuario == datosUsuario.id_usuario
                                select m).ToList();

            ViewData["Tiendas"] = new SelectList(listaTiendasporUsuario, "id_tienda", "nombreTienda");

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
        public IActionResult Pedidos()
        {
            // Obtiene los datos del usuario desde la sesión
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            if (datosUsuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction("Perfil");
            }

            // Obtiene las tiendas que pertenecen al usuario logueado
            var tiendasUsuario = _capibeadsDBContext.tiendas
                                        .Where(t => t.id_usuario == datosUsuario.id_usuario)
                                        .Select(t => t.id_tienda)
                                        .ToList();

            // Obtiene los productos que pertenecen a esas tiendas
            var productosTienda = _capibeadsDBContext.productos
                                         .Where(p => tiendasUsuario.Contains(p.id_tienda))
                                         .Select(p => p.id_producto)
                                         .ToList();

            // Obtiene las órdenes y detalles de productos asociados a esos productos
            var ordenesUsuario = (from o in _capibeadsDBContext.ordenes
                                  join u in _capibeadsDBContext.usuarios on o.id_usuario equals u.id_usuario
                                  where _capibeadsDBContext.ordenItems
                                         .Where(oi => productosTienda.Contains(oi.id_producto))
                                         .Select(oi => oi.id_orden)
                                         .Contains(o.id_orden)
                                  select new
                                  {
                                      Orden = o,
                                      UsuarioNombre = u.nombre, // Se obtiene el nombre del usuario
                                      Productos = _capibeadsDBContext.ordenItems
                                                 .Where(oi => oi.id_orden == o.id_orden)
                                                 .Select(oi => new
                                                 {
                                                     Producto = _capibeadsDBContext.productos
                                                               .FirstOrDefault(p => p.id_producto == oi.id_producto).nombreProducto,
                                                     Tienda = _capibeadsDBContext.tiendas
                                                               .FirstOrDefault(t => t.id_tienda == _capibeadsDBContext.productos
                                                                                                      .FirstOrDefault(p => p.id_producto == oi.id_producto).id_tienda).nombreTienda,
                                                     Cantidad = oi.cantidad,
                                                     PrecioUnitario = oi.precio_unitario,
                                                     ImagenProducto = _capibeadsDBContext.productos
                                                                .FirstOrDefault(p => p.id_producto == oi.id_producto).imagenProducto
                                                 }).ToList()
                                  }).ToList();

            // Pasa los datos de las órdenes y productos a la vista
            return View(ordenesUsuario);
        }

        //AA: Funciones para editar usuarios
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

        //Metodos para editar los productos
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

            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            if (datosUsuario == null)
            {
                TempData["ErrorMessage"] = "Usuario no encontrado.";
                return RedirectToAction("Perfil");
            }

            //Los limite a que sean solo para las tiendas del usuario vendedor haya creado
            var listaTiendasporUsuario = (from m in _capibeadsDBContext.tiendas
                                          where m.id_usuario == datosUsuario.id_usuario
                                          select m).ToList();

            ViewData["Tiendas"] = new SelectList(listaTiendasporUsuario, "id_tienda", "nombreTienda");

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

        //DELET PRODUCTOS
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

    }
}
