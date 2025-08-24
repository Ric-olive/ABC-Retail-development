using ABC_Retail.Models;
using ABC_Retail.Models.ViewModels;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace ABC_Retail.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }
        public async Task<IActionResult> Seed()
        {
            var email = "admin@example.com";
            var plainPassword = "123456";

            var admin = new Admin
            {
                RowKey = email.ToLower(),
                PartitionKey = "Admin",
                FullName = "System Administrator",
                Email = email,
                PasswordHash = HashPassword(plainPassword),
                CreatedOn = DateTime.UtcNow,
                IsActive = true
            };

            await _adminService.AddAdminAsync(admin);
            TempData["Message"] = "✅ Admin seeded successfully.";
            return RedirectToAction("Login");
        }
        public IActionResult Login()
        {
            return View(); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginAdminViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var admin = await _adminService.LoginAdminAsync(
                model.Email.ToLower().Trim(), model.Password);

            if (admin == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            HttpContext.Session.SetString("AdminEmail", admin.Email);
            TempData["SuccessMessage"] = "Welcome, Admin!";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clear all session data
            TempData["SuccessMessage"] = "You have been logged out.";
            return RedirectToAction("Login", "Admin");
        }
    }
}
