using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CapiBeadsSV.Controllers
{
    public class VendedorController : Controller
    {
        // GET: VendedorController
        public ActionResult IndexVendedor()
        {
            return View();
        }

        // GET: VendedorController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: VendedorController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: VendedorController/Create
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

        // GET: VendedorController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: VendedorController/Edit/5
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

        // GET: VendedorController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: VendedorController/Delete/5
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
