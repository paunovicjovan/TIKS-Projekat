using MongoDB.Driver;

namespace DataLayer;

public static class DbConnection
{
    private static readonly Lazy<IMongoClient> LazyClient = new(() =>
    {
        var connectionString = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build()
            .GetSection("ConnectionStrings")["MongoDB"];
        return new MongoClient(connectionString);
    });

    private static IMongoClient MongoClient => LazyClient.Value;

    public static IMongoDatabase GetDatabase(string databaseName = "nbp")
    {
        return MongoClient.GetDatabase(databaseName);
    }
}