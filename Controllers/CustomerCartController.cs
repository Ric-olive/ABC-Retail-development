using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class CustomerCartController : Controller
    {
        private readonly CartService _cartService;
        private readonly ProductService _productService;

        public CustomerCartController(CartService cartService, ProductService productService)
        {
            _cartService = cartService;
            _productService = productService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(string productId, string productName, decimal price, int quantity = 1)
        {
            var email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Please log in to use the cart.";
                return RedirectToAction("Login", "Customer");
            }

            try
            {
                var product = await _productService.GetProductAsync(productId);
                if (product == null) 
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index", "Product");
                }

                if (product.StockQty <= 0)
                {
                    TempData["Error"] = $"{product.Name} is out of stock.";
                    return RedirectToAction("Index", "Product");
                }

                await _cartService.AddToCartAsync(product, quantity, email);
                
                // Update cart count in session
                var cartItems = await _cartService.GetCartAsync(email);
                var totalItems = cartItems.Sum(item => item.Quantity);
                HttpContext.Session.SetInt32("CartCount", totalItems);
                
                TempData["Success"] = $"{product.Name} added to cart successfully!";
                
                // Always return JSON for AJAX requests from home page
                return Json(new { 
                    success = true, 
                    message = $"{product.Name} added to cart!", 
                    cartCount = totalItems 
                });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to add item to cart. Please try again.";
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                    Request.ContentType?.Contains("application/json") == true)
                {
                    return Json(new { success = false, message = "Failed to add item to cart." });
                }
                
                return RedirectToAction("Index", "Product");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewCart()
        {
            var email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Please log in to view your cart.";
                return RedirectToAction("Login", "Customer");
            }

            var cartItems = await _cartService.GetCartAsync(email);
            // Debug log each item
            foreach (var item in cartItems)
            {
                Console.WriteLine($"CartItem: {item.ProductName}, Quantity = {item.Quantity}, Price = {item.Price}");
            }

            return View(cartItems); // Assumes you have a View for this
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = true, cartCount = 0 });
            }

            try
            {
                var cartItems = await _cartService.GetCartAsync(email);
                var totalItems = cartItems.Sum(item => item.Quantity);
                HttpContext.Session.SetInt32("CartCount", totalItems);
                
                return Json(new { success = true, cartCount = totalItems });
            }
            catch
            {
                return Json(new { success = true, cartCount = 0 });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(string productId)
        {
            var email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Please log in to modify your cart." });
            }

            try
            {
                await _cartService.RemoveFromCartAsync(productId, email);
                
                // Update cart count in session
                var cartItems = await _cartService.GetCartAsync(email);
                var totalItems = cartItems.Sum(item => item.Quantity);
                HttpContext.Session.SetInt32("CartCount", totalItems);
                
                return Json(new { 
                    success = true, 
                    message = "Item removed from cart", 
                    cartCount = totalItems 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to remove item from cart." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(string productId, int quantity)
        {
            var email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Please log in to modify your cart." });
            }

            if (quantity <= 0)
            {
                return await RemoveFromCart(productId);
            }

            try
            {
                await _cartService.UpdateQuantityAsync(productId, quantity, email);
                
                // Update cart count in session
                var cartItems = await _cartService.GetCartAsync(email);
                var totalItems = cartItems.Sum(item => item.Quantity);
                HttpContext.Session.SetInt32("CartCount", totalItems);
                
                return Json(new { 
                    success = true, 
                    message = "Quantity updated", 
                    cartCount = totalItems 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to update quantity." });
            }
        }

    }
}
