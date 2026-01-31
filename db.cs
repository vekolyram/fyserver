using Newtonsoft.Json;
using System.Text;
using System.Text.Json;

public class RocksDbService : IDisposable
{
    private readonly JsonSerializerOptions _jsonOptions;
    // 存储值
    public async Task PutAsync(string key, object value)
    {
    }

    // 获取值
    public async Task<T?> GetAsync<T>(string key)
    {
    }

    // 删除值
    public async Task DeleteAsync(string key)
    {
    }

    // 检查键是否存在
    public async Task<bool> ExistsAsync(string key)
    {
    }

    // 获取所有键
    public async Task<List<string>> GetAllKeysAsync()
    {
    }

    // 根据前缀获取所有键
    public async Task<List<string>> GetKeysByPrefixAsync(string prefix)
    {
    }

    // 根据前缀获取所有值
    public async Task<List<T>> GetAllByPrefixAsync<T>(string prefix)
    {
    }

    // 批量操作
    public async Task BatchAsync(Dictionary<string, object> puts, List<string> deletes = null)
    {

    }

    // 清空数据库
    public async Task ClearAsync()
    {
    }

    public void Dispose()
    {
    }
}