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

        //Solamente tomara los productos que pertenezcan exclusivamente a las tiendas del usuario vendedor en cuestion
        public ActionResult Productos()
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

        public ActionResult Perfil()
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            ViewBag.NombreUsuario = datosUsuario.nombre;
            ViewBag.CorreoUsuario = datosUsuario.correo;
            ViewBag.FotoUsuario = datosUsuario.foto;

            //if (datosUsuario.foto != null)
            //{
            //    string base64Image = Convert.ToBase64String(datosUsuario.foto);
            //    ViewBag.FotoUsuario = $"data:image/png;base64,{base64Image}";
            //}
            //else if (!string.IsNullOrEmpty(datosUsuario.usuario))
            //{
            //    ViewBag.FotoUsuario = "/" + datosUsuario.usuario.Replace("\\", "/");
            //}
            //else
            //{
            //    ViewBag.FotoUsuario = null;
            //}

            return View();
        }

        private async Task<string> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "ProfileImg");

            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            string filePath = Path.Combine(uploadPath, fileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (Exception)
            {
                return null;
            }

            return Path.Combine("ProfileImg", fileName);
        }


        //Se supone que funciona pero no guarda nada
        [HttpPost]
        public async Task<IActionResult> CambiarFotoPerfil(IFormFile photoUpload)
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
            var usuarioF = await _userService.GetCurrentUserAsync();

            if (photoUpload != null && photoUpload.Length > 0)
            {
                if (!string.IsNullOrEmpty(usuarioF.usuario))
                {
                    string existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath, usuarioF.usuario);
                    if (System.IO.File.Exists(existingFilePath))
                    {
                        System.IO.File.Delete(existingFilePath);
                    }
                }

                string newFilePath = await UploadPhoto(photoUpload);
                if (!string.IsNullOrEmpty(newFilePath))
                {
                    usuarioF.foto = System.IO.File.ReadAllBytes(Path.Combine(_webHostEnvironment.WebRootPath, newFilePath));
                    usuarioF.usuario = newFilePath;
                }

                _capibeadsDBContext.Update(usuarioF);
                await _capibeadsDBContext.SaveChangesAsync();

                // Actualizar el objeto de usuario en la sesión
                var usuarioSesion = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));
                if (newFilePath != null)
                {
                    usuarioSesion.usuario = newFilePath; // Actualizar la ruta en el objeto de sesión
                }
                HttpContext.Session.SetString("user", JsonSerializer.Serialize(usuarioSesion));

                return Json(new { success = true, newImageUrl = "/" + newFilePath.Replace("\\", "/") });
            }

            return Json(new { success = false, message = "No se proporcionó ninguna imagen para actualizar." });
        }

        [HttpPost]
        public async Task<IActionResult> CambiarContrasena(string currentPassword, string newPassword, string confirmNewPassword)
        {
            var datosUsuario = JsonSerializer.Deserialize<usuarios>(HttpContext.Session.GetString("user"));

            var usuario = _capibeadsDBContext.usuarios.FirstOrDefault(u => u.contrasenya == currentPassword);

            if (usuario == null)
            {
                ModelState.AddModelError("currentPassword", "La contraseña actual es incorrecta.");
                return View("Settings");
            }

            if (!string.IsNullOrEmpty(newPassword) && newPassword != confirmNewPassword)
            {
                ModelState.AddModelError("confirmNewPassword", "Las contraseñas no coinciden.");
                return View("Settings");
            }

            if (!string.IsNullOrEmpty(newPassword))
            {
                usuario.contrasenya = newPassword;
                _capibeadsDBContext.Update(usuario);
                await _capibeadsDBContext.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Contraseña actualizada correctamente." });
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
        public IActionResult VerTienda(int? id)
        {
            var tienda = (from t in _capibeadsDBContext.tiendas
                               where t.id_tienda == id
                               select new
                               {
                                   t.id_tienda,
                                   t.id_usuario,
                                   t.nombreTienda,
                                   t.imagenFondo,
                                   t.descripcionTienda
                               }).FirstOrDefaultAsync();
            return View();
        }


    }
}
