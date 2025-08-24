# 🛍️ ABC Retail - E-Commerce Platform

[![.NET](https://img.shields.io/badge/.NET-6.0-purple.svg)](https://dotnet.microsoft.com/)
[![Azure](https://img.shields.io/badge/Azure-Cloud-blue.svg)](https://azure.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

ABC Retail is a world-class, Amazon-inspired e-commerce platform built with ASP.NET Core MVC and Azure cloud services. This application delivers a modern shopping experience with professional UI/UX design, real-time cart functionality, and enterprise-grade cloud infrastructure.

## 🌟 Features

### 🛒 **Customer Experience**
- **Modern Product Catalog** - Card-based layout with ratings and stock status
- **Real-time Shopping Cart** - AJAX-powered cart with instant updates
- **Toast Notifications** - Instant feedback for user actions
- **Responsive Design** - Optimized for desktop, tablet, and mobile
- **Secure Authentication** - Customer registration and login system

### 👨‍💼 **Admin Management**
- **Admin Dashboard** - Comprehensive administrative interface
- **Product Management** - Add, edit, and manage product inventory
- **Order Processing** - Track and manage customer orders
- **File Management** - Azure-powered image and document storage

### ☁️ **Cloud Integration**
- **Azure Table Storage** - Scalable NoSQL data storage
- **Azure Blob Storage** - Secure image and file storage
- **Azure Queue Storage** - Asynchronous order and inventory processing
- **Auto-scaling** - Cloud-native architecture for high availability

## 🏗️ Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   Backend       │    │   Azure Cloud   │
│                 │    │                 │    │                 │
│ • Bootstrap 5   │◄──►│ • ASP.NET Core  │◄──►│ • Table Storage │
│ • JavaScript    │    │ • MVC Pattern   │    │ • Blob Storage  │
│ • Razor Views   │    │ • C# Services   │    │ • Queue Storage │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 🚀 Technology Stack

| Layer | Technology |
|-------|------------|
| **Frontend** | ASP.NET Core MVC, Bootstrap 5, JavaScript, Razor |
| **Backend** | C# .NET 6, ASP.NET Core Identity |
| **Database** | Azure Table Storage (NoSQL) |
| **File Storage** | Azure Blob Storage |
| **Messaging** | Azure Queue Storage |
| **Hosting** | Azure App Service Ready |

## 📋 Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download) or later
- [Azure Storage Account](https://azure.microsoft.com/services/storage/)
- IDE: [Visual Studio](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

## ⚡ Quick Start

### 1. Clone Repository
```bash
git clone https://github.com/Ric-olive/ABC-Retail-development.git
cd ABC-Retail-development
```

### 2. Configure Azure Storage
Create a `.env` file in the project root:
```env
AzureStorageConnection=DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net
```

### 3. Install Dependencies
```bash
dotnet restore
```

### 4. Run Application
```bash
dotnet run
```

Navigate to `http://localhost:5181` to access the application.

## 🔧 Configuration

### Environment Variables
| Variable | Description | Required |
|----------|-------------|----------|
| `AzureStorageConnection` | Azure Storage connection string | ✅ |

### Azure Services Setup
1. **Create Azure Storage Account**
2. **Enable Table Storage** for data persistence
3. **Enable Blob Storage** for file uploads
4. **Enable Queue Storage** for async processing

## 📁 Project Structure

```
ABC-Retail-development/
├── Controllers/           # MVC Controllers
│   ├── AdminController.cs
│   ├── CustomerController.cs
│   ├── CheckoutController.cs
│   └── ...
├── Models/               # Data Models & ViewModels
│   ├── DTOs/
│   ├── ViewModels/
│   └── ...
├── Services/             # Business Logic Services
│   ├── AdminService.cs
│   ├── CartService.cs
│   ├── OrderService.cs
│   └── ...
├── Views/                # Razor Views
│   ├── Admin/
│   ├── Customer/
│   └── ...
├── wwwroot/              # Static Files
│   ├── css/
│   ├── js/
│   └── lib/
└── Program.cs            # Application Entry Point
```

## 🎯 Key Features Implementation

### Real-time Cart Updates
```javascript
// AJAX-powered cart functionality
async function addToCart(productId, quantity) {
    const response = await fetch('/CustomerCart/AddToCart', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ productId, quantity })
    });
    // Update cart badge and show toast notification
}
```

### Azure Queue Processing
```csharp
// Asynchronous order processing
public async Task EnqueueOrderProcessingAsync(OrderProcessingMessage message)
{
    var queueClient = new QueueClient(_connectionString, "order-processing");
    await queueClient.SendMessageAsync(Convert.ToBase64String(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message))
    ));
}
```

## 🧪 Testing

### Run Tests
```bash
dotnet test
```

### Manual Testing Checklist
- [ ] Customer registration and login
- [ ] Product browsing and search
- [ ] Add/remove items from cart
- [ ] Checkout process
- [ ] Admin product management
- [ ] File upload functionality

## 🚀 Deployment

### Azure App Service
1. Create Azure App Service
2. Configure connection strings
3. Deploy using Visual Studio or Azure CLI

### Local Development
```bash
# Development server with hot reload
dotnet watch run
```

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Authors

- **Olivier Richie** - *Initial work* - [@Ric-olive](https://github.com/Ric-olive)

## 🙏 Acknowledgments

- Bootstrap team for the UI framework
- Microsoft Azure for cloud services
- ASP.NET Core community for excellent documentation

## 📞 Support

For support and questions:
- Create an [Issue](https://github.com/Ric-olive/ABC-Retail-development/issues)
- Contact: [Your Email]

---

⭐ **Star this repository if you find it helpful!**
