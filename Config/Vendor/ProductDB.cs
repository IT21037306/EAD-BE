/*
 * File: ProductDB.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Config class of Vendor Operations for Product Management
 */


using EAD_BE.Models.Vendor.Product;
using MongoDB.Driver;

namespace EAD_BE.Config.Vendor
{
    public class MongoDbContextProduct
    {
        private readonly IMongoDatabase _database;

        public MongoDbContextProduct(IConfiguration configuration)
        {
            var client = new MongoClient(Environment.GetEnvironmentVariable("MONGO_URL"));
            _database = client.GetDatabase(Environment.GetEnvironmentVariable("DB_NAME"));
        }

        public IMongoCollection<ProductModel> Products => _database.GetCollection<ProductModel>("Products");
    }
}