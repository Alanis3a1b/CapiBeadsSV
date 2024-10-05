using Microsoft.AspNetCore.Mvc;
//using capibeadsBD.Models; esto para cuando ya este lista la base de datos

namespace CapiBeadsSV.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
