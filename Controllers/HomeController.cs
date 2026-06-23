using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudyGo.Models; // Asegúrate de que apunte a tu carpeta de Modelos si usas el ErrorViewModel

namespace StudyGo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Acción para la página principal: http://localhost:5131/ o /Home/Index
        public IActionResult Index()
        {
            return View();
        }

        // Acción para la página de privacidad: http://localhost:5131/Home/Privacy
        public IActionResult Privacy()
        {
            return View();
        }

        // Acción para el editor Monaco: http://localhost:5131/Home/Editor
        public IActionResult Editor()
        {
            return View();
        }
    }
}