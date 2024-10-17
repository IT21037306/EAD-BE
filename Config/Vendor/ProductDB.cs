/*
 * File: ProductDB.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Config class of Vendor Operations for Product Management
 */

using EAD_BE.Models.Vendor.Product;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace EAD_BE.Config.Vendor
{
    public class MongoDbContextProduct
    {
        private readonly IMongoDatabase _database;

        // Constructor
        public MongoDbContextProduct(IOptions<MongoDbSettings> mongoDbSettings)
        {
            var settings = mongoDbSettings.Value;
            var client = new MongoClient(settings.ConnectionString);
            _database = client.GetDatabase(settings.DatabaseName);
        }

        // Products Collection
        public IMongoCollection<ProductModel> Products => _database.GetCollection<ProductModel>("Products");
    }
}
