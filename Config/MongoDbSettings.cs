namespace EAD_BE.Config;

public class MongoDbSettings : IMongoDbSettings
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
}
