using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigService.Client.Test
{
    public static class BootstrapMongoDb
    {
        public static void BootstrapDb()
        {
            var client = new MongoClient("mongodb://benreeves:4ExhPj2MD1la@ds111791.mlab.com:11791/config-svc-backup");
            var db = client.GetDatabase("config-svc-backup");
            var collection = db.GetCollection<BsonDocument>("kvp-settings");

            var testConfig = new BsonDocument
            {
                {"Type" , "String"},
                {"Value" , "Test"},
                {"Name" , "Test"},
                {"Version" , "1.0.0"},
            };
            var testKey = new BsonDocument
            {
                {"Name" , "Test"},
            };

            var exists = collection.Find(testKey);
            if (exists.Count() > 1)
            {
                collection.DeleteMany(testConfig);
            }
            else if (exists.Count() == 0)
            {
                collection.InsertOne(testConfig);
            }
            else if (exists.Count() == 1)
            {
                //eff it just update no matter what
                var update = Builders<BsonDocument>.Update.Set("Value", "Test")
                    .Set("Type", "String")
                    .Set("Name", "Test")
                    .Set("Version", "1.0.0");
                collection.UpdateOne(testKey, update);
            }

            //super lazy right now
            var testKey2 = new BsonDocument
            {
                {"Name" , "Test2"},
            };

            var exists2 = collection.Find(testKey2);
            if (exists2.Count() > 0)
            {
                collection.DeleteMany(testKey2);
            }
        }
    }
}
