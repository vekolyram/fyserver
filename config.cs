using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace fyserver
{
    public class config
    {
        public int portWs=5232;
        public int portHttp = 5231;
        public bool banchaeat = false;
        public Random rand=new Random();
        public RocksDbKV users = new RocksDbKV(@"users.db");
        public List<KeyValuePair<User, string>>? clients;
public static config appconfig { get; }=new config();
        public void read() {
            if (File.Exists("./setting.json"))
            {
                String file = File.ReadAllText("./setting.json");
                var config = JsonConvert.DeserializeObject<config>(file);
                this.banchaeat = config.banchaeat;
                this.portWs = config.portWs;
                this.portHttp = config.portHttp;
            }
            else
            {
                
                write();
            }
        }
        public void write() {
            File.WriteAllText("./setting.json",JsonConvert.SerializeObject(this));
        }
    }
}
