using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;

        public OrderController(OrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Please log in to view your orders.";
                return RedirectToAction("Login", "Customer");
            }

            var orders = await _orderService.GetOrdersByCustomerAsync(email);
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Please log in to view order details.";
                return RedirectToAction("Login", "Customer");
            }

            var order = await _orderService.GetOrderAsync(id);
            if (order == null || order.PartitionKey != email)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index");
            }

            return View(order);
        }
    }
}
