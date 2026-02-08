using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace fyserver
{
    public static class GlobalState
    {
        public static FasterUserStoreService users = new FasterUserStoreService();
        private static StoreConfig? _storeConfig;
       // private static FPResponse? _frontPageConfig;

        public static StoreConfig GetStoreConfig()
        {
            if (_storeConfig == null)
            {
                string configPath = "./config/store.json";
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    _storeConfig = JsonConvert.DeserializeObject<StoreConfig>(json);
                }
                else
                {
                    _storeConfig = new StoreConfig
                    {
                        Currency = "USD",
                        Groups = new List<StoreGroup>(),
                        AlwaysFeatured = new AlwaysFeaturedGroup(1, -1, "2018-01-01T00:00:00Z", "2099-01-01T00:00:00Z", new List<StoreOffer>())
                    };
                }
            }
            return _storeConfig;
        }
        public static void ReloadStoreConfig()
        {
            _storeConfig = null;
        }
    }

    public class StoreConfig
    {
        [JsonProperty("currency")]
        public string Currency { get; set; } = "USD";

        [JsonProperty("groups")]
        public List<StoreGroup> Groups { get; set; } = new();

        [JsonProperty("alwaysFeatured")]
        public AlwaysFeaturedGroup AlwaysFeatured { get; set; }
    }
    public class config
    {
        public int portWs = 5232;
        public int portHttp = 5231;
        public bool bancheat = false;
        //public Random rand=new();
        public List<LobbyPlayer> WaitingPlayers1 = [];
        public List<LobbyPlayer> WaitingPlayers2 = [];
        public ConcurrentDictionary<string, List<LobbyPlayer>> BattleCodePlayers = [];
        public ConcurrentDictionary<int, MatchInfo> MatchedPairs = new();
        public readonly ConcurrentDictionary<int, WebSocket> UsersById = new();
        public string ip = "0.0.0.0";
        //public List<KeyValuePair<User, string>>? clients;
        public string getAddressWs()
        {
            return "http://0.0.0.0:"+portWs;
        }
        public string getAddressWs2()
        {
            return "ws://127.0.0.1:" + portWs;
        }
        public string getAddressWsR()
        {
            return $"ws://{ip}:{portWs}";
        }
        public string getAddressHttp()
        {
            return "http://0.0.0.0:" + portHttp.ToString();
        }
        public string getAddressHttpR()
        {
            return "http://" + ip + ":" + portHttp.ToString();
        }
        public async Task<Config> getConfigAsync(string auth)
        {
            CurrentUser? currentUser = null;
                string authToken = auth["JWT ".Length..];
            if (auth != "1939Mother")
            {
                User? user = await GlobalState.users.GetByUserNameAsync(authToken);
                currentUser = new CurrentUser(
                    // TS 对应: "client_id": user.id
                    ClientId: user.Id,
                    // TS 对应: "exp": user.id
                    Exp: 1770289815,
                    // TS 对应: "external_id": auth.slice (8) -> 即用户名
                    ExternalId: authToken,
                    Iat: 1770289815,
                    // TS 对应: "identity_id": user.id
                    IdentityId: user.Id,
                    Iss: "fyserver",
                    Jti: "114514",
                    // TS 对应: "language": user.locale
                    Language: user.Locale,
                    Payment: "notavailable",
                    // TS 对应: "player_id": user.id
                    // 注意：你的 Record 定义 PlayerId 是 string，所以这里要 ToString ()
                    PlayerId: user.Id.ToString(),
                    Provider: "device_id",
                    Roles: new List<string>(),
                    Tier: "LIVE",
                    // TS 对应: "user_id": user.id
                    UserId: user.Id,
                    // TS 对应: "user_name": auth.slice (8)
                    UserName: authToken
                );
            }
            var config1 = new Config(
                       CurrentUser: currentUser,
                       Endpoints: new Endpoints(
                           Draft: $"{config.appconfig.getAddressHttpR()}/draft/",
                           Email: $"{getAddressHttpR()}/email/set",
                           Lobbyplayers: $"{getAddressHttpR()}/lobbyplayers",
                           Matches: $"{getAddressHttpR()}/matches",
                           Matches2: $"{getAddressHttpR()}/matches/v2/",
                           MyDraft: auth.Equals("1939Mother") ? "" : $"{getAddressHttpR()}/draft/{authToken}",
                           MyItems: auth.Equals("1939Mother") ? "" : $"{getAddressHttpR()}/items/{authToken}",
                           MyPlayer: auth.Equals("1939Mother") ? "" : $"{getAddressHttpR()}/players/{authToken}",
                           Players: $"{getAddressHttpR()}/players",
                           Purchase: $"{getAddressHttpR()}/store/v2/txn",
                           Root: getAddressHttpR(),
                           Session: $"{getAddressHttpR()}/session",
                           Store: $"{getAddressHttpR()}/store/",
                           Tourneys: $"{getAddressHttpR()}/tourney/",
                           Transactions: $"{getAddressHttpR()}/store/txn",
                           ViewOffers: $"{getAddressHttpR()}/store/v2/"
                       ));
            return config1;
        }
        public static config appconfig { get; } = new config();
        public void read()
        {
            if (File.Exists("./setting.json"))
            {
                String file = File.ReadAllText("./setting.json");
                var config = JsonConvert.DeserializeObject<config>(file);
                this.bancheat = config.bancheat;
                this.portHttp = config.portHttp;
                this.ip = config.ip;
                this.portWs = config.portWs;
            }
            else
            {
                write();
            }
        }
        public void write()
        {
            File.WriteAllText("./setting.json", JsonConvert.SerializeObject(this));
        }
    }
}
