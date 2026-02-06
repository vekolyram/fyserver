using LiteDB;
using System.Collections.Generic;
using System.Text.Json;

public class LiteDbService : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<BsonDocument> _col;
    private const string CollectionName = "kv_store";
    private const string IdField = "_id";
    private const string PayloadField = "payload";
    // System.Text.Json 序列化选项
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    private readonly bool _verboseLogging;

    // 构造函数：传入数据库路径
    public LiteDbService(string connectionString = "Filename=./db/users.db;Connection=Shared", bool verboseLogging = true)
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

    // 存储值 (同步)
    public void Put(string key, object value)
    {
        // 使用 System.Text.Json 序列化为 JSON 字符串
        var jsonString = System.Text.Json.JsonSerializer.Serialize(value, value.GetType(), JsonOptions);

        var doc = new BsonDocument
        {
            [IdField] = key,
            [PayloadField] = jsonString
        };

        // Upsert: 如果存在则更新，不存在则插入
        _col.Upsert(doc);

        if (_verboseLogging)
            Console.WriteLine($"Put: key={key}, size={jsonString.Length} bytes");
    }

    // 获取值
    public T? Get<T>(string key)
    {
        if (_verboseLogging)
            Console.WriteLine($"Get<{typeof(T).Name}>: Looking for key={key}");

        var doc = _col.FindById(key);

        if (doc == null)
        {
            if (_verboseLogging)
                Console.WriteLine($"Get<{typeof(T).Name}>: key={key} not found");
            return default;
        }

        var jsonString = doc[PayloadField].AsString;

        if (_verboseLogging)
            Console.WriteLine($"Get<{typeof(T).Name}>: key={key}, size={jsonString.Length} bytes");

        // 使用 System.Text.Json 反序列化
        var result = System.Text.Json.JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
        return result;
    }

    // 删除值 (同步)
    public void Delete(string key)
    {
        _col.Delete(key);

        if (_verboseLogging)
            Console.WriteLine($"Delete: key={key}");
    }

    // 检查键是否存在 (同步)
    public bool Exists(string key)
    {
        return _col.Exists(Query.EQ(IdField, key));
    }

    // 获取所有键 (同步)
    public List<string> GetAllKeys()
    {
        var result = new List<string>();
        foreach (var doc in _col.FindAll())
        {
            result.Add(doc[IdField].AsString);
        }
        return result;
    }

    // 根据前缀获取所有键 (同步)
    public List<string> GetKeysByPrefix(string prefix)
    {
        var result = new List<string>();
        foreach (var doc in _col.Find(Query.StartsWith(IdField, prefix)))
        {
            result.Add(doc[IdField].AsString);
        }
        return result;
    }

    // 根据前缀获取所有值 (同步)
    public List<T> GetAllByPrefix<T>(string prefix)
    {
        var docs = _col.Find(Query.StartsWith(IdField, prefix));

        var result = new List<T>();
        foreach (var doc in docs)
        {
            var jsonString = doc[PayloadField].AsString;
            var obj = System.Text.Json.JsonSerializer.Deserialize<T>(jsonString, JsonOptions);
            if (obj != null)
            {
                result.Add(obj);
            }
        }
        return result;
    }

    // 批量操作 (事务支持) (同步)
    public void Batch(Dictionary<string, object> puts, List<string> deletes = null)
    {
        if ((deletes == null || deletes.Count == 0) && (puts == null || puts.Count == 0))
        {
            return;
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
                }
            }

            if (puts != null && puts.Count > 0)
            {
                foreach (var kvp in puts)
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(kvp.Value, kvp.Value.GetType(), JsonOptions);
                    var doc = new BsonDocument
                    {
                        [IdField] = kvp.Key,
                        [PayloadField] = jsonString
                    };
                    _col.Upsert(doc);
                }
            }

            _db.Commit();

            if (_verboseLogging)
            {
                var putCount = puts?.Count ?? 0;
                var delCount = deletes?.Count ?? 0;
                Console.WriteLine($"Batch: puts={putCount}, deletes={delCount}");
            }
        }
        catch
        {
            _db.Rollback();
            throw;
        }
    }

    // 清空数据库 (删除集合) (同步)
    public void Clear()
    {
        _db.DropCollection(CollectionName);
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}