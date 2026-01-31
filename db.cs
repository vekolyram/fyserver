using LiteDB;
using System.Text.Json; // 如果你需要手动处理JSON，虽然LiteDB自带BsonMapper
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class LiteDbService : IDisposable
{
    private readonly LiteDatabase _db;
    private const string CollectionName = "kv_store"; // 模拟 LevelDB 的主存储区

    // 构造函数：传入数据库路径，例如 "Filename=./db/users.db;Connection=Shared"
    public LiteDbService(string connectionString = "Filename=./db/users.db;Connection=Shared")
    {

        _db = new LiteDatabase(connectionString);
        var connectionInfo = new ConnectionString(connectionString);
        var directory = Path.GetDirectoryName(connectionInfo.Filename);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        // 确保 _id (即你的 Key) 有索引，保证查询速度
        var col = _db.GetCollection(CollectionName);
        col.EnsureIndex("_id");
    }
    // 存储值
    // LiteDB 是同步IO，为了匹配接口的 async/Task，我们使用 Task.Run 包装
    public Task PutAsync(string key, object value)
    {
        return Task.Run(() =>
        {
            var col = _db.GetCollection(CollectionName);

            // 我们构建一个 BsonDocument 来包装数据
            // _id = 你的 key
            // payload = 你的实际对象
            var doc = new BsonDocument();
            doc["_id"] = key;

            // BsonMapper.Global.Serialize 会将任意对象转换为 BsonValue
            doc["payload"] = BsonMapper.Global.Serialize(value);

            // Upsert: 如果存在则更新，不存在则插入
            col.Upsert(doc);
        });
    }

    // 获取值
    public Task<T?> GetAsync<T>(string key)
    {
        return Task.Run(() =>
        {
            var col = _db.GetCollection(CollectionName);
            var doc = col.FindById(key);

            if (doc == null)
            {
                return default(T);
            }

            // 反序列化 payload 字段回原本的类型 T
            return BsonMapper.Global.Deserialize<T>(doc["payload"]);
        });
    }

    // 删除值
    public Task DeleteAsync(string key)
    {
        return Task.Run(() =>
        {
            var col = _db.GetCollection(CollectionName);
            col.Delete(key);
        });
    }

    // 检查键是否存在
    public Task<bool> ExistsAsync(string key)
    {
        return Task.Run(() =>
        {
            var col = _db.GetCollection(CollectionName);
            return col.Exists(Query.EQ("_id", key));
        });
    }

    // 获取所有键
    public Task<List<string>> GetAllKeysAsync()
    {
        return Task.Run(() =>
        {
            var col = _db.GetCollection(CollectionName);
            // 这是一个比较重的操作，类似于 LevelDB 的全量扫描
            return col.FindAll().Select(x => x["_id"].AsString).ToList();
        });
    }

    // 根据前缀获取所有键 (模拟 LevelDB 的 iterator 扫描)
    public Task<List<string>> GetKeysByPrefixAsync(string prefix)
    {
        return Task.Run(() =>
        {
            var col = _db.GetCollection(CollectionName);
            // LiteDB 支持 StartsWith 查询
            return col.Find(Query.StartsWith("_id", prefix))
                      .Select(x => x["_id"].AsString)
                      .ToList();
        });
    }

    // 根据前缀获取所有值
    public Task<List<T>> GetAllByPrefixAsync<T>(string prefix)
    {
        return Task.Run(() =>
        {
            var col = _db.GetCollection(CollectionName);
            var docs = col.Find(Query.StartsWith("_id", prefix));

            var result = new List<T>();
            foreach (var doc in docs)
            {
                result.Add(BsonMapper.Global.Deserialize<T>(doc["payload"]));
            }
            return result;
        });
    }

    // 批量操作 (事务支持)
    public Task BatchAsync(Dictionary<string, object> puts, List<string> deletes = null)
    {
        return Task.Run(() =>
        {
            var col = _db.GetCollection(CollectionName);

            // 开启事务，保证批量操作的原子性
            _db.BeginTrans();
            try
            {
                if (deletes != null)
                {
                    foreach (var key in deletes)
                    {
                        col.Delete(key);
                    }
                }

                if (puts != null)
                {
                    foreach (var kvp in puts)
                    {
                        var doc = new BsonDocument();
                        doc["_id"] = kvp.Key;
                        doc["payload"] = BsonMapper.Global.Serialize(kvp.Value);
                        col.Upsert(doc);
                    }
                }

                _db.Commit();
            }
            catch
            {
                _db.Rollback();
                throw;
            }
        });
    }

    // 清空数据库 (删除集合)
    public Task ClearAsync()
    {
        return Task.Run(() =>
        {
            _db.DropCollection(CollectionName);
        });
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
