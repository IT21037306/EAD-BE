using MongoDB.Driver;
using EAD_BE.Models.Vendor.Product;

namespace EAD_BE.Data
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