using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FitTrack.Data;
using Microsoft.EntityFrameworkCore;
namespace FitTrack.Controllers;


[Authorize(Roles = "Admin")] // Only Admins can enter
public class AdminController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Dashboard()
    {
        var users = await context.Users.ToListAsync();
        return View(users);
    }
}