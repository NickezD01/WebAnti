using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
    public class QRController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
