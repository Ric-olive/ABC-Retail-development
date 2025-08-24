using ABC_Retail.Models;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Services
{
    public class ProductService
    {
        private readonly TableClient _table;

        public ProductService(TableServiceClient serviceClient)
        {
            _table = serviceClient.GetTableClient("Products");
            _table.CreateIfNotExists(); // Safe init
        }

        public async Task AddProductAsync(Product product) =>
            await _table.AddEntityAsync(product);

        public async Task<List<Product>> GetProductsAsync()
        {
            var items = new List<Product>();
            await foreach (Product p in _table.QueryAsync<Product>())
                items.Add(p);
            return items;
        }

        //READ: Single product by RowKey
         public async Task<Product?> GetProductAsync(string rowKey)
        {
            try
            {
                var response = await _table.GetEntityAsync<Product>("Retail", rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null; // Not found
            }
        }

        // Alias method for consistency with controller usage
        public async Task<Product?> GetProductByIdAsync(string id) => await GetProductAsync(id);

        public async Task UpdateProductAsync(Product product)
        {
            // Handle case where ETag might be null or empty
            if (product.ETag == null)
            {
                // Use UpsertEntityAsync with Replace mode as fallback
                await _table.UpsertEntityAsync(product, TableUpdateMode.Replace);
            }
            else
            {
                await _table.UpdateEntityAsync(product, product.ETag);
            }
        }

        public async Task DeleteProductAsync(string rowKey) =>
            await _table.DeleteEntityAsync("Retail", rowKey);

    }
}
