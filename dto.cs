using Newtonsoft.Json;

namespace fyserver
{
    public class a{
    public static readonly LibraryResponse Library = new(
        new List<LibraryItem>
        {
            new("card_unit_seahawk", 4, 5, 2001, 0)
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
}
    public record msg
    {
        public string match_id = "";
        public string message = "";
        public string channel = "";
        public string context = "";
        public long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public int sender;
        public string receiver = "";
    }
    public record Action
    {
        public string action;
        public string value;
        public string deck_code;
    }
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

    // Session DTOs as records
    public record Session(
        string Provider,
        string ClientType,
        string Build,
        string PlatformType,
        string AppGuid,
        string Version,
        string PlatformInfo,
        string PlatformVersion,
        string AccountLinking,
        string Language,
        string AutomaticAccountCreation,
        string Username,
        string Password
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
        int PlayerId,
        string Provider,
        List<string> Roles,
        string Tier,
        int UserId,
        string UserName
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
        string Message,
        string Channel,
        string Context,
        DateTime Timestamp,
        string Sender,
        string Receiver,
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
        private readonly RocksDbService _rocksDb;
        private readonly object _lock = new object();

        public UserStoreService()
        {
            _rocksDb = new RocksDbService("./db/users.db");
        }

        // 通过用户名获取用户
        public async Task<User?> GetByUserNameAsync(string userName)
        {
            return await _rocksDb.GetAsync<User>($"user:username:{userName}");
        }

        // 通过ID获取用户
        public async Task<User?> GetByIdAsync(int userId)
        {
            return await _rocksDb.GetAsync<User>($"user:id:{userId}");
        }

        // 保存用户
        public async Task SaveUserAsync(User user)
        {
            if (string.IsNullOrEmpty(user.UserName) || user.Id == 0)
                throw new ArgumentException("Invalid user data");

            var puts = new Dictionary<string, object>
            {
                [$"user:username:{user.UserName}"] = user,
                [$"user:id:{user.Id}"] = user
            };

            await _rocksDb.BatchAsync(puts);
        }

        // 创建新用户
        public async Task<User> CreateUserAsync(string userName)
        {
            // 检查用户名是否已存在
            var existingUser = await GetByUserNameAsync(userName);
            if (existingUser != null)
                throw new InvalidOperationException($"User with username '{userName}' already exists");

            var user = new User(userName);
            await SaveUserAsync(user);
            return user;
        }

        // 更新用户
        public async Task UpdateUserAsync(User user)
        {
            await SaveUserAsync(user);
        }

        // 删除用户
        public async Task DeleteUserAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
                return;

            var deletes = new List<string>
        {
            $"user:username:{user.UserName}",
            $"user:id:{userId}"
        };

            await _rocksDb.BatchAsync(new Dictionary<string, object>(), deletes);
        }

        // 获取所有用户
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _rocksDb.GetAllByPrefixAsync<User>("user:id:");
        }

        // 搜索用户
        public async Task<List<User>> SearchUsersAsync(string searchTerm)
        {
            var allUsers = await GetAllUsersAsync();
            return allUsers
                .Where(u => u.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           u.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // 检查用户是否存在
        public async Task<bool> UserExistsAsync(string userName)
        {
            return await _rocksDb.ExistsAsync($"user:username:{userName}");
        }

        // 获取在线用户数
        public async Task<int> GetOnlineUserCountAsync()
        {
            var allUsers = await GetAllUsersAsync();
            // 注意：这里需要WebSocket连接来判断在线状态
            // 暂时返回所有用户数作为简化
            return allUsers.Count;
        }

        public void Dispose()
        {
            _rocksDb?.Dispose();
        }
    }
    // User.cs (更新版)
    public class User
    {
        [JsonIgnore]
        private UserStoreService? _userStore;

        public User()
        {
        }

        public User(string userName)
        {
            Id = Random.Shared.Next(100000, 999999);
            UserName = userName;
            Name = "XDLG";
            Tag = Random.Shared.Next(1000, 9999);
            Locale = "zh-Hans";
            Decks = new Dictionary<int, Deck>();
            EquippedItem = new List<Item>();
            Items = new List<Item>();
            Banned = false;
        }

        public int Id { get; set; }
        public string UserName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Locale { get; set; } = "";
        public int Tag { get; set; }

        [JsonIgnore]
        public Dictionary<int, Deck> Decks { get; set; } = new();

        public List<Deck> DecksList
        {
            get => Decks.Values.ToList();
            set
            {
                Decks = new Dictionary<int, Deck>();
                foreach (var deck in value)
                {
                    Decks[deck.Id] = deck;
                }
            }
        }

        public List<Item> EquippedItem { get; set; } = new();
        public List<Item> Items { get; set; } = new();
        public bool Banned { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // 设置用户存储服务（依赖注入）
        [JsonIgnore]
        public UserStoreService UserStore
        {
            set => _userStore = value;
        }

        // 保存用户到数据库
        public async Task StoreAsync()
        {
            UpdatedAt = DateTime.UtcNow;
                await _userStore.SaveUserAsync(this);
        }

        // 从数据库加载用户
        public static async Task<User?> LoadAsync(string userName)
        {
            return await config.appconfig.users.GetByUserNameAsync(userName);
        }

        public static async Task<User?> LoadAsync(int userId)
        {
            return await config.appconfig.users.GetByIdAsync(userId);
        }

        // 删除用户
        public async Task DeleteAsync()
        {
            if (_userStore != null)
            {
                await _userStore.DeleteUserAsync(Id);
            }
            else
            {
                await config.appconfig.users.DeleteUserAsync(Id);
            }
        }
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
