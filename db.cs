//using LiteDB;
//using System.Text.Json; // 如果你需要手动处理JSON，虽然LiteDB自带BsonMapper
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Linq;

//public class LiteDbService : IDisposable
//{
//    private readonly LiteDatabase _db;
//    private const string CollectionName = "kv_store"; // 模拟 LevelDB 的主存储区

//    // 构造函数：传入数据库路径，例如 "Filename=./db/users.db;Connection=Shared"
//    public LiteDbService(string connectionString = "Filename=./db/users.db;Connection=Shared")
//    {

//        _db = new LiteDatabase(connectionString);
//        var connectionInfo = new ConnectionString(connectionString);
//        var directory = Path.GetDirectoryName(connectionInfo.Filename);

//        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
//        {
//            Directory.CreateDirectory(directory);
//        }
//        // 确保 _id (即你的 Key) 有索引，保证查询速度
//        var col = _db.GetCollection(CollectionName);
//        col.EnsureIndex("_id");
//    }
//    // 存储值
//    // LiteDB 是同步IO，为了匹配接口的 async/Task，我们使用 Task.Run 包装
//    public Task PutAsync(string key, object value)
//    {
//        return Task.Run(() =>
//        {
//            var col = _db.GetCollection(CollectionName);

//            // 我们构建一个 BsonDocument 来包装数据
//            // _id = 你的 key
//            // payload = 你的实际对象
//            var doc = new BsonDocument();
//            doc["_id"] = key;

//            // BsonMapper.Global.Serialize 会将任意对象转换为 BsonValue
//            doc["payload"] = BsonMapper.Global.Serialize(value);

//            // Upsert: 如果存在则更新，不存在则插入
//            col.Upsert(doc);
//        });
//    }

//    // 获取值
//    public Task<T?> GetAsync<T>(string key)
//    {
//        return Task.Run(() =>
//        {
//            var col = _db.GetCollection(CollectionName);
//            Console.WriteLine($"Looking for key: {key}");
//            var doc = col.FindById(key);
//            Console.WriteLine($"Looking for doc{doc}");
//            if (doc == null)
//            {
//                return default(T);
//            }

//            // 反序列化 payload 字段回原本的类型 T
//            return BsonMapper.Global.Deserialize<T>(doc["payload"]);
//        });
//    }

//    // 删除值
//    public Task DeleteAsync(string key)
//    {
//        return Task.Run(() =>
//        {
//            var col = _db.GetCollection(CollectionName);
//            col.Delete(key);
//        });
//    }

//    // 检查键是否存在
//    public Task<bool> ExistsAsync(string key)
//    {
//        return Task.Run(() =>
//        {
//            var col = _db.GetCollection(CollectionName);
//            return col.Exists(Query.EQ("_id", key));
//        });
//    }

//    // 获取所有键
//    public Task<List<string>> GetAllKeysAsync()
//    {
//        return Task.Run(() =>
//        {
//            var col = _db.GetCollection(CollectionName);
//            // 这是一个比较重的操作，类似于 LevelDB 的全量扫描
//            return col.FindAll().Select(x => x["_id"].AsString).ToList();
//        });
//    }

//    // 根据前缀获取所有键 (模拟 LevelDB 的 iterator 扫描)
//    public Task<List<string>> GetKeysByPrefixAsync(string prefix)
//    {
//        return Task.Run(() =>
//        {
//            var col = _db.GetCollection(CollectionName);
//            // LiteDB 支持 StartsWith 查询
//            return col.Find(Query.StartsWith("_id", prefix))
//                      .Select(x => x["_id"].AsString)
//                      .ToList();
//        });
//    }

//    // 根据前缀获取所有值
//    public Task<List<T>> GetAllByPrefixAsync<T>(string prefix)
//    {
//        return Task.Run(() =>
//        {
//            var col = _db.GetCollection(CollectionName);
//            var docs = col.Find(Query.StartsWith("_id", prefix));

//            var result = new List<T>();
//            foreach (var doc in docs)
//            {
//                result.Add(BsonMapper.Global.Deserialize<T>(doc["payload"]));
//            }
//            return result;
//        });
//    }

//    // 批量操作 (事务支持)
//    public Task BatchAsync(Dictionary<string, object> puts, List<string> deletes = null)
//    {
//        return Task.Run(() =>
//        {
//            var col = _db.GetCollection(CollectionName);

//            // 开启事务，保证批量操作的原子性
//            _db.BeginTrans();
//            try
//            {
//                if (deletes != null)
//                {
//                    foreach (var key in deletes)
//                    {
//                        col.Delete(key);
//                    }
//                }

//                if (puts != null)
//                {
//                    foreach (var kvp in puts)
//                    {
//                        var doc = new BsonDocument();
//                        doc["_id"] = kvp.Key;
//                        doc["payload"] = BsonMapper.Global.Serialize(kvp.Value);
//                        col.Upsert(doc);
//                    }
//                }

//                _db.Commit();
//            }
//            catch
//            {
//                _db.Rollback();
//                throw;
//            }
//        });
//    }

//    // 清空数据库 (删除集合)
//    public Task ClearAsync()
//    {
//        return Task.Run(() =>
//        {
//            _db.DropCollection(CollectionName);
//        });
//    }

//    public void Dispose()
//    {
//        _db?.Dispose();
//    }
//}
using LiteDB;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

public class LiteDbService : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<BsonDocument> _col;
    private const string CollectionName = "kv_store"; // 模拟 LevelDB 的主存储区
    private const string IdField = "_id";
    private const string PayloadField = "payload";
    private static readonly BsonMapper Mapper = BsonMapper.Global;
    // In-memory cache to reduce disk reads for frequently accessed keys
    private readonly ConcurrentDictionary<string, BsonValue> _payloadCache = new();
    // Cache deserialized objects to avoid repeated expensive deserialization
    private readonly ConcurrentDictionary<string, object> _objectCache = new();
    private readonly bool _verboseLogging;

    // 构造函数：传入数据库路径，例如 "Filename=./db/users.db;Connection=Shared"
    public LiteDbService(string connectionString = "Filename=./db/users.db;Connection=Shared", bool verboseLogging = false)
    {

        _verboseLogging = verboseLogging;

        _db = new LiteDatabase(connectionString);
        var connectionInfo = new ConnectionString(connectionString);
        var directory = Path.GetDirectoryName(connectionInfo.Filename);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        // 确保 _id (即你的 Key) 有索引，保证查询速度
        _col = _db.GetCollection(CollectionName);
        _col.EnsureIndex(IdField);
    }
    // 存储值
    // LiteDB 是同步IO，为了匹配接口的 async/Task，这里同步执行并返回已完成 Task
    public Task PutAsync(string key, object value)
    {
        // 我们构建一个 BsonDocument 来包装数据
        // _id = 你的 key
        // payload = 你的实际对象
        var doc = new BsonDocument
        {
            [IdField] = key,
            // BsonMapper.Global.Serialize 会将任意对象转换为 BsonValue
            [PayloadField] = Mapper.Serialize(value)
        };

        // Upsert: 如果存在则更新，不存在则插入
        _col.Upsert(doc);
        // Update in-memory cache
        _payloadCache[key] = doc[PayloadField];
        _objectCache[key] = value!;
        return Task.CompletedTask;
    }

    // 获取值
    public Task<T?> GetAsync<T>(string key)
    {
        if (_verboseLogging) Console.WriteLine($"Looking for key: {key}");
        // Try object cache first (fastest)
        if (_objectCache.TryGetValue(key, out var obj))
        {
            if (obj is T t) return Task.FromResult<T?>(t);
            // if stored object type doesn't match requested type, fall through
        }

        // Next try payload cache to deserialize
        if (_payloadCache.TryGetValue(key, out var cached))
        {
            try
            {
                var des = Mapper.Deserialize<T>(cached);
                _objectCache[key] = des!;
                return Task.FromResult<T?>(des);
            }
            catch
            {
                // fall back to disk if cache corrupt for this type
            }
        }

        var doc = _col.FindById(key);
        if (_verboseLogging) Console.WriteLine($"Looking for doc{doc}");
        if (doc == null)
        {
            return Task.FromResult<T?>(default);
        }

        var payload = doc[PayloadField];
        // populate cache for subsequent reads
        _payloadCache[key] = payload;
        var deserialized = Mapper.Deserialize<T>(payload);
        _objectCache[key] = deserialized!;
        // 反序列化 payload 字段回原本的类型 T
        return Task.FromResult<T?>(deserialized);
    }

    // 删除值
    public Task DeleteAsync(string key)
    {
        _col.Delete(key);
        // remove from cache
        _payloadCache.TryRemove(key, out _);
        _objectCache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    // 检查键是否存在
    public Task<bool> ExistsAsync(string key)
    {
        // check object cache first
        if (_objectCache.ContainsKey(key)) return Task.FromResult(true);
        // then payload cache
        if (_payloadCache.ContainsKey(key)) return Task.FromResult(true);
        return Task.FromResult(_col.Exists(Query.EQ(IdField, key)));
    }

    // 获取所有键
    public Task<List<string>> GetAllKeysAsync()
    {
        // 这是一个比较重的操作，类似于 LevelDB 的全量扫描
        var result = new List<string>();
        foreach (var doc in _col.FindAll())
        {
            result.Add(doc[IdField].AsString);
        }
        return Task.FromResult(result);
    }

    // 根据前缀获取所有键 (模拟 LevelDB 的 iterator 扫描)
    public Task<List<string>> GetKeysByPrefixAsync(string prefix)
    {
        // LiteDB 支持 StartsWith 查询
        var result = new List<string>();
        foreach (var doc in _col.Find(Query.StartsWith(IdField, prefix)))
        {
            result.Add(doc[IdField].AsString);
        }
        return Task.FromResult(result);
    }

    // 根据前缀获取所有值
    public Task<List<T>> GetAllByPrefixAsync<T>(string prefix)
    {
        var docs = _col.Find(Query.StartsWith(IdField, prefix));

        var result = new List<T>();
        foreach (var doc in docs)
        {
            var key = doc[IdField].AsString;
            var payload = doc[PayloadField];
            // update cache
            _payloadCache[key] = payload;
            var des = Mapper.Deserialize<T>(payload);
            _objectCache[key] = des!;
            result.Add(des);
        }
        return Task.FromResult(result);
    }

    // 批量操作 (事务支持)
    public Task BatchAsync(Dictionary<string, object> puts, List<string> deletes = null)
    {
        if ((deletes == null || deletes.Count == 0) && (puts == null || puts.Count == 0))
        {
            return Task.CompletedTask;
        }

        // 开启事务，保证批量操作的原子性
        _db.BeginTrans();
        try
        {
            if (deletes != null && deletes.Count > 0)
            {
                foreach (var key in deletes)
                {
                    _col.Delete(key);
                    _payloadCache.TryRemove(key, out _);
                    _objectCache.TryRemove(key, out _);
                }
            }

            if (puts != null && puts.Count > 0)
            {
                foreach (var kvp in puts)
                {
                    var doc = new BsonDocument
                    {
                        [IdField] = kvp.Key,
                        [PayloadField] = Mapper.Serialize(kvp.Value)
                    };
                    _col.Upsert(doc);
                    _payloadCache[kvp.Key] = doc[PayloadField];
                    _objectCache[kvp.Key] = kvp.Value!;
                }
            }

            _db.Commit();
        }
        catch
        {
            _db.Rollback();
            throw;
        }
        return Task.CompletedTask;
    }

    // 清空数据库 (删除集合)
    public Task ClearAsync()
    {
        _db.DropCollection(CollectionName);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
