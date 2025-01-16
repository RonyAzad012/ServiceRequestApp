using Microsoft.AspNetCore.Mvc;
using ServiceRequestApp.Models;
using System.Diagnostics;

namespace ServiceRequestApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}