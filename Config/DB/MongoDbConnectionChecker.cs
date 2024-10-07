/*
 * File: MongoDbConnectionChecker.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Config class of connection checker for MongoDB
 */


using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;

public class MongoDbConnectionChecker
{
    private readonly string _connectionString;
    private readonly string _databaseName;

    //constructor
    public MongoDbConnectionChecker(string connectionString, string databaseName)
    {
        _connectionString = connectionString;
        _databaseName = databaseName;
    }

    //check the mongo db connection
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