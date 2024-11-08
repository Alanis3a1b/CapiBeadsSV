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
                                m.contrasenya
                            }).FirstOrDefault();

            ViewBag.usuario = usuario;

            if (usuario == null)
            {
                return NotFound();
            }

            ViewData["Usuario"] = usuario;

            return View();
        }

        //AA: Funcion para eliminar
        public IActionResult DeleteUsuario(int? id)
        {
            var usuario = (from m in _capibeadsDBContext.usuarios
                                   where m.id_usuario == id
                                   select m).FirstOrDefault();
            if (usuario == null)
                return NotFound ();

            _capibeadsDBContext.usuarios.Attach(usuario);
            _capibeadsDBContext.usuarios.Remove(usuario);
            _capibeadsDBContext.SaveChanges();

            return RedirectToAction("IndexAdmin");
        }

    }
}
