using Microsoft.AspNetCore.Mvc;
using CapiBeadsSV.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CapiBeadsSV.Controllers
{
    public class AdminController : Controller
    {
        private readonly capibeadsBDContext _context;

        public AdminController(capibeadsBDContext context)
        {
            _context = context;
        }

        // GET: Admin/Usuarios
        public async Task<IActionResult> IndexAdmin()
        {
            var usuarios = await _context.usuarios.ToListAsync();
            return View(usuarios);
        }

        // GET: Admin/CreateUsuario
        public IActionResult CreateUsuario()
        {
            return View();
        }

        // POST: Admin/CreateUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUsuario(usuarios usuario)
        {
            if (ModelState.IsValid)
            {
                _context.Add(usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(IndexAdmin));
            }
            return View(usuario);
        }

        // GET: Admin/EditUsuario/5
        public async Task<IActionResult> EditUsuario(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }
            return View(usuario);
        }

        // POST: Admin/EditUsuario/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUsuario(int id, usuarios usuario)
        {
            if (id != usuario.id_usuario)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(IndexAdmin));
            }
            return View(usuario);
        }

        // GET: Admin/DeleteUsuario/5
        public async Task<IActionResult> DeleteUsuario(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.usuarios
                .FirstOrDefaultAsync(m => m.id_usuario == id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // POST: Admin/DeleteUsuario/5
        [HttpPost, ActionName("DeleteUsuario")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.usuarios.FindAsync(id);
            _context.usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(IndexAdmin));
        }
    }
}
