// NuGet: Install-Package RocksDbNative
// NuGet: Install-Package RocksDbSharp

using RocksDbSharp;
namespace fyserver
{
    public class RocksDbKV
    {
        private readonly RocksDb db;

        public RocksDbKV(string path = "rocksdb-data")
        {
            var options = new DbOptions()
                .SetCreateIfMissing(true)
                .SetMaxOpenFiles(1000);

            db = RocksDb.Open(options, path);
        }

        public void Set(string key, string value)
        {
            db.Put(key, value);
        }

        public string Get(string key)
        {
            return db.Get(key);
        }

        public void Dispose()
        {
            db.Dispose();
        }
    }
}