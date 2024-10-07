/*
 * File: IMongoDbSettings.cs
 * Author: Ahamed Fahmi (IT21037306)
 * Description: Interface for MongoDB Connection Settings
 */

namespace EAD_BE.Config;

public interface IMongoDbSettings
{
    string ConnectionString { get; set; }
    string DatabaseName { get; set; }
}