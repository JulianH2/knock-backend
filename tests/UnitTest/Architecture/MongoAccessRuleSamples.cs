// Tipos de prueba para la regla de acceso a MongoDB (MongoAccessRule): Alpha imita un módulo y Hosting el arranque.
// Son estáticos y nunca se llaman: la regla solo lee sus dependencias. El cliente apunta a un host que no existe.
namespace Knock.UnitTest.Architecture.Fixtures.Mongo.Violating.Modules.Alpha.Services
{
    using MongoDB.Bson;
    using MongoDB.Driver;

    internal static class AlphaServiceReadsCollection
    {
        public static long Count(IMongoCollection<BsonDocument> collection) => collection.CountDocuments(FilterDefinition<BsonDocument>.Empty);
    }
}

namespace Knock.UnitTest.Architecture.Fixtures.Mongo.Violating.Modules.Alpha.Data
{
    using MongoDB.Driver;

    internal static class AlphaDataCreatesClient
    {
        public static IMongoClient Create() => new MongoClient(new MongoClientSettings { Server = new MongoServerAddress("alpha.invalid") });
    }
}

namespace Knock.UnitTest.Architecture.Fixtures.Mongo.Compliant.Modules.Alpha.Data
{
    using MongoDB.Bson;
    using MongoDB.Driver;

    internal static class AlphaRepository
    {
        public static long Count(IMongoDatabase database) =>
            database.GetCollection<BsonDocument>("items").CountDocuments(FilterDefinition<BsonDocument>.Empty);
    }
}

namespace Knock.UnitTest.Architecture.Fixtures.Mongo.Compliant.Hosting
{
    using MongoDB.Driver;

    internal static class SharedClient
    {
        public static IMongoClient Create() => new MongoClient(new MongoClientSettings { Server = new MongoServerAddress("hosting.invalid") });
    }
}
