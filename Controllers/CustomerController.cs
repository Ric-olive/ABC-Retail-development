using ABC_Retail.Models;
using ABC_Retail.Models.ViewModels;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class CustomerController : Controller
    {
        private readonly CustomerService _customerService;

        public CustomerController(CustomerService customerService)
        {
            _customerService = customerService;
        }
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterCustomerViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Create CustomerEntity from ViewModel
            var customer = new Customer
            {
                PartitionKey = "Customer",
                RowKey = model.Email.ToLower(), // Email as unique RowKey
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = model.Password,   // Will be hashed inside service
                RegisteredOn = DateTime.UtcNow,
                IsActive = true
            };

            var success = await _customerService.RegisterCustomerAsync(customer);

            if (!success)
            {
                ModelState.AddModelError("", "Email already registered.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Registration successful!";
            return RedirectToAction("Index", "Home");
        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginCustomerViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var customer = await _customerService.LoginCustomerAsync(
                model.Email.ToLower().Trim(), model.Password);

            if (customer == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }
            HttpContext.Session.SetString("CustomerRowKey", customer.RowKey);
            HttpContext.Session.SetString("CustomerEmail", customer.Email);
            HttpContext.Session.SetString("IsCustomerLoggedIn", "true");
            TempData["SuccessMessage"] = "Login successful!";
            return RedirectToAction("Index", "Product");

        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Clear all session data
            TempData["SuccessMessage"] = "You have been logged out.";
            return RedirectToAction("Login", "Customer");
        }

    }
}
