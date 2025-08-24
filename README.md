# ABC Retail 

ABC Retail is a lightweight, modern ecommerce web application built with ASP.NET Core MVC and Azure Table Storage. It’s designed to help small businesses, NGOs, and community groups launch a digital storefront with minimal overhead and maximum scalability.

---

##  Features

- Customer registration and login (session-based)
- Product listing, cart, and checkout flows
- Order tracking
- Search and filter UI with modern design
- Admin login and dashboard (optional)
- Azure Table Storage for fast, scalable data access
- Responsive UI with Bootstrap 5

---

## Tech Stack

| Layer        | Technology             |
|--------------|------------------------|
| Backend      | ASP.NET Core MVC       |
| Data Storage | Azure Table Storage    |
| UI Framework | Bootstrap 5            |
| Auth         | Session-based login    |
| Hosting      | Local IIS Express / Azure App Service |

---

## Local Setup Instructions

### 1. **Clone the Repository**
git clone https://github.com/yourusername/ABC_Retail.git
cd ABC_Retail

### 2. **Configure environment variables**
Create an .env file
Add your storage account connection string:
AzureStorageConnection=DefaultEndpointsProtocol=https;AccountName=youraccount;AccountKey=yourkey;EndpointSuffix=core.windows.net
