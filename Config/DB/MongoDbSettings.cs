/*
 * File: MongoDbSettings.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Class of MongoDB Connection Settings
 */


namespace EAD_BE.Config;

public class MongoDbSettings : IMongoDbSettings
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
}
