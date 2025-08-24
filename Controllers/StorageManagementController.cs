using ABC_Retail.Models;
using ABC_Retail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail.Controllers
{
    public class StorageManagementController : Controller
    {
        private readonly ProductService _productService;
        private readonly CustomerService _customerService;
        private readonly BlobImageService _blobImageService;
        private readonly OrderProcessingQueueService _queueService;
        private readonly AdminActivityQueueService _adminActivityQueueService;
        private readonly AzureFileService _fileService;

        public StorageManagementController(
            ProductService productService,
            CustomerService customerService,
            BlobImageService blobImageService,
            OrderProcessingQueueService queueService,
            AdminActivityQueueService adminActivityQueueService,
            AzureFileService fileService)
        {
            _productService = productService;
            _customerService = customerService;
            _blobImageService = blobImageService;
            _queueService = queueService;
            _adminActivityQueueService = adminActivityQueueService;
            _fileService = fileService;
        }

        // Main dashboard view
        public async Task<IActionResult> Index()
        {
            var (orderQueueLength, inventoryQueueLength, lifecycleQueueLength) = await _queueService.GetQueueLengthsAsync();
            var adminActivityQueueLength = await _adminActivityQueueService.GetQueueLengthAsync();
            
            ViewBag.OrderQueueLength = orderQueueLength;
            ViewBag.InventoryQueueLength = inventoryQueueLength;
            ViewBag.LifecycleQueueLength = lifecycleQueueLength;
            ViewBag.AdminActivityQueueLength = adminActivityQueueLength;
            
            var logFiles = await _fileService.ListLogFilesAsync();
            ViewBag.LogFiles = logFiles;
            
            return View();
        }

        #region Table Storage Operations

        // Product Management
        [HttpGet]
        public IActionResult AddProduct()
        {
            return View(new Product());
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product)
        {
            // Debug: Log that we received the form submission
            Console.WriteLine($"DEBUG: AddProduct POST received for product: {product?.Name ?? "NULL"}");
            
            if (!ModelState.IsValid)
            {
                // Debug: Log validation errors
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"DEBUG: ModelState Error: {error.ErrorMessage}");
                }
                TempData["Error"] = "Please fix the validation errors and try again.";
                return View(product);
            }

            try
            {
                product.PartitionKey = "Retail";
                // Generate RowKey if not provided or empty
                if (string.IsNullOrWhiteSpace(product.RowKey))
                {
                    product.RowKey = Guid.NewGuid().ToString();
                }
                
                // If an image file was uploaded, push it to Blob Storage and set ImageUrl
                if (product.ImageFile != null && product.ImageFile.Length > 0)
                {
                    try
                    {
                        using var stream = product.ImageFile.OpenReadStream();
                        var contentType = product.ImageFile.ContentType;
                        var fileName = product.ImageFile.FileName;
                        var imageUrl = await _blobImageService.UploadImageAsync(stream, fileName, contentType);
                        product.ImageUrl = imageUrl;
                        Console.WriteLine($"DEBUG: Image uploaded for product {product.RowKey}: {imageUrl}");
                    }
                    catch (Exception imgEx)
                    {
                        Console.WriteLine($"DEBUG: Image upload failed: {imgEx.Message}");
                        ModelState.AddModelError("ImageFile", "Image upload failed. Please try again.");
                        return View(product);
                    }
                }

                Console.WriteLine($"DEBUG: Attempting to save product with RowKey: {product.RowKey}");
                
                await _productService.AddProductAsync(product);
                
                Console.WriteLine($"DEBUG: Product saved successfully to Azure Tables");

                // Queue admin activity message
                var adminActivity = new AdminActivityMessage
                {
                    AdminId = User.Identity?.Name ?? "system",
                    Action = "CreateProduct",
                    EntityType = "Product",
                    EntityId = product.RowKey,
                    Details = $"Created product '{product.Name}' with price {product.Price:C}",
                    Metadata = new Dictionary<string, object>
                    {
                        ["ProductName"] = product.Name,
                        ["Category"] = product.Category,
                        ["Price"] = product.Price,
                        ["StockQty"] = product.StockQty,
                        ["HasImage"] = !string.IsNullOrEmpty(product.ImageUrl)
                    }
                };
                await _adminActivityQueueService.EnqueueAdminActivityAsync(adminActivity);

                // Queue inventory update message
                var inventoryMessage = new InventoryUpdateMessage
                {
                    ProductId = product.RowKey,
                    Action = "InitialStock",
                    Quantity = product.StockQty,
                    Reason = $"Initial stock for new product '{product.Name}'"
                };
                await _queueService.EnqueueInventoryUpdateAsync(inventoryMessage);
                
                // Log the operation
                try
                {
                    Console.WriteLine($"DEBUG: Attempting to upload log file for product: {product.Name}");
                    var logPath = await _fileService.UploadLogFileAsync(
                        "product_operations.log",
                        $"Product added: {product.Name} (ID: {product.RowKey}) at {DateTime.UtcNow}",
                        "products");
                    Console.WriteLine($"DEBUG: Log file created successfully at: {logPath}");
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"DEBUG: Log file creation failed: {logEx.Message}");
                    Console.WriteLine($"DEBUG: Log exception stack trace: {logEx.StackTrace}");
                    // Don't fail the whole operation if logging fails
                }
                
                TempData["Success"] = $"Product '{product.Name}' added successfully with ID: {product.RowKey}";
                return RedirectToAction(nameof(ViewProducts));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Exception occurred: {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Error adding product: {ex.Message}");
                TempData["Error"] = $"Failed to add product: {ex.Message}";
                return View(product);
            }
        }

        // Product Edit functionality
        [HttpGet]
        public async Task<IActionResult> EditProduct(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Product ID is required.";
                return RedirectToAction(nameof(ViewProducts));
            }

            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction(nameof(ViewProducts));
                }
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading product: {ex.Message}";
                return RedirectToAction(nameof(ViewProducts));
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(Product product)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fix the validation errors and try again.";
                return View(product);
            }

            try
            {
                // Get the original product for comparison
                var originalProduct = await _productService.GetProductByIdAsync(product.RowKey);
                if (originalProduct == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction(nameof(ViewProducts));
                }

                // Handle image upload if provided
                if (product.ImageFile != null && product.ImageFile.Length > 0)
                {
                    try
                    {
                        using var stream = product.ImageFile.OpenReadStream();
                        var contentType = product.ImageFile.ContentType;
                        var fileName = product.ImageFile.FileName;
                        var imageUrl = await _blobImageService.UploadImageAsync(stream, fileName, contentType);
                        product.ImageUrl = imageUrl;
                        Console.WriteLine($"DEBUG: New image uploaded for product {product.RowKey}: {imageUrl}");
                    }
                    catch (Exception imgEx)
                    {
                        Console.WriteLine($"DEBUG: Image upload failed: {imgEx.Message}");
                        ModelState.AddModelError("ImageFile", "Image upload failed. Please try again.");
                        return View(product);
                    }
                }
                else
                {
                    // Keep the existing image URL
                    product.ImageUrl = originalProduct.ImageUrl;
                }

                // Ensure ETag is preserved from original product
                product.ETag = originalProduct.ETag;
                product.Timestamp = originalProduct.Timestamp;
                
                // Update the product
                await _productService.UpdateProductAsync(product);

                // Queue admin activity message
                var adminActivity = new AdminActivityMessage
                {
                    AdminId = User.Identity?.Name ?? "system",
                    Action = "UpdateProduct",
                    EntityType = "Product",
                    EntityId = product.RowKey,
                    Details = $"Updated product '{product.Name}' - Price: {product.Price:C}, Stock: {product.StockQty}",
                    Metadata = new Dictionary<string, object>
                    {
                        ["ProductName"] = product.Name,
                        ["OldPrice"] = originalProduct.Price,
                        ["NewPrice"] = product.Price,
                        ["OldStock"] = originalProduct.StockQty,
                        ["NewStock"] = product.StockQty,
                        ["ImageUpdated"] = product.ImageFile != null && product.ImageFile.Length > 0
                    }
                };
                await _adminActivityQueueService.EnqueueAdminActivityAsync(adminActivity);

                // Queue inventory update message if stock changed
                if (originalProduct.StockQty != product.StockQty)
                {
                    var stockDifference = product.StockQty - originalProduct.StockQty;
                    var inventoryMessage = new InventoryUpdateMessage
                    {
                        ProductId = product.RowKey,
                        Action = stockDifference > 0 ? "StockIncrease" : "StockDecrease",
                        Quantity = Math.Abs(stockDifference),
                        Reason = $"Product update - Stock changed from {originalProduct.StockQty} to {product.StockQty}"
                    };
                    await _queueService.EnqueueInventoryUpdateAsync(inventoryMessage);
                }

                // Log the operation
                await _fileService.UploadLogFileAsync(
                    "product_operations.log",
                    $"Product updated: {product.Name} (ID: {product.RowKey}) - Price: {originalProduct.Price:C} -> {product.Price:C}, Stock: {originalProduct.StockQty} -> {product.StockQty} at {DateTime.UtcNow}",
                    "products");

                TempData["Success"] = $"Product '{product.Name}' updated successfully!";
                return RedirectToAction(nameof(ViewProducts));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Exception occurred during product update: {ex.Message}");
                TempData["Error"] = $"Failed to update product: {ex.Message}";
                return View(product);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewProducts()
        {
            try
            {
                var products = await _productService.GetProductsAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading products: {ex.Message}";
                return View(new List<Product>());
            }
        }

        // Customer Profile Management
        [HttpGet]
        public IActionResult AddCustomer()
        {
            return View(new Customer());
        }

        [HttpPost]
        public async Task<IActionResult> AddCustomer(Customer customer)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    customer.PartitionKey = "Customer";
                    customer.RowKey = customer.Email;
                    
                    var success = await _customerService.RegisterCustomerAsync(customer);
                    if (success)
                    {
                        // Log the operation
                        await _fileService.UploadLogFileAsync(
                            "customer_operations.log",
                            $"Customer registered: {customer.Email} at {DateTime.UtcNow}",
                            "customers");
                        
                        TempData["Success"] = "Customer added successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        ModelState.AddModelError("", "Customer with this email already exists.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error adding customer: {ex.Message}");
                }
            }
            return View(customer);
        }

        #endregion

        #region Blob Storage Operations

        [HttpGet]
        public IActionResult UploadImage()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    using var stream = imageFile.OpenReadStream();
                    var imageUrl = await _blobImageService.UploadImageAsync(
                        stream, 
                        imageFile.FileName, 
                        imageFile.ContentType);
                    
                    // Log the operation
                    await _fileService.UploadLogFileAsync(
                        "image_operations.log",
                        $"Image uploaded: {imageFile.FileName} -> {imageUrl} at {DateTime.UtcNow}",
                        "images");
                    
                    TempData["Success"] = $"Image uploaded successfully! URL: {imageUrl}";
                    ViewBag.ImageUrl = imageUrl;
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error uploading image: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "Please select an image file.";
            }
            
            return View();
        }

        #endregion

        #region Queue Operations

        [HttpGet]
        public IActionResult ProcessOrder()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessOrder(string orderId, string customerId, decimal totalAmount)
        {
            try
            {
                var orderMessage = new OrderProcessingMessage
                {
                    OrderId = orderId,
                    CustomerId = customerId,
                    Action = "ProcessOrder",
                    TotalAmount = totalAmount,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = "sample-product", ProductName = "Sample Product", Quantity = 1, Price = totalAmount }
                    }
                };

                await _queueService.EnqueueOrderProcessingAsync(orderMessage);
                
                // Log the operation
                await _fileService.UploadLogFileAsync(
                    "order_operations.log",
                    $"Order queued for processing: {orderId} (Customer: {customerId}, Amount: {totalAmount:C}) at {DateTime.UtcNow}",
                    "orders");
                
                TempData["Success"] = $"Order {orderId} queued for processing successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error processing order: {ex.Message}";
            }
            
            return View();
        }

        [HttpGet]
        public IActionResult UpdateInventory()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInventory(string productId, string action, int quantity, string reason)
        {
            try
            {
                var inventoryMessage = new InventoryUpdateMessage
                {
                    ProductId = productId,
                    Action = action,
                    Quantity = quantity,
                    Reason = reason
                };

                await _queueService.EnqueueInventoryUpdateAsync(inventoryMessage);
                
                // Log the operation
                await _fileService.UploadLogFileAsync(
                    "inventory_operations.log",
                    $"Inventory update queued: {action} {quantity} units for product {productId} (Reason: {reason}) at {DateTime.UtcNow}",
                    "inventory");
                
                TempData["Success"] = $"Inventory update for product {productId} queued successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating inventory: {ex.Message}";
            }
            
            return View();
        }

        #endregion

        #region File Storage Operations

        [HttpGet]
        public async Task<IActionResult> ViewLogs(string logType = "general")
        {
            try
            {
                var logFiles = await _fileService.ListLogFilesAsync(logType);
                ViewBag.LogType = logType;
                ViewBag.LogTypes = new[] { "general", "products", "customers", "images", "orders", "inventory" };
                return View(logFiles);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading log files: {ex.Message}";
                return View(new List<string>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewLogFile(string filePath)
        {
            try
            {
                var content = await _fileService.DownloadLogFileAsync(filePath);
                ViewBag.FilePath = filePath;
                ViewBag.Content = content;
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading log file: {ex.Message}";
                return RedirectToAction(nameof(ViewLogs));
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateLogEntry(string logType, string fileName, string content)
        {
            try
            {
                var filePath = await _fileService.UploadLogFileAsync(fileName, content, logType);
                TempData["Success"] = $"Log file created successfully: {filePath}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating log file: {ex.Message}";
            }
            
            return RedirectToAction(nameof(ViewLogs), new { logType });
        }

        #endregion

        #region Queue Processing (Background Operations)

        [HttpPost]
        public async Task<IActionResult> ProcessNextOrder()
        {
            try
            {
                var orderMessage = await _queueService.DequeueOrderProcessingAsync();
                if (orderMessage != null)
                {
                    // Simulate order processing
                    await Task.Delay(1000);

                    // Queue order lifecycle message for "Processing" status
                    var lifecycleMessage = new OrderLifecycleMessage
                    {
                        OrderId = orderMessage.OrderId,
                        CustomerId = orderMessage.CustomerId,
                        Status = "Processing",
                        PreviousStatus = "Placed",
                        TotalAmount = orderMessage.TotalAmount,
                        Notes = "Order moved to processing by admin"
                    };
                    await _queueService.EnqueueOrderLifecycleAsync(lifecycleMessage);

                    // Queue admin activity
                    var adminActivity = new AdminActivityMessage
                    {
                        AdminId = User.Identity?.Name ?? "system",
                        Action = "ProcessOrder",
                        EntityType = "Order",
                        EntityId = orderMessage.OrderId,
                        Details = $"Processed order {orderMessage.OrderId} for {orderMessage.TotalAmount:C}",
                        Metadata = new Dictionary<string, object>
                        {
                            ["CustomerId"] = orderMessage.CustomerId,
                            ["TotalAmount"] = orderMessage.TotalAmount,
                            ["ItemCount"] = orderMessage.Items.Count
                        }
                    };
                    await _adminActivityQueueService.EnqueueAdminActivityAsync(adminActivity);
                    
                    await _fileService.UploadLogFileAsync(
                        "processed_orders.log",
                        $"Processed order: {orderMessage.OrderId} for customer {orderMessage.CustomerId} (Amount: {orderMessage.TotalAmount:C}) at {DateTime.UtcNow}",
                        "orders");
                    
                    TempData["Success"] = $"Order {orderMessage.OrderId} processed successfully!";
                }
                else
                {
                    TempData["Info"] = "No orders in queue to process.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error processing order: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ProcessNextInventoryUpdate()
        {
            try
            {
                var inventoryMessage = await _queueService.DequeueInventoryUpdateAsync();
                if (inventoryMessage != null)
                {
                    // Simulate inventory processing
                    await Task.Delay(500);

                    // Queue admin activity
                    var adminActivity = new AdminActivityMessage
                    {
                        AdminId = User.Identity?.Name ?? "system",
                        Action = "ProcessInventoryUpdate",
                        EntityType = "Inventory",
                        EntityId = inventoryMessage.ProductId,
                        Details = $"Processed inventory {inventoryMessage.Action}: {inventoryMessage.Quantity} units",
                        Metadata = new Dictionary<string, object>
                        {
                            ["Action"] = inventoryMessage.Action,
                            ["Quantity"] = inventoryMessage.Quantity,
                            ["Reason"] = inventoryMessage.Reason
                        }
                    };
                    await _adminActivityQueueService.EnqueueAdminActivityAsync(adminActivity);
                    
                    await _fileService.UploadLogFileAsync(
                        "processed_inventory.log",
                        $"Processed inventory update: {inventoryMessage.Action} {inventoryMessage.Quantity} units for product {inventoryMessage.ProductId} at {DateTime.UtcNow}",
                        "inventory");
                    
                    TempData["Success"] = $"Inventory update for product {inventoryMessage.ProductId} processed successfully!";
                }
                else
                {
                    TempData["Info"] = "No inventory updates in queue to process.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error processing inventory update: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // Order Shipping Management
        [HttpPost]
        public async Task<IActionResult> ShipOrder(string orderId, string trackingNumber, string carrier, DateTime estimatedDelivery, string? shippingNotes)
        {
            try
            {
                // Queue order lifecycle message for "Shipped" status
                var lifecycleMessage = new OrderLifecycleMessage
                {
                    OrderId = orderId,
                    CustomerId = "customer", // In real app, get from order data
                    Status = "Shipped",
                    PreviousStatus = "Processing",
                    TotalAmount = 0, // In real app, get from order data
                    Notes = $"Shipped via {carrier}, Tracking: {trackingNumber}, Est. Delivery: {estimatedDelivery:yyyy-MM-dd}"
                };
                await _queueService.EnqueueOrderLifecycleAsync(lifecycleMessage);

                // Queue admin activity
                var adminActivity = new AdminActivityMessage
                {
                    AdminId = User.Identity?.Name ?? "system",
                    Action = "ShipOrder",
                    EntityType = "Order",
                    EntityId = orderId,
                    Details = $"Order {orderId} shipped via {carrier}",
                    Metadata = new Dictionary<string, object>
                    {
                        ["TrackingNumber"] = trackingNumber,
                        ["Carrier"] = carrier,
                        ["EstimatedDelivery"] = estimatedDelivery,
                        ["ShippingNotes"] = shippingNotes ?? ""
                    }
                };
                await _adminActivityQueueService.EnqueueAdminActivityAsync(adminActivity);

                // Log the operation
                await _fileService.UploadLogFileAsync(
                    "shipping_operations.log",
                    $"Order shipped: {orderId} via {carrier}, Tracking: {trackingNumber}, Est. Delivery: {estimatedDelivery:yyyy-MM-dd} at {DateTime.UtcNow}",
                    "orders");

                TempData["Success"] = $"Order {orderId} marked as shipped successfully! Tracking: {trackingNumber}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error shipping order: {ex.Message}";
            }

            return RedirectToAction("OrderShipping");
        }

        [HttpGet]
        public IActionResult OrderShipping()
        {
            return View();
        }

        // Process lifecycle events
        [HttpPost]
        public async Task<IActionResult> ProcessNextLifecycleEvent()
        {
            try
            {
                var lifecycleMessage = await _queueService.DequeueOrderLifecycleAsync();
                if (lifecycleMessage != null)
                {
                    // Queue admin activity for processing lifecycle event
                    var adminActivity = new AdminActivityMessage
                    {
                        AdminId = User.Identity?.Name ?? "system",
                        Action = "ProcessLifecycleEvent",
                        EntityType = "OrderLifecycle",
                        EntityId = lifecycleMessage.OrderId,
                        Details = $"Processed lifecycle event: {lifecycleMessage.PreviousStatus} → {lifecycleMessage.Status}",
                        Metadata = new Dictionary<string, object>
                        {
                            ["OrderId"] = lifecycleMessage.OrderId,
                            ["PreviousStatus"] = lifecycleMessage.PreviousStatus,
                            ["NewStatus"] = lifecycleMessage.Status,
                            ["TotalAmount"] = lifecycleMessage.TotalAmount
                        }
                    };
                    await _adminActivityQueueService.EnqueueAdminActivityAsync(adminActivity);

                    await _fileService.UploadLogFileAsync(
                        "lifecycle_events.log",
                        $"Processed lifecycle event: Order {lifecycleMessage.OrderId} - {lifecycleMessage.PreviousStatus} → {lifecycleMessage.Status} at {DateTime.UtcNow}",
                        "orders");

                    TempData["Success"] = $"Lifecycle event processed: Order {lifecycleMessage.OrderId} - {lifecycleMessage.PreviousStatus} → {lifecycleMessage.Status}";
                }
                else
                {
                    TempData["Info"] = "No lifecycle events in queue to process.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error processing lifecycle event: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Process admin activities
        [HttpPost]
        public async Task<IActionResult> ProcessNextAdminActivity()
        {
            try
            {
                var adminActivity = await _adminActivityQueueService.DequeueAdminActivityAsync();
                if (adminActivity != null)
                {
                    await _fileService.UploadLogFileAsync(
                        "admin_activities.log",
                        $"Processed admin activity: {adminActivity.Action} by {adminActivity.AdminId} on {adminActivity.EntityType} {adminActivity.EntityId} - {adminActivity.Details} at {DateTime.UtcNow}",
                        "admin");

                    TempData["Success"] = $"Admin activity processed: {adminActivity.Action} on {adminActivity.EntityType} {adminActivity.EntityId}";
                }
                else
                {
                    TempData["Info"] = "No admin activities in queue to process.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error processing admin activity: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion
    }
}
