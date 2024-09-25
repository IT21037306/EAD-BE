using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;

public class MongoDbConnectionChecker
{
    private readonly string _connectionString;
    private readonly string _databaseName;

    public MongoDbConnectionChecker(string connectionString, string databaseName)
    {
        _connectionString = connectionString;
        _databaseName = databaseName;
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            var client = new MongoClient(_connectionString);
            var database = client.GetDatabase(_databaseName);
            var command = new BsonDocument("ping", 1);
            var result = await database.RunCommandAsync<BsonDocument>(command);
            return result.Contains("ok") && result["ok"] == 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection check failed: {ex.Message}");
            return false;
        }
    }
}