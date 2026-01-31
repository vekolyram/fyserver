using NativeImport;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace fyserver
{
    public static class GlobalState{
        public static UserStoreService users = new UserStoreService();
       
    }
    public class config
    {
        public int portWs=5232;
        public int portHttp = 5231;
        public bool bancheat = false;
        //public Random rand=new();
        public ConcurrentBag<User> clients =[];
        public List<LobbyPlayer> WaitingPlayers1 = [];
        public List<LobbyPlayer> WaitingPlayers2 = [];
        public ConcurrentDictionary<string, List<LobbyPlayer>> BattleCodePlayers =[];
        public ConcurrentDictionary<int, MatchInfo> MatchedPairs = new();
        public readonly ConcurrentDictionary<int, User> UsersById = new();
        public string ip="0.0.0.0";
        //public List<KeyValuePair<User, string>>? clients;
        public string getAddressWs() {
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
        public Config getConfig(string auth) {
            var config1 = new Config(
                       CurrentUser: string.IsNullOrEmpty(auth) ? null :
                           new CurrentUser(
                           ClientId: 0,
                           Exp: 0,
                           ExternalId: auth["Bearer ".Length..],
                           Iat: 1752328020,
                           IdentityId: 0,
                           Iss: "",
                           Jti: "",
                           Language: "en",
                           Payment: "notavailable",
                           PlayerId: int.Parse(auth["Bearer ".Length..]),
                           Provider: "device",
                           Roles: new List<string>(),
                           Tier: "LIVE",
                           UserId: int.Parse(auth["Bearer ".Length..]),
                           UserName: auth["Bearer ".Length..]
                           )
                       ,
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
        public static config appconfig { get; }=new config();
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
        public void write() {
            File.WriteAllText("./setting.json",JsonConvert.SerializeObject(this));
        }
    }
}
