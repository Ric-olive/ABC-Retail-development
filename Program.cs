using ABC_Retail.Services;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using DotNetEnv;
using System.Globalization;

namespace ABC_Retail
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
                options.Cookie.HttpOnly = true;                 // Secure the session cookie
                options.Cookie.IsEssential = true;              // Ensure it's saved even if GDPR applies
            });

            // Load secrets from .env file (only for local dev)
            Env.Load();

            // Load environment variable securely
            string? connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection");


            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("AzureStorageConnection environment variable not found.");
            }

            // Register BlobServiceClient for DI
            builder.Services.AddSingleton(new BlobServiceClient(connectionString));

            // Instantiate TableServiceClient and register Services
            TableServiceClient tableServiceClient = new TableServiceClient(connectionString);
            builder.Services.AddSingleton(new ProductService(tableServiceClient));
            builder.Services.AddSingleton(new CustomerService(tableServiceClient));
            builder.Services.AddSingleton(new CartService(tableServiceClient));
            builder.Services.AddSingleton(new OrderService(tableServiceClient));
            builder.Services.AddSingleton(new AdminService(tableServiceClient));
            builder.Services.AddScoped<BlobImageService>();
            builder.Services.AddSingleton(new ImageUploadQueueService(connectionString, "image-upload-queue"));
            
            // Register new enhanced services
            builder.Services.AddSingleton(new OrderProcessingQueueService(connectionString));
            builder.Services.AddSingleton(new AdminActivityQueueService(connectionString));
            builder.Services.AddSingleton(new AzureFileService(connectionString));






            // Configure localization/culture to South Africa (Rand currency)
            var supportedCultures = new[] { new CultureInfo("en-ZA") };
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-ZA"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });
            app.UseStaticFiles();

            app.UseRouting();
            app.UseSession();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
