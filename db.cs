using Newtonsoft.Json;
using RocksDbSharp;
using System.Text;
using System.Text.Json;

public class RocksDbService : IDisposable
{
    private readonly RocksDb _db;
    private readonly JsonSerializerOptions _jsonOptions;

    public RocksDbService(string dbPath = "userdb")
    {
        // 创建数据库目录
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 配置RocksDB选项
        var options = new DbOptions()
            .SetCreateIfMissing(true)
            .SetCreateMissingColumnFamilies(true);

        // 打开数据库
        _db = RocksDb.Open(options, dbPath);

        // 配置JSON序列化选项
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    // 存储值
    public async Task PutAsync(string key, object value)
    {
        var json = JsonConvert.SerializeObject(value);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        await Task.Run(() => _db.Put(key, bytes.ToString()));
    }

    // 获取值
    public async Task<T?> GetAsync<T>(string key)
    {
        var bytes = await Task.Run(() => _db.Get(key));
        if (bytes == null)
            return default;

        var json = System.Text.Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(bytes));
        return                 JsonConvert.DeserializeObject<T>(json);
    }

    // 删除值
    public async Task DeleteAsync(string key)
    {
        await Task.Run(() => _db.Remove(key));
    }

    // 检查键是否存在
    public async Task<bool> ExistsAsync(string key)
    {
        var bytes = await Task.Run(() => _db.Get(key));
        return bytes != null;
    }

    // 获取所有键
    public async Task<List<string>> GetAllKeysAsync()
    {
        var keys = new List<string>();
        using (var iterator = _db.NewIterator())
        {
            iterator.SeekToFirst();
            while (iterator.Valid())
            {
                var key = iterator.StringKey();
                keys.Add(key);
                iterator.Next();
            }
        }
        return await Task.FromResult(keys);
    }

    // 根据前缀获取所有键
    public async Task<List<string>> GetKeysByPrefixAsync(string prefix)
    {
        var keys = new List<string>();
        using (var iterator = _db.NewIterator())
        {
            iterator.Seek(prefix);
            while (iterator.Valid() && iterator.StringKey().StartsWith(prefix))
            {
                keys.Add(iterator.StringKey());
                iterator.Next();
            }
        }
        return await Task.FromResult(keys);
    }

    // 根据前缀获取所有值
    public async Task<List<T>> GetAllByPrefixAsync<T>(string prefix)
    {
        var results = new List<T>();
        using (var iterator = _db.NewIterator())
        {
            iterator.Seek(prefix);
            while (iterator.Valid() && iterator.StringKey().StartsWith(prefix))
            {
                var json = iterator.StringValue();
                var value =                 JsonConvert.DeserializeObject<T>(json);
                if (value != null)
                    results.Add(value);
                iterator.Next();
            }
        }
        return await Task.FromResult(results);
    }

    // 批量操作
    public async Task BatchAsync(Dictionary<string, object> puts, List<string> deletes = null)
    {
        using (var writeBatch = new WriteBatch())
        {
            // 添加写入操作
            foreach (var kvp in puts)
            {
                var json = JsonConvert.SerializeObject(kvp.Value);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                writeBatch.Put(kvp.Key, bytes.ToString());
            }

            // 添加删除操作
            if (deletes != null)
            {
                foreach (var key in deletes)
                {
                    writeBatch.Delete(Encoding.UTF8.GetBytes(key));
                }
            }

            // 执行批量操作
            await Task.Run(() => _db.Write(writeBatch));
        }
    }

    // 清空数据库
    public async Task ClearAsync()
    {
        var keys = await GetAllKeysAsync();
        using (var writeBatch = new WriteBatch())
        {
            foreach (var key in keys)
            {
                writeBatch.Delete(Encoding.UTF8.GetBytes(key));
            }
            await Task.Run(() => _db.Write(writeBatch));
        }
    }

    public void Dispose()
    {
        _db?.Dispose();
        GC.SuppressFinalize(this);
    }
}