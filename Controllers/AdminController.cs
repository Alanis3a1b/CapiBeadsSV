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

        public IActionResult Categorias()
        {

            return View();
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
                               UsuarioNombre = u.nombre
                           }).ToList();

            ViewBag.tiendas = tiendas;
            return View();
        }
        // GET: Admin/CreateTienda
        public IActionResult CreateTienda()
        {
            var usuarios = (from u in _capibeadsDBContext.usuarios
                            select new { u.id_usuario, u.nombre }).ToList();
            ViewData["Usuarios"] = new SelectList(usuarios, "id_usuario", "nombre");

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
            return RedirectToAction("Success");
        }

        // GET: Admin/EditTienda/5
        public async Task<IActionResult> EditTienda(int? id)
        {
            if (id == null) return NotFound();

            var tienda = await _capibeadsDBContext.tiendas.FindAsync(id);
            if (tienda == null) return NotFound();

            var usuarios = (from u in _capibeadsDBContext.usuarios
                            select new { u.id_usuario, u.nombre }).ToList();
            ViewData["Usuarios"] = new SelectList(usuarios, "id_usuario", "nombre", tienda.id_usuario);

            ViewBag.tienda = tienda;
            return View();
        }

        // POST: Admin/EditTienda/5
        [HttpPost]
        public async Task<IActionResult> EditTienda(int id, tiendas tiendaModificada, IFormFile imagenFondo)
        {
            if (id != tiendaModificada.id_tienda) return NotFound();

            var tienda = await _capibeadsDBContext.tiendas.FindAsync(id);
            if (tienda == null) return NotFound();

            tienda.nombreTienda = tiendaModificada.nombreTienda;
            tienda.descripcionTienda = tiendaModificada.descripcionTienda;
            tienda.id_usuario = tiendaModificada.id_usuario;

            if (imagenFondo != null && imagenFondo.Length > 0)
            {
                using (var ms = new System.IO.MemoryStream())
                {
                    imagenFondo.CopyTo(ms);
                    tienda.imagenFondo = ms.ToArray();
                }
            }

            _capibeadsDBContext.Entry(tienda).State = EntityState.Modified;
            await _capibeadsDBContext.SaveChangesAsync();

            return RedirectToAction("SuccessModificar");
        }

        // GET: Admin/DeleteTienda/5
        public async Task<IActionResult> DeleteTienda(int? id)
        {
            if (id == null) return NotFound();

            var tienda = (from t in _capibeadsDBContext.tiendas
                          join u in _capibeadsDBContext.usuarios on t.id_usuario equals u.id_usuario
                          where t.id_tienda == id
                          select new
                          {
                              t.id_tienda,
                              t.nombreTienda,
                              t.descripcionTienda,
                              UsuarioNombre = u.nombre
                          }).FirstOrDefault();

            if (tienda == null) return NotFound();

            ViewBag.tienda = tienda;
            return View();
        }

        // POST: Admin/ConfirmDeleteTienda/5
        [HttpPost, ActionName("ConfirmDeleteTienda")]
        public async Task<IActionResult> ConfirmDeleteTienda(int id)
        {
            var tienda = await _capibeadsDBContext.tiendas.FindAsync(id);
            if (tienda == null) return NotFound();

            _capibeadsDBContext.tiendas.Remove(tienda);
            await _capibeadsDBContext.SaveChangesAsync();

            TempData["Mensaje"] = "Tienda eliminada correctamente.";
            return RedirectToAction("IndexTiendas");
        }
        public IActionResult Productos()
        {

            return View();
        }

        public IActionResult Pedidos()
        {

            return View();
        }
    }
}
