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
                                      t.descripcionTienda

                                  }).Take(5).ToList(); //Máximo de filas (tickets) a mostrar en el Home de Cliente

            ViewBag.Tiendas = tiendasUsuario;
            return View();
        }

        public async Task<IActionResult> Productos()
        {
            var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            var productoUsuario = (from p in _capibeadsDBContext.productos
                                   join t in _capibeadsDBContext.tiendas on p.id_tienda equals t.id_tienda
                                   join u in _capibeadsDBContext.usuarios on t.id_usuario equals u.id_usuario
                                   join c in _capibeadsDBContext.categorias on p.id_categoria equals c.id_categoria
                                   select new
                                   {
                                       p.id_producto,
                                       nombreTienda = t.nombreTienda,
                                       nombreProducto = p.nombreProducto,
                                       descripcionProd = p.descripcion,
                                       categoriaProd = c.nombreCategoria,
                                       precioProd = p.precio

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

        //En progreso, aun la vista tiendas no funciona
        public async Task<IActionResult> VerTienda(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tiendas = await (from p in _capibeadsDBContext.productos
                               join t in _capibeadsDBContext.tiendas on p.id_tienda equals t.id_tienda
                               where t.id_tienda == id
                               select new
                               {
                                   t.id_tienda,
                                   t.id_usuario,
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
                               }).FirstOrDefaultAsync();

            ViewBag.Tiendas = tiendas;

            return View();
        }

        //public IActionResult Productos()
        //{
        //    // Obtener el usuario desde la sesión
        //    var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));

        //    // Obtener las tiendas del usuario y sus productos
        //    var tiendas = await _capibeadsDBContext.tiendas
        //        .Where(t => t.id_usuario == usuarioSesion.id_usuario)
        //        .Select(t => new
        //        {
        //            t.id_tienda,
        //            t.nombreTienda,
        //            t.descripcionTienda,
        //            t.imagenFondo,
        //            Productos = _capibeadsDBContext.productos
        //                .Where(p => p.id_tienda == t.id_tienda)
        //                .Select(p => new
        //                {
        //                    p.id_producto,
        //                    p.nombreProducto,
        //                    p.precio,
        //                    p.imagenProducto,
        //                    CategoriaNombre = _capibeadsDBContext.categorias
        //                        .Where(c => c.id_categoria == p.id_categoria)
        //                        .Select(c => c.nombreCategoria)
        //                        .FirstOrDefault()
        //                }).ToList()
        //        }).ToListAsync();

        //    // Pasar las tiendas con sus productos a la vista
        //    ViewBag.Tiendas = tiendas;
        //    return View();
        //}

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

        public IActionResult Pedidos()
        {

            return View();
        }

    }
}
