using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly CartService _cartService;
        private readonly OrderService _orderService;
        private readonly OrderProcessingQueueService _queueService;
        private readonly AzureFileService _fileService;

        public CheckoutController(CartService cartService, OrderService orderService, 
            OrderProcessingQueueService queueService, AzureFileService fileService)
        {
            _cartService = cartService;
            _orderService = orderService;
            _queueService = queueService;
            _fileService = fileService;
        }

        [HttpPost]
        public async Task<IActionResult> ProceedToCheckout()
        {
            try
            {
                Console.WriteLine("DEBUG: Starting checkout process...");
                
                var email = HttpContext.Session.GetString("CustomerEmail");
                if (string.IsNullOrEmpty(email))
                {
                    Console.WriteLine("DEBUG: No customer email in session");
                    TempData["Error"] = "Please log in to proceed with checkout.";
                    return RedirectToAction("Login", "Customer");
                }

                Console.WriteLine($"DEBUG: Customer email: {email}");

                var cartItems = await _cartService.GetCartAsync(email);
                if (!cartItems.Any())
                {
                    Console.WriteLine("DEBUG: Cart is empty");
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("ViewCart", "CustomerCart");
                }

                Console.WriteLine($"DEBUG: Found {cartItems.Count} items in cart");

                var total = cartItems.Sum(item => item.Price * item.Quantity);
                Console.WriteLine($"DEBUG: Order total: {total:C}");

                // Place order in database
                var orderId = await _orderService.PlaceOrderAsync(email, cartItems, total);
                Console.WriteLine($"DEBUG: Order placed with ID: {orderId}");

                // Create order processing message
                var orderMessage = new OrderProcessingMessage
                {
                    OrderId = orderId,
                    CustomerId = email,
                    Action = "ProcessOrder",
                    TotalAmount = total,
                    Items = cartItems.Select(item => new OrderItem
                    {
                        ProductId = item.RowKey,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Price = item.Price
                    }).ToList()
                };

                Console.WriteLine("DEBUG: Enqueueing order processing message...");
                await _queueService.EnqueueOrderProcessingAsync(orderMessage);
                Console.WriteLine("DEBUG: Order processing message enqueued successfully");

                // Queue order lifecycle message for "Placed" status
                var lifecycleMessage = new OrderLifecycleMessage
                {
                    OrderId = orderId,
                    CustomerId = email,
                    Status = "Placed",
                    PreviousStatus = "Cart",
                    TotalAmount = total,
                    Notes = $"Order placed with {cartItems.Count} items"
                };
                
                Console.WriteLine("DEBUG: Enqueueing order lifecycle message...");
                await _queueService.EnqueueOrderLifecycleAsync(lifecycleMessage);
                Console.WriteLine("DEBUG: Order lifecycle message enqueued successfully");

                // Queue inventory updates for each item
                Console.WriteLine("DEBUG: Enqueueing inventory updates...");
                foreach (var item in cartItems)
                {
                    var inventoryMessage = new InventoryUpdateMessage
                    {
                        ProductId = item.RowKey,
                        Action = "ReserveStock",
                        Quantity = item.Quantity,
                        Reason = $"Stock reserved for order {orderId}",
                        Timestamp = DateTime.UtcNow
                    };
                    
                    await _queueService.EnqueueInventoryUpdateAsync(inventoryMessage);
                    Console.WriteLine($"DEBUG: Reserved {item.Quantity} units of {item.ProductName} (ID: {item.RowKey})");
                }
                Console.WriteLine("DEBUG: All inventory updates enqueued successfully");

                // Log the automatic queuing
                Console.WriteLine("DEBUG: Uploading log file...");
                await _fileService.UploadLogFileAsync(
                    "checkout_orders.log",
                    $"Order {orderId} automatically queued after checkout - Customer: {email}, Amount: {total:C}, Items: {cartItems.Count} at {DateTime.UtcNow}",
                    "orders");
                Console.WriteLine("DEBUG: Log file uploaded successfully");

                // Clear cart
                Console.WriteLine("DEBUG: Clearing cart...");
                await _cartService.ClearCartAsync(email);
                
                // Update session cart count
                HttpContext.Session.SetInt32("CartCount", 0);
                Console.WriteLine("DEBUG: Cart cleared successfully");

                TempData["Success"] = $"Order {orderId} placed successfully and queued for processing!";
                Console.WriteLine($"DEBUG: Checkout completed successfully for order {orderId}");
                
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Checkout failed - {ex.Message}");
                Console.WriteLine($"ERROR: Stack trace - {ex.StackTrace}");
                TempData["Error"] = "An error occurred during checkout. Please try again.";
                return RedirectToAction("ViewCart", "CustomerCart");
            }
        }

    }
}
