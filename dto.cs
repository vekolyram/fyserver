using Newtonsoft.Json;
namespace fyserver
{
    public class a
    {
        class Card
        {
            public string card { get; set; }
            public string deck_code_id { get; set; }
            public int ID { get; set; }
        }
        public static LibraryResponse Library = new(
            new List<LibraryItem>
            {
            //new("card_unit_seahawk", 1, 0, 2001, 0),
            //new("card_wildcard_elite",99,0,1065,0),
            //  new("card_wildcard_limited",99,0,1071,0),
            //new("card_wildcard_special",99,0,1076,0)
            },
            new List<object>()
        );

        public static readonly List<Item> Items = new()
    {
        new Item("", "cardback_yokoshiba", "")
    };

        public static readonly Dictionary<string, CardLookup> DeckCodeTable = new()
    {
        { "v7", new CardLookup("card_unit_1st_airborne", "v7", 2001) }
    };
        public static void InitLibrary(String Path) {
            string a = File.ReadAllText(Path);
            List<Card> cs = JsonConvert.DeserializeObject<List<Card>>(a);
            foreach (var c in cs) { 
                Library.Cards.Add(new LibraryItem(c.card, 1, 1, c.ID, 0));
            }
        }
    }
    public record DeckAction
    (
         string Action,
         string Value,
         string DeckCode
    );
    // Match DTOs as records
    public record LobbyPlayer(
        int PlayerId,
        int DeckId,
        string ExtraData = ""
    );

    public record MatchAction(
        string Action = "",
        string ActionType = "",
        int ActionId = 0,
        Dictionary<string, object>? Value = null,
        string? PlayerId = null,
        Dictionary<string, object>? ActionData = null,
        int LocalSubactions = 0
    );

    public record MulliganCards(
        List<int> DiscardedCardIds
    );

    public record MatchCard(
        int CardId,
        bool IsGold,
        string Location,
        int LocationNumber,
        string Name
    );

    public record MulliganResult(
        List<MatchCard> Deck,
        List<MatchCard> ReplacementCards
    );

    public record StartingData(
        string AllyFactionLeft,
        string AllyFactionRight,
        string CardBackLeft,
        string CardBackRight,
        List<MatchCard> StartingHandLeft,
        List<MatchCard> StartingHandRight,
        List<MatchCard> DeckLeft,
        List<MatchCard> DeckRight,
        List<string> EquipmentLeft,
        List<string> EquipmentRight,
        bool IsAiMatch,
        string LeftPlayerName,
        bool LeftPlayerOfficer,
        string LeftPlayerTag,
        MatchCard? LocationCardLeft,
        MatchCard? LocationCardRight,
        int PlayerIdLeft,
        int PlayerIdRight,
        int PlayerStarsLeft,
        int PlayerStarsRight,
        string RightPlayerName,
        bool RightPlayerOfficer,
        string RightPlayerTag
    );

    public record MatchData(
        string ActionPlayerId,
        string ActionSide,
        List<MatchAction> Actions,
        string ActionsUrl,
        int CurrentActionId,
        int CurrentTurn,
        int DeckIdLeft,
        int DeckIdRight,
        int LeftIsOnline,
        int MatchId,
        string MatchType,
        string MatchUrl,
        string ModifyDate,
        List<object> Notifications,
        int PlayerIdLeft,
        int PlayerIdRight,
        string PlayerStatusLeft,
        string PlayerStatusRight,
        int RightIsOnline,
        string StartSide,
        string Status,
        int WinnerId,
        string WinnerSide
    );

    public record MatchAndStartingData(
        MatchData Match,
        StartingData StartingData
    );

    public record MatchStartingInfo(
        bool LocalSubactions,
        MatchAndStartingData MatchAndStartingData
    );

    // Player DTOs as records
    public record CreateDeck(
        string Name,
        string MainFaction,
        string AllyFaction,
        string DeckCode
    );

    public record ChangeDeck(
        int Id,
        string Name,
        string Action
    );

    public record MatchingAction(
        string Action,
        string Value,
        string DeckCode
    );
    public record ProviderDetail(
        string PaymentProvider
    );
    // Session DTOs as records
    public record Session(
        string Provider,
        ProviderDetail ProviderDetails,
        string ClientType,
        string Build,
        string PlatformType,
        string AppGuid,
        string Version,
        string PlatformInfo,
        string PlatformVersion,
        string AccountLinking,
        string Language,
        bool AutomaticAccountCreation,
        string Username,
        string Password
    );
    public record Entitlement(
    string EntitlementType,
    string Name
  );
    public record FriendsReponse(
            List<int> Friends,
            List<int> PreviousOpponents
        );
    // Config records
    public record CurrentUser(
        int ClientId,
        int Exp,
        string ExternalId,
        int Iat,
        int IdentityId,
        string Iss,
        string Jti,
        string Language,
        string Payment,
        string PlayerId,
        string Provider,
        List<string> Roles,
        string Tier,
        int UserId,
        string UserName
    );
    public record CloseConfig(
       string XserverClosed = "",
        string XserverClosedHeader = "Server maintenance",
        string ForgotPasswordUrl = "https://pornhub.com"
    );
    public record Endpoints(
        string Draft,
        string Email,
        string Lobbyplayers,
        string Matches,
        string Matches2,
        string MyDraft,
        string MyItems,
        string MyPlayer,
        string Players,
        string Purchase,
        string Root,
        string Session,
        string Store,
        string Tourneys,
        string Transactions,
        string ViewOffers
    );

    public record Config(
        CurrentUser CurrentUser,
        Endpoints Endpoints
    );

    // Library/Cards records
    public record Card(
        string CardType,
        int Count,
        int GoldCardCount,
        int Id,
        int RecentlyCraftedCount
    );

    public record Library(
        List<Card> Cards,
        List<object> NewCards
    );

    public record ItemsResponse(
        List<Item> EquippedItems,
        List<Item> Items
    );
    // WebSocket messages as records
    public record WebSocketMessage(
        long Timestamp,
        string Message = "",
        string Channel = "",
        string Sender = "",
        string Receiver = "",
        string Context = "",
        int? MatchId = null
    );

    // Match response types as records
    public record MatchResponse(
        Dictionary<string, object> Match,
        List<MatchAction> Actions,
        bool OpponentPolling
    );

    public record PostMatchResponse(
        string Faction,
        bool Winner
    );

    // Service classes as records
    public record DeckCardsResult(
        List<MatchCard> Cards,
        MatchCard? Location
    );

    public record CardLookup(
        string Card,
        string DeckCodeId,
        int ID
    );

    public record ClientInfo(
        User User,
        System.Net.WebSockets.WebSocket Client
    );
    // UserStoreService.cs
    public class UserStoreService : IDisposable
    {
        private readonly LiteDbService _db;
        private readonly bool _ownsConnection;

        // 构造函数
        public UserStoreService(LiteDbService dbService)
        {
            _db = dbService;
            _ownsConnection = false;
        }

        public UserStoreService() : this(new LiteDbService("Filename=./db/users.db;Connection=Shared"))
        {
            _ownsConnection = true;
        }

        public Task<User?> GetByUserNameAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return Task.FromResult<User?>(null);
            var a = _db.Get<User>($"user:username:{userName}");
            return Task.FromResult<User?>(a);
        }

        public Task<User?> GetByIdAsync(int userId)
        {
            if (userId <= 0) return Task.FromResult<User?>(null);
            var u = _db.Get<User>($"user:id:{userId}");
            return Task.FromResult<User?>(u);
        }

        // 核心保存逻辑
        public Task SaveUserAsync(User user)
        {
            if (string.IsNullOrEmpty(user.UserName) || user.Id == 0)
                throw new ArgumentException("Invalid user data: Missing UserName or ID");

            // 更新修改时间
            user.UpdatedAt = DateTime.UtcNow;

            // 模拟 LevelDB 的双重索引 (冗余存储)
            // LiteDB 的 Batch 操作保证了原子性：要么都成功，要么都失败
            var puts = new Dictionary<string, object>
            {
                [$"user:username:{user.UserName}"] = user,
                [$"user:id:{user.Id}"] = user
            };

            _db.Batch(puts);
            return Task.CompletedTask;
        }

        // 创建用户：处理 ID 生成和冲突
        public async Task<User> CreateUserAsync(string userName)
        {
            // 1. 检查用户名是否存在
            var existingUser = await GetByUserNameAsync(userName);
            if (existingUser != null)
            {
                // 如果是幂等设计，可以直接返回旧用户；如果是严格创建，抛异常
                // 这里为了安全起见，如果不慎重复调用，返回已存在的
                return existingUser;
                // 或者: throw new InvalidOperationException ($"User '{userName}' already exists");
            }

            var user = new User(userName);
            // 2. 生成唯一 ID (带冲突重试)
            var rnd = Random.Shared;
            int newId;
            bool idExists;
            newId = rnd.Next(100000, 1000000);
            user.Id = newId;

            // 3. 保存
            await SaveUserAsync(user);
            return user;
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return;

            var deletes = new List<string>
            {
                $"user:username:{user.UserName}",
                $"user:id:{userId}"
            };

            _db.Batch(null, deletes);
            await Task.CompletedTask;
        }

        public Task<List<User>> GetAllUsersAsync()
        {
            // 只取一种 Key 前缀，防止数据重复
            var list = _db.GetAllByPrefix<User>("user:id:");
            return Task.FromResult(list);
        }

        public void Dispose()
        {
            if (_ownsConnection)
            {
                _db?.Dispose();
            }
        }
    }
    public class FasterUserStoreService : IDisposable
    {
        private readonly FasterKvService _db;
        private readonly bool _ownsConnection;

        // 构造函数
        public FasterUserStoreService(FasterKvService dbService)
        {
            _db = dbService;
            _ownsConnection = false;
        }

        public FasterUserStoreService() : this(new FasterKvService())
        {
            _ownsConnection = true;
        }
        public Task<User?> GetByUserNameAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return Task.FromResult<User?>(null);
            var a = _db.Get<User>($"user:username:{userName}");
            return Task.FromResult<User?>(a);
        }

        public Task<User?> GetByIdAsync(int userId)
        {
            if (userId <= 0) return Task.FromResult<User?>(null);
            var u = _db.Get<User>($"user:id:{userId}");
            return Task.FromResult<User?>(u);
        }

        // 核心保存逻辑
        public Task SaveUserAsync(User user)
        {
            if (string.IsNullOrEmpty(user.UserName) || user.Id == 0)
                throw new ArgumentException("Invalid user data: Missing UserName or ID");

            // 更新修改时间
            user.UpdatedAt = DateTime.UtcNow;

            // 模拟 LevelDB 的双重索引 (冗余存储)
            // LiteDB 的 Batch 操作保证了原子性：要么都成功，要么都失败
            var puts = new Dictionary<string, object>
            {
                [$"user:username:{user.UserName}"] = user,
                [$"user:id:{user.Id}"] = user
            };

            _db.Batch(puts);
            return Task.CompletedTask;
        }

        // 创建用户：处理 ID 生成和冲突
        public async Task<User> CreateUserAsync(string userName)
        {
            // 1. 检查用户名是否存在
            var existingUser = await GetByUserNameAsync(userName);
            if (existingUser != null)
            {
                // 如果是幂等设计，可以直接返回旧用户；如果是严格创建，抛异常
                // 这里为了安全起见，如果不慎重复调用，返回已存在的
                return existingUser;
                // 或者: throw new InvalidOperationException ($"User '{userName}' already exists");
            }

            var user = new User(userName);
            // 2. 生成唯一 ID (带冲突重试)
            var rnd = Random.Shared;
            int newId;
            bool idExists;
            newId = rnd.Next(100000, 1000000);
            user.Id = newId;

            // 3. 保存
            await SaveUserAsync(user);
            return user;
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return;

            var deletes = new List<string>
            {
                $"user:username:{user.UserName}",
                $"user:id:{userId}"
            };

            _db.Batch(null, deletes);
            await Task.CompletedTask;
        }

        public Task<List<User>> GetAllUsersAsync()
        {
            // 只取一种 Key 前缀，防止数据重复
            var list = _db.GetAllByPrefix<User>("user:id:");
            return Task.FromResult(list);
        }

        public void Dispose()
        {
            if (_ownsConnection)
            {
                _db?.Dispose();
            }
        }
    }

    // User.cs
    public class User
    {
        // 无参构造函数对 LiteDB 和 System.Text.Json 都是必须的
        public User()
        {
            // 初始化集合，防止空引用
            Decks = new Dictionary<int, Deck>();
            EquippedItem = new List<Item>();
            Items = new List<Item>();
        }

        public User(string userName) : this()
        {
            UserName = userName;
            // 默认值
            Name = "XDLG";
            Locale = "zh-Hans";
            Tag = Random.Shared.Next(1000, 9999);
            Banned = false;
        }

        public int Id { get; set; }
        public string UserName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Locale { get; set; } = "";
        public int Tag { get; set; }

        // System.Text.Json 支持 Dictionary<int, T> 的序列化
        // 会自动将 int key 转为 string key ("1": {...})
        public Dictionary<int, Deck> Decks { get; set; }

        public List<Item> EquippedItem { get; set; }
        public List<Item> Items { get; set; }
        public bool Banned { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Deck
    {
        public Deck()
        {
        }

        public Deck(CreateDeck createDeck, int playerId)
        {
            Name = createDeck.Name;
            MainFaction = createDeck.MainFaction;
            AllyFaction = createDeck.AllyFaction;
            CardBack = $"cardback_starter_{createDeck.MainFaction.ToLower()}";
            DeckCode = createDeck.DeckCode;
            Favorite = false;
            Id = Random.Shared.Next(100000, 999999);
            PlayerId = playerId;
            LastPlayed = DateTime.Now;
            CreateDate = DateTime.Now;
            ModifyDate = DateTime.Now;
        }

        public string Name { get; set; } = "";
        public string MainFaction { get; set; } = "";
        public string AllyFaction { get; set; } = "";
        public string CardBack { get; set; } = "";
        public string DeckCode { get; set; } = "";
        public bool Favorite { get; set; }
        public int Id { get; set; }
        public int PlayerId { get; set; }

        [JsonIgnore]
        public DateTime LastPlayed { get; set; }

        public string LastPlayedString
        {
            get => LastPlayed.ToString("o");
            set => LastPlayed = DateTime.Parse(value);
        }

        [JsonIgnore]
        public DateTime CreateDate { get; set; }

        public string CreateDateString
        {
            get => CreateDate.ToString("o");
            set => CreateDate = DateTime.Parse(value);
        }

        [JsonIgnore]
        public DateTime ModifyDate { get; set; }

        public string ModifyDateString
        {
            get => ModifyDate.ToString("o");
            set => ModifyDate = DateTime.Parse(value);
        }
    }
    public class Item
    {
        public Item()
        {
        }

        public Item(string faction, string itemId, string slot)
        {
            Faction = faction;
            ItemId = itemId;
            Slot = slot;
        }

        public string Faction { get; set; } = "";
        public string ItemId { get; set; } = "";
        public string Slot { get; set; } = "";
    }

    public class MatchInfo
    {
        public MatchInfo()
        {
        }

        public MatchInfo(int matchId, LobbyPlayer left, LobbyPlayer right)
        {
            MatchId = matchId;
            Left = left;
            Right = right;
            LeftActions = new List<MatchAction>();
            RightActions = new List<MatchAction>();
            PlayerStatusLeft = "not_done";
            PlayerStatusRight = "not_done";
            LeftMinactionid = 0;
            RightMinactionid = 0;
        }

        public int MatchId { get; set; }
        public dynamic? MatchStartingInfo { get; set; }
        public LobbyPlayer? Left { get; set; }
        public LobbyPlayer? Right { get; set; }
        public List<MatchAction> LeftActions { get; set; } = new();
        public List<MatchAction> RightActions { get; set; } = new();
        public string PlayerStatusLeft { get; set; } = "not_done";
        public string PlayerStatusRight { get; set; } = "not_done";
        public MulliganResult? MulliganLeft { get; set; }
        public MulliganResult? MulliganRight { get; set; }
        public List<MatchCard> LeftDeck { get; set; } = new();
        public List<MatchCard> RightDeck { get; set; } = new();
        public List<MatchCard> LeftHand { get; set; } = new();
        public List<MatchCard> RightHand { get; set; } = new();
        public int LeftMinactionid { get; set; }
        public int RightMinactionid { get; set; }
        public string? WinnerSide { get; set; }

        public bool HasPlayer(int playerId)
        {
            return Left?.PlayerId == playerId || Right?.PlayerId == playerId;
        }

        public LobbyPlayer? GetPlayerById(int playerId)
        {
            return Left?.PlayerId == playerId ? Left : Right;
        }

        public List<MatchAction> GetActionsById(int playerId)
        {
            return Left?.PlayerId == playerId ? LeftActions : RightActions;
        }
    }
    // 静态常量和工具类
    public static class GameConstants
    {
        // 位置常量
        public const string DeckLeft = "deck_left";
        public const string DeckRight = "deck_right";
        public const string BoardHqLeft = "board_hqleft";
        public const string BoardHqRight = "board_hqright";
        public const string HandLeft = "hand_left";
        public const string HandRight = "hand_right";

        // 玩家状态常量
        public const string NotDone = "not_done";
        public const string MulliganDone = "mulligan_done";
        public const string EndMatch = "end_match";

        // 比赛状态常量
        public const string Pending = "pending";
        public const string Running = "running";
        public const string Finished = "finished";

        // 派系列表
        public static readonly List<string> MainFactions = new() { "Germany", "Britain", "Soviet", "USA", "Japan" };
        public static readonly List<string> AllyFactions = new() { "Germany", "Britain", "Soviet", "USA", "Japan", "France", "Italy", "Poland", "Finland" };

        // 动作类型
        public const string XActionCheat = "XActionCheat";
    }
    // 游戏库和物品相关的record类型
    public record LibraryItem(
        string CardType,
        int Count,
        int GoldCardCount,
        int Id,
        int RecentlyCraftedCount
    );

    public record LibraryResponse(
        List<LibraryItem> Cards,
        List<object> NewCards
    );

    // WebSocket消息相关的record类型
    public record WsNotification(
        string Message,
        string Channel,
        string Context,
        DateTime Timestamp,
        string Sender,
        string Receiver
    );

    // 错误响应相关的record类型
    public record ErrorResponse(
        string Error,
        string Message,
        int StatusCode
    );

    public record BannedResponse(
        Error Error,
        string Message,
        int StatusCode
    );

    public record Error(
        string Code,
        string Description
    );

    // 配置相关的record类型
    public record ServerOptions(
        int NuiMobile = 1,
        Dictionary<string, Dictionary<string, List<string>>> ScalabilityOverride = null,
        double AppscaleDesktopDefault = 1.0,
        double AppscaleDesktopMax = 1.4,
        double AppscaleMobileDefault = 1.4,
        double AppscaleMobileMax = 1.4,
        double AppscaleMobileMin = 1.0,
        double AppscaleTabletMin = 1.0,
        int BattleWaitTime = 60,
        string Websocketurl = "",
        string HomefrontDate = "2025.11.27-09.00.00"
    );

    public record SessionResponse(
        string AchievementsUrl,
        List<object> AllKnockoutTourneys,
        int BritainLevel,
        int BritainLevelClaimed,
        int BritainXp,
        List<object> CardsBlacklist,
        int ClaimableCrateLevel,
        int ClientId,
        string Currency,
        Dictionary<string, object> CurrentKnockoutTourney,
        string DailymissionsUrl,
        Dictionary<string, object> Decks,
        string DecksUrl,
        int Diamonds,
        string DoubleXpEndDate,
        int DraftAdmissions,
        int Dust,
        string? Email,
        bool EmailRewardReceived,
        bool EmailVerified,
        bool ExtendedRewards,
        int GermanyLevel,
        int GermanyLevelClaimed,
        int GermanyXp,
        int Gold,
        bool HasBeenOfficer,
        string HeartbeatUrl,
        bool IsOfficer,
        bool IsOnline,
        int JapanLevel,
        int JapanLevelClaimed,
        int JapanXp,
        string Jti,
        string Jwt,
        string LastCrateClaimedDate,
        string? LastDailyMissionCancel,
        string LastDailyMissionRenewal,
        string LastLogonDate,
        List<object> LaunchMessages,
        string LibraryUrl,
        string LinkerAccount,
        string Locale,
        Dictionary<string, object> Misc,
        List<object> NewCards,
        Dictionary<string, object> NewPlayerLoginReward,
        bool Npc,
        bool OnlineFlag,
        string PacksUrl,
        int PlayerId,
        string PlayerName,
        string PlayerTag,
        List<object> Rewards,
        string SeasonEnd,
        int SeasonWins,
        string ServerOptions,
        string ServerTime,
        int SovietLevel,
        int SovietLevelClaimed,
        int SovietXp,
        int Stars,
        int TutorialsDone,
        List<string> TutorialsFinished,
        int UsaLevel,
        int UsaLevelClaimed,
        int UsaXp,
        int UserId
    );
}
