using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace fyserver
{
    public static class GlobalState
    {
        public static FasterUserStoreService users = new FasterUserStoreService();

    }
    public class config
    {
        public int portWs = 5232;
        public int portHttp = 5231;
        public bool bancheat = false;
        //public Random rand=new();
        public ConcurrentBag<User> clients = [];
        public List<LobbyPlayer> WaitingPlayers1 = [];
        public List<LobbyPlayer> WaitingPlayers2 = [];
        public ConcurrentDictionary<string, List<LobbyPlayer>> BattleCodePlayers = [];
        public ConcurrentDictionary<int, MatchInfo> MatchedPairs = new();
        public readonly ConcurrentDictionary<int, User> UsersById = new();
        public string ip = "0.0.0.0";
        //public List<KeyValuePair<User, string>>? clients;
        public string getAddressWs()
        {
            return "ws://127.0.0.1:" + portWs.ToString();
        }
        public string getAddressWsR()
        {
            return "ws://" + ip + ":" + portWs.ToString();
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
            if (auth != "1939Mother")
            {
                string authToken = auth["JWT ".Length..];
                User? user = await GlobalState.users.GetByUserNameAsync(authToken);
                Console.WriteLine("getConfigAsync: authToken=" + authToken);
                Console.WriteLine("getConfigAsync: user=" + (user == null ? "null" : user.UserName));
                currentUser = new CurrentUser(
                    // TS 对应: "client_id": user.id
                    ClientId: user.Id,
                    // TS 对应: "exp": user.id
                    Exp: user.Id,
                    // TS 对应: "external_id": auth.slice (8) -> 即用户名
                    ExternalId: authToken,
                    Iat: 1752328020,
                    // TS 对应: "identity_id": user.id
                    IdentityId: user.Id,
                    Iss: "",
                    Jti: "",
                    // TS 对应: "language": user.locale
                    Language: user.Locale,
                    Payment: "notavailable",
                    // TS 对应: "player_id": user.id
                    // 注意：你的 Record 定义 PlayerId 是 string，所以这里要 ToString ()
                    PlayerId: user.Id.ToString(),
                    Provider: "device",
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
                           MyDraft: string.IsNullOrEmpty(auth) ? "" : $"{getAddressHttpR()}/draft/{auth["Bearer ".Length..]}",
                           MyItems: string.IsNullOrEmpty(auth) ? "" : $"{getAddressHttpR()}/items/{auth["Bearer ".Length..]}",
                           MyPlayer: string.IsNullOrEmpty(auth) ? "" : $"{getAddressHttpR()}/players/{auth["Bearer ".Length..]}",
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
                this.portWs = config.portWs;
                this.portHttp = config.portHttp;
                this.ip = config.ip;
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
