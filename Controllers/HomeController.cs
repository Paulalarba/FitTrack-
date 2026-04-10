using FitTrack.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FitTrack.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
