using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
//using capibeadsBD.Models; esto para cuando ya este lista la base de datos
using System.Text.Json;

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
