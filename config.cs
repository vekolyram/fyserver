using Newtonsoft.Json;
using System.Collections.Concurrent;
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
        public UserStoreService users = new UserStoreService();
        public readonly ConcurrentBag<User> Users = new();
        public List<LobbyPlayer> WaitingPlayers1 = new();
        public List<LobbyPlayer> WaitingPlayers2 = new();
        public ConcurrentDictionary<string, List<LobbyPlayer>> BattleCodePlayers = new();
        public ConcurrentDictionary<int, MatchInfo> MatchedPairs = new();
        public readonly ConcurrentDictionary<int, User> UsersById = new();
        public List<KeyValuePair<User, string>>? clients;
        public string getAddressWs() {
            return "ws://127.0.0.1:" + portWs.ToString();
        }
        public string getAddressHttp()
        {
            return "http://127.0.0.1:" + portHttp.ToString();
        }
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
