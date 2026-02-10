using Microsoft.AspNetCore.Mvc;
using System.Buffers.Text;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace fyserver
{
    public class http
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();

        public static async Task<int> GetPlayerIdFromAuthAsync(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null) {
                authHeader = context.Request.Headers["authorization"].FirstOrDefault();
            }
            User? a = null;
            if (authHeader != null && authHeader.StartsWith("JWT "))
                a = await GlobalState.users.GetByUserNameAsync(Decode(authHeader["JWT ".Length..],out var actionId));
            else
                return 0;
            if (a != null)
            {
                Console.WriteLine($"Authorization header found: {Decode(authHeader["JWT ".Length..],out var actionId)}");
                return a.Id;
            }
            return 0;
        }
        public static async Task<User?> GetUserFromAuthAsync(HttpContext context)
        {
            var playerId = await GetPlayerIdFromAuthAsync(context);
            Console.WriteLine($"Extracted player ID from auth: {playerId}");
            if (playerId > 0)
            {
                // 纯粹的数据获取，不需要注入 UserStore
                return await GlobalState.users.GetByIdAsync(playerId);
            }
            return null;
        }
        [DllImport("codec.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int _xR7qM2vP(string plaintext, int actionId, StringBuilder output, int outputBufSize);
        [DllImport("codec.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int _kW3nJ9tF(string encoded, StringBuilder plaintextOut, int plaintextBufSize, ref int actionIdOut);
        public static string Encode(string plaintext, int actionId)
        {
            StringBuilder sb = new StringBuilder(plaintext.Length * 4 + 256);
            int len = _xR7qM2vP(plaintext, actionId, sb, sb.Capacity);
            if (len < 0)
                throw new Exception("编码失败");
            return sb.ToString();
        }

        public static string Decode(string encoded, out int actionId)
        {
            StringBuilder sb = new StringBuilder(encoded.Length + 256);
            actionId = 0;
            int len = _kW3nJ9tF(encoded, sb, sb.Capacity, ref actionId);
            if (len < 0)
                throw new Exception("解码失败");
            return sb.ToString();
        }
        async public Task StartHttpServer()
        {
            PlayerLibrary.InitLibrary("./library/deckCodeIDsTable2.json", "./library/emojiLib.json", "./library/cardbackLib.json");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("XXXXXXXXXX");
            Console.ForegroundColor = ConsoleColor.White;
            builder.Services.AddOpenApi();
            builder.WebHost.UseUrls(config.appconfig.getAddressHttp());
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            });
            builder.Services.AddRouting();
            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            void UseMiddleWares()
            {
                app.Use(async (context, next) =>
                {
                    var path = context.Request.Path.Value;
                    if (!string.IsNullOrEmpty(path) && path.Contains("//"))
                    {
                        while (path.Contains("//"))
                        {
                            path = path.Replace("//", "/");
                        }
                        context.Request.Path = path;
                    }
                    await next();
                });

                app.Use(async (context, next) =>
                {
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers["Content-Type"] = context.Response.Headers["Content-Type"].ToString().Replace("; charset=utf-8", "");
                        return Task.CompletedTask;
                    });
                    await next();
                });
            }
            void MakeUserEndpoints(){
            app.MapPost("/session", async (Session session) =>
            {
                string addressHttp = config.appconfig.getAddressHttpR();
                User? user = null;
                try
                {
                    user = await GlobalState.users.GetByUserNameAsync(session.Username);
                    Console.WriteLine($"Find user: {session.Username}");
                }
                catch (Exception)
                {
                }
                if (user == null)
                {
                    Console.WriteLine("未找到");
                    try
                    {
                        user = await GlobalState.users.CreateUserAsync(session.Username);
                        Console.WriteLine($"Created new user: {session.Username}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.InnerException.Message);
                        // 用户已存在或其他错误
                        return Results.BadRequest(ex.Message);
                    }
                }

                if (user.Banned)
                {
                    return Results.Json(
                        new BannedResponse(
                            new Error("user_error", "banned"),
                            "Forbidden",
                            403
                        ),
                        statusCode: 403
                    );
                }
                // 设置用户存储服务
                //

                var response = new SessionResponse(
                    AchievementsUrl: $"{addressHttp}/players/{user.Id}/achievements",
                    AllKnockoutTourneys: new List<object>(),
                    BritainLevel: 500,
                    BritainLevelClaimed: 500,
                    BritainXp: 0,
                    CardsBlacklist: new List<object>(),
                    ClaimableCrateLevel: 0,
                    ClientId: user.Id,
                    Currency: "USD",
                    CurrentKnockoutTourney: new Dictionary<string, object>(),
                    DailymissionsUrl: $"{addressHttp}/players/{user.Id}/dailymissions",
                    Decks: new Dictionary<string, object>
                    {
                        ["headers"] = user.Decks.Values.Select(d => new
                        {
                            d.Name,
                            d.MainFaction,
                            d.AllyFaction,
                            d.CardBack,
                            d.DeckCode,
                            d.Favorite,
                            d.Id,
                            d.PlayerId,
                            LastPlayed = d.LastPlayed.ToString("o"),
                            CreateDate = d.CreateDate.ToString("o"),
                            ModifyDate = d.ModifyDate.ToString("o")
                        }).ToList()
                    },
                    DecksUrl: $"{addressHttp}/players/{user.Id}/decks",
                    Diamonds: 99999,
                    DoubleXpEndDate: "2025-07-03T12:13:36.889692Z",
                    DraftAdmissions: 1,
                    Dust: 1000,
                    Email: null,
                    EmailRewardReceived: false,
                    EmailVerified: false,
                    ExtendedRewards: false,
                    GermanyLevel: 500,
                    GermanyLevelClaimed: 500,
                    GermanyXp: 0,
                    Gold: 999999,
                    HasBeenOfficer: true,
                    HeartbeatUrl: $"{addressHttp}/players/{user.Id}/heartbeat",
                    IsOfficer: true,
                    IsOnline: true,
                    JapanLevel: 500,
                    JapanLevelClaimed: 500,
                    JapanXp: 0,
                    Jti: "114514",
                    Jwt: $"{Encode(session.Username,114)}",
                    LastCrateClaimedDate: "2025-07-02T11:24:15.567042Z",
                    LastDailyMissionCancel: null,
                    LastDailyMissionRenewal: "2025-07-05T15:21:43.653915Z",
                    LastLogonDate: "2025-07-05T15:21:06.168847Z",
                    LaunchMessages: new List<object>(),
                    LibraryUrl: $"{addressHttp}/players/{user.Id}/library",
                    LinkerAccount: "",
                    Locale: "zh-hans",
                    Misc: new Dictionary<string, object>
                    {
                        ["createDate"] = "2025-07-02T11:24:15.529671Z",
                        ["featuredAchievements"] = new List<object>()
                    },
                    NewCards: new List<object>(),
                    NewPlayerLoginReward: new Dictionary<string, object>
                    {
                        ["day"] = 8,
                        ["reset"] = "0001-01-01 00:00:00",
                        ["seconds"] = 0
                    },
                    Npc: false,
                    OnlineFlag: true,
                    PacksUrl: $"{addressHttp}/players/{user.Id}/packs",
                    PlayerId: user.Id,
                    PlayerName: user.Name,
                    PlayerTag: user.Tag.ToString(),
                    Rewards: new List<object>(),
                    SeasonEnd: "2025-08-01T00:00:00Z",
                    SeasonWins: 9999,
                    ServerOptions: File.Exists("./config/serverOptions.json") ? File.ReadAllText("./config/serverOptions.json").Replace("{WsAddress}", config.appconfig.getAddressWsR()) : "",//'{"nui_mobile": 1, "scalability_override": {"Android_Low": {"console_commands": ["r.Screenpercentage 100"]}, "Android_Mid": {"console_commands": ["r.Screenpercentage 100"]}, "Android_High": {"console_commands": ["r.Screenpercentage 100"]}}, "appscale_desktop_default": 1.0, "appscale_desktop_max": 1.4, "appscale_mobile_default": 1.4, "appscale_mobile_max": 1.4, "appscale_mobile_min": 1.0, "appscale_tablet_min": 1.0, "battle_wait_time": 60, "nui_mobile": 1, "scalability_override": {"Android_Low": {"console_commands": ["r.Screenpercentage 100"]}, "Android_Mid": {"console_commands": ["r.Screenpercentage 100"]}, "Android_High": {"console_commands": ["r.Screenpercentage 100"]}},"websocketurl": "ws://127.0.0.1:5232","homefront_date":"2025.11.27-09.00.00"}',
                    ServerTime: DateTime.UtcNow.ToString("yyyy.MM.dd-HH.mm.ss"),
                    SovietLevel: 500,
                    SovietLevelClaimed: 500,
                    SovietXp: 0,
                    Stars: 120,
                    TutorialsDone: 0,
                    TutorialsFinished: new List<string>
                    {
            "unlocking_germany_1",
            "unlocking_germany_2",
            "unlocking_germany_0",
            "germany_cards_rewarded",
            "unlocking_usa_8",
            "recruit_missions_done",
            "draft_1",
            "draft_ally",
            "draft_kredits",
            "unlocking_japan_0",
            "japan_cards_rewarded",
            "unlocking_soviet_0",
            "soviet_cards_rewarded",
            "unlocking_usa_0",
            "usa_cards_rewarded",
            "unlocking_britain_0",
            "britain_cards_rewarded"
                    },
                    UsaLevel: 500,
                    UsaLevelClaimed: 500,
                    UsaXp: 0,
                    UserId: user.Id
                );
                return Results.Ok(response);
            });
            // 在 http.cs 中
            // 2. 配置和基本信息
            app.MapGet("/", async (HttpContext context) =>
            {
                var auth = (context.Request.Headers["Authorization"].FirstOrDefault());
                // Console.WriteLine(auth);
                if (auth == null)
                {
                    auth = "JWT 1939Mother";
                }
                string userName = "1939Mother";
                try
                {
                    userName = Decode(auth["JWT ".Length..],out var a);
                }
                catch {
                    if (auth is not "JWT 1939Mother")
                        return Results.BadRequest("Invalid Authorization header");
                }
                User? user = await GlobalState.users.GetByUserNameAsync(userName);
                if (user == null)
                    try
                    {
                        user = await GlobalState.users.CreateUserAsync(userName);
                        Console.WriteLine($"Created new user: {userName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.InnerException.Message);
                        // 用户已存在或其他错误
                        return Results.BadRequest(ex.Message);
                    }
                return Results.Ok(await config.appconfig.getConfigAsync(user));
            });// 3. 玩家管理
        }
            void MakePlayerEndpoints()
            {
                //TODO: 完善entitlements接口,用于处理所有权
                // FP接口 - 前端展示（Front Page）
                app.MapGet("/fp/", (HttpContext context) =>
            {
                var fpt = File.Exists("./config/frontpage.json") ? File.ReadAllText("./config/frontpage.json") : "{}";
                JsonDocument fp1 = JsonDocument.Parse(fpt);
                return Results.Ok(
                    fp1
                    );
            });
                // Store V2接口 - 商店数据
                app.MapGet("/store/v2/", (HttpContext context, string? provider) =>
                {
                    var storeConfig = GlobalState.GetStoreConfig();
                    var now = DateTime.UtcNow;
                    var timestamp = (now - new DateTime(1970, 1, 1)).TotalSeconds;

                    var response = new StoreResponse(
                        Currency: storeConfig.Currency,
                        Groups: storeConfig.Groups,
                        AlwaysFeatured: storeConfig.AlwaysFeatured,
                        Message: $"Offers for {now:yyyy-MM-ddTHH:mm:ss.ffffffZ}",
                        Status: 200,
                        Ts: timestamp
                    );

                    return Results.Ok(response);
                });
                app.MapPost("/store/v2/txn", () =>
                {
                    return Results.Ok();
                });
                app.MapGet("/entitlements/{id}", (HttpContext context) =>
                {
                    List<Entitlement> e = new();
                    e.Add(new Entitlement
                    (
                        EntitlementType: "emote",
                        Name: "emote_appreciate"
                    ));
                    return Results.Ok(e);
                });
                app.MapGet("/{a}/players/{player_id}/friends", async (HttpContext context) =>
                {
                    List<int> nil = new();
                    return Results.Ok(new FriendsReponse(Friends: nil, PreviousOpponents: nil));
                });
                app.MapMethods("/players/{id}/heartbeat", new[] { "PUT", "DELETE" }, (string id) =>
                {
                    return Results.Ok(new { });
                });
                app.MapMethods("/players/notifications/{id}", new[] { "PUT", "DELETE" }, (string id) =>
                {
                    return Results.Ok(new { });
                });
                // 4. 卡牌库和卡组
                app.MapGet("/players/{id}/librarynew", (string id) =>
                {
                    return Results.Ok(PlayerLibrary.Library);
                });
            }
            void MakeDecksEndpoints()
            {
                app.MapPost("/players/{id}/decks", async (string id, CreateDeck createDeck) =>
                {
                    var user = await GlobalState.users.GetByIdAsync(int.Parse(id));
                    if (user == null)
                        return Results.NotFound($"User with ID {id} not found");
                    var deck = new Deck(createDeck, user.Id);
                    user.Decks[deck.Id] = deck;

                    await GlobalState.users.SaveUserAsync(user);

                    return Results.Ok(new
                    {
                        deck.Id,
                        deck.Name,
                        deck.MainFaction,
                        deck.AllyFaction,
                        deck.CardBack,
                        deck.DeckCode,
                        deck.Favorite,
                        deck.PlayerId,
                        LastPlayed = deck.LastPlayed.ToString("o"),
                        CreateDate = deck.CreateDate.ToString("o"),
                        ModifyDate = deck.ModifyDate.ToString("o")
                    });
                });

                app.MapPut("/players/{player_id}/decks/{deck_id}", async (string player_id, int deck_id, [FromBody] DeckAction action) =>
                {
                    var user = await GlobalState.users.GetByIdAsync(int.Parse(player_id));
                    if (user == null)
                        return Results.NotFound($"User with ID {player_id} not found");
                    if (user.Decks.TryGetValue(deck_id, out var deck))
                    {
                        Console.WriteLine(user.Decks[deck_id].Name);
                        Console.WriteLine(action.ToString());
                        switch (action.Action)
                        {
                            case "fill":
                                Console.WriteLine(deck.Name);
                                Console.WriteLine(action.DeckCode);
                                Console.WriteLine(action.ToString());
                                deck.DeckCode = action.DeckCode;
                                deck.ModifyDate = DateTime.Now;
                                break;
                        }
                        await GlobalState.users.SaveUserAsync(user);
                    }
                    Console.WriteLine((await GlobalState.users.GetByIdAsync(int.Parse(player_id))).Decks[deck_id].DeckCode);
                    return Results.Ok(new { });
                });

                app.MapPut("/players/{player_id}/decks/", async (string player_id, ChangeDeck changeDeck) =>
                {
                    var user = await GlobalState.users.GetByIdAsync(int.Parse(player_id));
                    if (user == null)
                        return Results.NotFound($"User with ID {player_id} not found");
                    if (user.Decks.TryGetValue(changeDeck.Id, out var deck))
                    {
                        switch (changeDeck.Action)
                        {
                            case "rename":
                                deck.Name = changeDeck.Name;
                                deck.ModifyDate = DateTime.Now;
                                break;
                            case "change_card_back":
                                deck.CardBack = changeDeck.Name;
                                deck.ModifyDate = DateTime.Now;
                                break;
                            case "make_favorite":
                                user.Name = deck.Name;
                                deck.Favorite = true;
                                deck.ModifyDate = DateTime.Now;
                                break;
                        }

                        await GlobalState.users.SaveUserAsync(user);
                    }

                    return Results.Ok(new { });
                });

                app.MapDelete("/players/{player_id}/decks/{deck_id}", async (string player_id, int deck_id) =>
                {
                    var user = await GlobalState.users.GetByIdAsync(int.Parse(player_id));
                    if (user == null)
                        return Results.NotFound($"User with ID {player_id} not found");
                    user.Decks.Remove(deck_id);
                    await GlobalState.users.SaveUserAsync(user);

                    return Results.Ok(new { });
                });
                //这个端点获取卡组详情（预制卡组），暂未使用
                app.MapGet("/items/decks/{id}", async (string id) =>
                {
                    return Results.Ok("");
                });
            }
            // 5. 物品装备
            app.MapGet("/items/{id}", async (string id) =>
            {
                var user = await GlobalState.users.GetByIdAsync(int.Parse(id));
                if (user == null)
                    return Results.NotFound($"User with ID {id} not found");
                var response = new ItemsResponse(
                    Date: DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
        EquippedItems: user.EquippedItem,
                    Items: PlayerLibrary.Items.ToList()
                );

                return Results.Ok(response);
            });
            app.MapPost("/items/{id}", async (string id, EquippedItem item) =>
            {
                var user = await GlobalState.users.GetByIdAsync(int.Parse(id));
                if (user == null)
                    return Results.NotFound($"User with ID {id} not found");
                if (user.EquippedItem == null)
                    user.EquippedItem = new List<EquippedItem>();
                // 移除相同槽位的装备
                user.EquippedItem.RemoveAll(i => i.Slot == item.Slot);
                // 添加新装备
                user.EquippedItem.Add(item);
                await GlobalState.users.SaveUserAsync(user);
                return Results.Created();
            });
            // 6. 匹配系统
            //现在的lobby还只是单人匹配，后续会增加战斗码匹配和分段匹配
            app.MapPost("/lobbyplayers", async (LobbyPlayer lobbyPlayer, HttpContext context) =>
             {
                 var user = await GlobalState.users.GetByIdAsync(lobbyPlayer.PlayerId);
                 if (user == null)// || user.Name == "XDLG")
                 {
                     // TODO: WebSocket断开连接消息
                     //context.Connection.Close();
                     context.Connection.RequestClose();
                     return Results.BadRequest("问号问号问号");
                 }
                 // 检查卡组有效性（简化）
                 if (!user.Decks.TryGetValue(lobbyPlayer.DeckId, out var deck))
                 {
                     // context.Connection.Close();
                     context.Connection.RequestClose();
                     return Results.BadRequest("无效卡组");
                 }
                 // 对战码匹配
                 if (lobbyPlayer.ExtraData.StartsWith("battle_code:"))
                 {
                     var code = lobbyPlayer.ExtraData;
                     if (!config.appconfig.BattleCodePlayers.ContainsKey(code))
                         config.appconfig.BattleCodePlayers[code] = new List<LobbyPlayer>();

                     var players = config.appconfig.BattleCodePlayers[code];
                     players.Add(lobbyPlayer);

                     if (players.Count >= 2)
                     {
                         var matchId = Random.Shared.Next(100000, 999999);
                         var matchInfo = new MatchInfo(matchId, players[0], players[1]);
                         players.RemoveAt(0);
                         players.RemoveAt(0);

                         config.appconfig.MatchedPairs[matchId] = matchInfo;
                     }
                 }
                 // 普通匹配
                 else if (lobbyPlayer.ExtraData == "")
                 {
                     Console.WriteLine("检测到匹配：" + lobbyPlayer.ToString());
                     config.appconfig.WaitingPlayers1.Add(lobbyPlayer);
                     Console.WriteLine($"当前有{config.appconfig.WaitingPlayers1.Count}");
                     //单人对战
                     if (config.appconfig.WaitingPlayers1.Count >= 1)
                     {
                         var matchId = Random.Shared.Next(100000, 999999);
                         //var matchInfo = new MatchInfo(matchId, config.appconfig.WaitingPlayers1[0], config.appconfig.WaitingPlayers1[1]);
                         //config.appconfig.MatchedPairs[matchId] = matchInfo;
                         //config.appconfig.WaitingPlayers1.RemoveAt(0);
                         //config.appconfig.WaitingPlayers1.RemoveAt(0);

                         //上面的是双人，下面的是单人
                         var matchInfo = new MatchInfo(matchId, config.appconfig.WaitingPlayers1[0], config.appconfig.WaitingPlayers1[0]);
                         config.appconfig.MatchedPairs[matchId] = matchInfo;
                         config.appconfig.WaitingPlayers1.RemoveAt(0);
                         //config.appconfig.WaitingPlayers1.RemoveAt(0);
                         Console.WriteLine("匹配成功，信息为" + matchInfo.ToString());
                     }
                 }
                 else
                 {
                     config.appconfig.WaitingPlayers2.Add(lobbyPlayer);
                     if (config.appconfig.WaitingPlayers2.Count >= 2)
                     {
                         var matchId = Random.Shared.Next(100000, 999999);
                         var matchInfo = new MatchInfo(matchId, config.appconfig.WaitingPlayers2[0], config.appconfig.WaitingPlayers2[1]);
                         config.appconfig.WaitingPlayers2.RemoveAt(0);
                         config.appconfig.WaitingPlayers2.RemoveAt(0);
                         config.appconfig.MatchedPairs[matchId] = matchInfo;
                         Console.WriteLine("匹配成功，信息为" + matchInfo.ToString());
                     }
                 }
                 return Results.Text("OK");
             });
            app.MapDelete("/lobbyplayers", ([FromBody] LobbyPlayer lobbyPlayer) =>
            {
                config.appconfig.WaitingPlayers1.RemoveAll(p => p.PlayerId == lobbyPlayer.PlayerId);
                config.appconfig.WaitingPlayers2.RemoveAll(p => p.PlayerId == lobbyPlayer.PlayerId);
                foreach (var code in config.appconfig.BattleCodePlayers.Keys)
                {
                    config.appconfig.BattleCodePlayers[code].RemoveAll(p => p.PlayerId == lobbyPlayer.PlayerId);
                }
                return Results.Ok(new { status = 200 });
            });
            void MakeMatchEndpoints()
            {
                MatchAction decryptMA(MatchActionEn matchActionen) {
                    var matchAction = new MatchAction();
                    Console.ForegroundColor = ConsoleColor.Green;
                    try
                    {
                        int actionId = 0;
                        string plaintext = Decode(matchActionen.A, out actionId);
                        matchAction=JsonSerializer.Deserialize<MatchAction>(plaintext,GameConstants.JsonOptions);
                        Console.WriteLine("ActionId：" + actionId);
                        Console.WriteLine("解密结果：" + plaintext);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("解密失败，使用原始数据");
                        Console.WriteLine("原始数据：" + matchActionen.A);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex?.Message);
                        Console.WriteLine(ex?.InnerException?.Message);
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                    return matchAction;
                }
                app.MapGet("/matches/v2", async (HttpContext context) =>
                {
                    var user = await GetUserFromAuthAsync(context);
                    if (user == null)
                        return Results.Unauthorized();
                    Console.WriteLine("正在获取匹配信息，用户ID：" + user.Id);
                    MatchInfo? match = null;
                    Console.WriteLine(config.appconfig.MatchedPairs.Count);
                    foreach (var kvp in config.appconfig.MatchedPairs)
                    {
                        Console.WriteLine(kvp.Value.LeftDeck);
                        if (string.IsNullOrEmpty(kvp.Value.WinnerSide) && kvp.Value.HasPlayer(user.Id))
                        {
                            match = kvp.Value;
                            break;
                        }
                    }

                    if (match == null)
                        return Results.Text("null");

                    // TODO: 实现makeMatchStartingInfo逻辑
                    return Results.Ok(await MakeMatchStartingInfo(user.Id, match));
                });
                async Task<MatchStartingInfo> MakeMatchStartingInfo(int myId, MatchInfo match)
                {
                    var other = match.Left?.PlayerId == myId ? match.Right : match.Left;
                    Console.WriteLine("正在生成MatchStartingInfo，玩家ID：" + myId);
                    if (match.MatchStartingInfo != null)
                    {
                        return match.MatchStartingInfo;
                    }
                    Console.WriteLine("MatchStartingInfo不存在，正在生成...");
                    var leftUser = await GlobalState.users.GetByIdAsync(match.Left.PlayerId);
                    var rightUser = await GlobalState.users.GetByIdAsync(match.Right.PlayerId);
                    Console.WriteLine("双方为" + leftUser.Id + "," + rightUser.Id);
                    // 获取卡组信息
                    var leftDeck = leftUser.Decks[match.Left.DeckId];
                    var rightDeck = rightUser.Decks[match.Right.DeckId];

                    // 生成卡牌列表（简化版本，实际应该解析deck_code）
                    var (leftCards, leftLocation) = GetCardsFromDeck(leftDeck, 1, true);
                    var (rightCards, rightLocation) = GetCardsFromDeck(rightDeck, 41, false);

                    // 分离手牌和牌组
                    match.LeftDeck = leftCards.Skip(4).ToList();
                    match.RightDeck = rightCards.Skip(5).ToList();

                    match.LeftHand = leftCards.Take(4).Select((card, index) =>
                    {
                        card = card with { Location = "hand_left" };
                        //card = card with { LocationNumber = index };
                        return card;
                    }).ToList();

                    match.RightHand = rightCards.Take(5).Select((card, index) =>
                    {
                        card = card with { Location = "hand_right" };
                        //card = card with { LocationNumber = index };
                        return card;
                    }).ToList();
                    // 构建MatchStartingInfo
                    match.MatchStartingInfo = new MatchStartingInfo(
                        LocalSubactions: true,
                        MatchAndStartingData: new MatchAndStartingData(
                            Match: new MatchData(
                                ActionPlayerId: other?.PlayerId,
                                //单人
                                //ActionSide:"right",
                                ActionSide: other?.PlayerId == match.Left?.PlayerId ? "left" : "right",
                                Actions: new List<MatchAction>(),
                                ActionsUrl: $"{config.appconfig.getAddressHttpR()}/matches/v2/{match.MatchId}/actions",
                                CurrentActionId: 0,
                                CurrentTurn: 1,
                                DeckIdLeft: match.Left.DeckId,
                                DeckIdRight: match.Right.DeckId,
                                //单人对战特供
                                LeftIsOnline:1,
                                //LeftIsOnline:1,
                                MatchId: match.MatchId,
                                MatchType: "battle",
                                MatchUrl: $"{config.appconfig.getAddressHttpR()}/matches/v2/{match.MatchId}",
                                ModifyDate: DateTime.UtcNow.ToString("o"),
                                Notifications: new List<object>(),
                                PlayerIdLeft: leftUser.Id,
                                PlayerIdRight: rightUser.Id,
                                PlayerStatusLeft: "not_done",
                                PlayerStatusRight: "not_done",
                                //单人对战特供
                                //RightIsOnline:1,
                                RightIsOnline:1,
                                StartSide: "left",
                                Status: "pending",
                                WinnerId: 0,
                                WinnerSide: ""
                            ),
                            StartingData: new StartingData(
                                AllyFactionLeft: leftDeck.AllyFaction,
                                AllyFactionRight: rightDeck.AllyFaction,
                                CardBackLeft: leftDeck.CardBack,
                                CardBackRight: rightDeck.CardBack,
                                StartingHandLeft: match.LeftHand,
                                StartingHandRight: match.RightHand,
                                DeckLeft: match.LeftDeck,
                                DeckRight: match.RightDeck,
                                EquipmentLeft: leftUser.EquippedItem?.Select(i => i.ItemId).ToList() ?? new List<string>(),
                                EquipmentRight: rightUser.EquippedItem?.Select(i => i.ItemId).ToList() ?? new List<string>(),
                                IsAiMatch: false,
                                LeftPlayerName: leftUser.Name,
                                LeftPlayerOfficer: false,
                                LeftPlayerTag: leftUser.Tag.ToString(),
                                LocationCardLeft: leftLocation,
                                LocationCardRight: rightLocation,
                                PlayerIdLeft: leftUser.Id,
                                PlayerIdRight: rightUser.Id,
                                PlayerStarsLeft: 120,
                                PlayerStarsRight: 120,
                                RightPlayerName: rightUser.Name,
                                RightPlayerOfficer: false,
                                RightPlayerTag: rightUser.Tag.ToString()
                            )
                        )
                    );

                    return match.MatchStartingInfo;
                }
                // 新增辅助方法：从卡组生成卡牌（简化版本）
                (List<MatchCard> cards, MatchLocation location) GetCardsFromDeck(Deck deck, int startId, bool isLeft)
                {
                    Console.WriteLine(deck.DeckCode);
                    var cards = new List<MatchCard>();
                    var locationCard = new MatchLocation(
                        CardId: startId,
                        IsGold: false,
                        Location: isLeft ? "board_hqleft" : "board_hqright",
                        LocationNumber: 0,
                        Faction: deck.MainFaction,
                        Name: PlayerLibrary.DeckCodeTable[deck.DeckCode[^4..^2]].Card // 这里应该根据deck_code解析实际卡牌名
                    );
                    deck.DeckCode = deck.DeckCode.Remove(0, 5);
                    int cCount = 1;
                    foreach (var item in deck.DeckCode.Split(';'))
                    {
                        if (cCount >= 5)
                            break;
                        var a = (item.Chunk(2).Select(chunk => { return string.Concat(chunk).Length == 2 ? (PlayerLibrary.DeckCodeTable[string.Concat(chunk)]) : null ; }));
                        foreach (var lkp in a)
                        {
                            if (lkp == null)
                                continue;
                            Console.WriteLine(lkp.DeckCodeId);
                            cards.AddRange(Enumerable.Repeat(new MatchCard(
                                CardId: startId++,
                                IsGold: false,
                                Location: isLeft ? "deck_left" : "deck_right",
                                LocationNumber: 0,
                                Name: lkp.Card
                            ),cCount));

                        }
                        cCount++;
                    }
                    Console.WriteLine(cards.Count);
                    Console.WriteLine(JsonSerializer.Serialize(cards));
                    cards = cards.OrderBy(_ => Random.Shared.Next()).Select((a, l) => { return a with { LocationNumber = l + (isLeft ? 4 : 5) }; }).ToList();
                    return (cards, locationCard);
                }
                app.MapGet("/matches/v2/reconnect", () =>
                {
                    return Results.Ok("");
                });
                app.MapGet("/matches/v2/{id}", (int id) =>
                {
                    return Results.Text("running");
                });

                app.MapPut("/matches/v2/{id}/", async (int id, MatchAction matchAction, HttpContext context) =>
                {
                    var user = await GetUserFromAuthAsync(context);
                    if (user == null)
                        return Results.Unauthorized();
                    if (!config.appconfig.MatchedPairs.TryGetValue(id, out var match))
                        return Results.NotFound($"Match with ID {id} not found");
                    // 反作弊检查
                    if (config.appconfig.bancheat && matchAction.ActionType == GameConstants.XActionCheat)
                    {
                        // TODO: WebSocket发送封禁消息
                        user.Banned = true;

                        await GlobalState.users.SaveUserAsync(user);
                        match.WinnerSide = user.Id == match.Left?.PlayerId ? "right" : "left";
                        return Results.Ok(new { });
                    }
                    if (matchAction.Action == "lvl-loaded")
                        return Results.Ok(new { otherPlayerReady = 1 });
                    if (matchAction.Action == "end-match" && string.IsNullOrEmpty(match.WinnerSide))
                    {
                        match.WinnerSide = matchAction.Value?["winner_side"]?.ToString();
                    }

                    return Results.Text("OK");
                });

                app.MapPut("/matches/v2/{id}/actions", async (int id, HttpContext context, dynamic body) =>
                {
                    var user = await GetUserFromAuthAsync(context);
                    if (user == null)
                        return Results.Unauthorized();

                    if (!config.appconfig.MatchedPairs.TryGetValue(id, out var match))
                        return Results.NotFound($"Match with ID {id} not found");

                    var result = new Dictionary<string, object>();
                    var actions = match.GetActionsById(user.Id);
                    Console.WriteLine($"正在获取动作，玩家ID：{user.Id}，动作数量：{actions.Count}");
                    foreach (var a in actions) { 
                        Console.ForegroundColor=ConsoleColor.Blue ;
                        int actionId = 0;
                        string plantext = Decode(a, out actionId);
                        Console.WriteLine("\nActionId：" + actionId);
                        Console.WriteLine("解密结果：" + plantext);
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    if (actions.Count > 0)
                    {
                        result["actions"] = actions.ToArray();
                        actions.Clear();
                    }
                    result["match"] = new
                    {
                        player_status_left = match.PlayerStatusLeft,
                        player_status_right = match.PlayerStatusRight,
                        //  player_status_left             = "mulligan_done",

                        //单人对战
                       //player_status_right = "mulligan_done",
                        status = "running"
                    };

                    result["opponent_polling"] = true;

                    if (!string.IsNullOrEmpty(match.WinnerSide))
                    {
                        result["match"] = new
                        {
                            player_status_left = GameConstants.MulliganDone,
                            player_status_right = GameConstants.MulliganDone,
                            status = GameConstants.Finished
                        };
                        var opponentId = user.Id == match.Left?.PlayerId ? match.Right?.PlayerId : match.Left?.PlayerId;
                        var cheatAction = new MatchAction(
                            ActionType: GameConstants.XActionCheat,
                            PlayerId: opponentId,
                            ActionData: new Dictionary<string, object>
                            {
                                ["0"] = "DamageCard",
                                ["1"] = match.WinnerSide == "left" ? "41" : "1",
                                ["2"] = "99",
                                ["playerID"] = opponentId?.ToString()
                            },
                            ActionId: user.Id == match.Left?.PlayerId ? match.RightMinactionid + 1 : match.LeftMinactionid + 1,
                            LocalSubactions: 1
                        );

                        if (result["actions"] is object[] existingActions)
                        {
                            result["actions"] = new object[] { cheatAction }.Concat(existingActions).ToArray();
                        }
                        else
                        {
                            result["actions"] = new object[] { cheatAction };
                        }
                    }
                    //返回action
                    return Results.Ok(result);
                });
                app.MapGet("/config", (HttpContext context) =>
                {
                    return Results.Ok(new CloseConfig(XserverClosed: "路几把"));
                });
                app.MapPost("/matches/v2/{id}/actions", async (int id, MatchActionEn matchActionen, HttpContext context) =>
                {
                    var user = await GetUserFromAuthAsync(context);
                    if (user == null)
                        return Results.Unauthorized();
                    if (!config.appconfig.MatchedPairs.TryGetValue(id, out var match))
                        return Results.NotFound($"Match with ID {id} not found");
                    var matchAction = decryptMA((matchActionen));
                    Console.WriteLine(matchActionen.A);
                    if (matchAction.ActionType.Equals("XActionEndOfTurn")) {
                        Console.WriteLine("OK有个入结束了回合") ;
                    } 


                    // 反作弊检查
                    if (config.appconfig.bancheat && matchAction.ActionType == GameConstants.XActionCheat)
                    {
                        // TODO: WebSocket发送封禁消息
                        user.Banned = true;
                        await GlobalState.users.SaveUserAsync(user);
                        match.WinnerSide = user.Id == match.Left?.PlayerId ? "right" : "left";
                        return Results.Ok(new { });
                    }
                  
                    if (matchAction.Action == "lvl-loaded")
                        return Results.Ok(new { otherPlayerReady = 1 });
                    if (matchAction.Action == "end-match" && string.IsNullOrEmpty(match.WinnerSide))
                    {
                        match.WinnerSide = matchAction.Value?["winner_side"]?.ToString();
                    }

                    if (!string.IsNullOrEmpty(matchAction.ActionType) || !string.IsNullOrEmpty(matchAction.Action))
                    {
                        if (matchAction.ActionId > 0)
                        {
                            if (user.Id == match.Left?.PlayerId && matchAction.ActionId >= match.LeftMinactionid)
                                match.LeftMinactionid = matchAction.ActionId;
                            else if (matchAction.ActionId >= match.RightMinactionid)
                                match.RightMinactionid = matchAction.ActionId;
                        }

                        if (user.Id == match.Left?.PlayerId)
                        {
                            match.LeftActions.Add(matchActionen.A);
                            Console.WriteLine("玩家" + user.Id + "提交了一个动作，当前最小ActionId：" + match.LeftMinactionid);
                        }
                        else
                            match.RightActions.Add(matchActionen.A);
                    }

                    return Results.Text("OK");
                });
                // 7. 调度阶段
                app.MapPost("/matches/v2/{id}/mulligan", async (int id, MulliganCards mulliganCards, HttpContext context) =>
                {
                    var user = await GetUserFromAuthAsync(context);
                    if (user == null)
                        return Results.Unauthorized();

                    if (!config.appconfig.MatchedPairs.TryGetValue(id, out var match))
                        return Results.NotFound($"Match with ID {id} not found");
                    List<MatchCard> deck, hand;
                    if (user.Id == match.Left?.PlayerId)
                    {
                        deck = match.LeftDeck;
                        hand = match.LeftHand;
                        match.PlayerStatusLeft = GameConstants.MulliganDone;
                    }
                    else
                    {
                        deck = match.RightDeck;
                        hand = match.RightHand;
                        match.PlayerStatusRight = GameConstants.MulliganDone;
                    }

                    var result = new MulliganResult(
                        Deck: deck,
                        ReplacementCards: new List<MatchCard>()
                    );
                    Console.WriteLine("deck"+deck.Count);
                    foreach (var cardId in mulliganCards.DiscardedCardIds)
                    {
                        foreach (var card in hand)
                        {
                            if (card.CardId == cardId)
                            {
                                var randomIndex = Random.Shared.Next(result.Deck.Count);

                                //// 交换位置
                                // Use record 'with' to create new instances and preserve immutability.
                                var deckCard = result.Deck[randomIndex];

                                // The value added to ReplacementCards should reflect the deck card
                                // but with the hand card's location info (matching previous tuple-swap behavior).
                                var replacementCard = deckCard with
                                {
                                    Location = card.Location,
                                    LocationNumber = card.LocationNumber
                                };

                                // The deck slot will be replaced by the hand card but keep the original deck location info.
                                var newDeckEntry = card with
                                {
                                    Location = deckCard.Location,
                                    LocationNumber = deckCard.LocationNumber
                                };
                                result.ReplacementCards.Add(replacementCard);
                                result.Deck[randomIndex] = newDeckEntry;
                                break;
                            }
                        }
                    }

                    if (user.Id == match.Left?.PlayerId)
                        match.MulliganLeft = result;
                    else
                        match.MulliganRight = result;
                    return Results.Ok(result);
                });

                app.MapGet("/matches/v2/{id}/mulligan/{location}", (int id, string location) =>
                {
                    if (!config.appconfig.MatchedPairs.TryGetValue(id, out var match))
                        return Results.Text("null");
                    var mulligan = location == "left" ? match.MulliganLeft : match.MulliganRight;
                    //单人
                    //mulligan=new MulliganResult(
                    //    Deck: match.RightDeck,
                    //    ReplacementCards: new List<MatchCard>()
                    //);
                    //单人结束
                    return mulligan == null ? Results.Text("null") : Results.Ok(mulligan);
                });
                // 8. 比赛结束
                app.MapGet("/matches/v2/{id}/post", async (int id, HttpContext context) =>
                {
                    var user = await GetUserFromAuthAsync(context);
                    if (user == null)
                        return Results.Unauthorized();

                    if (!config.appconfig.MatchedPairs.TryGetValue(id, out var match))
                        return Results.NotFound($"Match with ID {id} not found");

                    if (user.Id == match.Left?.PlayerId)
                        match.PlayerStatusLeft = GameConstants.EndMatch;
                    else
                        match.PlayerStatusRight = GameConstants.EndMatch;

                    if (match.PlayerStatusLeft == GameConstants.EndMatch && match.PlayerStatusRight == GameConstants.EndMatch)
                    {
                        config.appconfig.MatchedPairs.TryRemove(id, out _);
                    }

                    var player = match.GetPlayerById(user.Id);
                    var isWinner = match.WinnerSide == (user.Id == match.Left?.PlayerId ? "left" : "right");

                    if (player != null && user.Decks.TryGetValue(player.DeckId, out var deck))
                    {
                        var response = new PostMatchResponse(
                            Faction: deck.MainFaction,
                            Winner: isWinner
                        );
                        Console.WriteLine(JsonSerializer.Serialize(response));
                        return Results.Ok(response);
                    }

                    return Results.NotFound();
                });
            }
            // 10. 管理API端点,暂时不需要
            void MakeAdminEndpoints()
            {
                app.MapGet("/admin/users/count", async () =>
            {
                var users = await GlobalState.users.GetAllUsersAsync();
                return Results.Ok(new { count = users.Count });
            });
                app.MapGet("/admin/users/list", async () =>
                {
                    var Users = await GlobalState.users.GetAllUsersAsync();
                    var simplifiedUsers = Users.Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        u.Name,
                        u.Tag,
                        DeckCount = u.Decks.Count,
                        u.Banned,
                        CreatedAt = u.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    }).ToList();

                    return Results.Ok(simplifiedUsers);
                });

                app.MapDelete("/admin/users/{userId}", async (int userId) =>
                {
                    var user = await GlobalState.users.GetByIdAsync(userId);
                    if (user == null)
                        return Results.NotFound($"User with ID {userId} not found");

                    await GlobalState.users.DeleteUserAsync(userId);
                    return Results.Ok(new { message = $"User {userId} deleted successfully" });
                });

                app.MapPost("/admin/users/{userId}/ban", async (int userId) =>
                {
                    var user = await GlobalState.users.GetByIdAsync(userId);
                    if (user == null)
                        return Results.NotFound($"User with ID {userId} not found");

                    user.Banned = true;

                    await GlobalState.users.SaveUserAsync(user);
                    return Results.Ok(new { message = $"User {userId} banned successfully" });
                });

                app.MapPost("/admin/users/{userId}/unban", async (int userId) =>
                {
                    var user = await GlobalState.users.GetByIdAsync(userId);
                    if (user == null)
                        return Results.NotFound($"User with ID {userId} not found");

                    user.Banned = false;

                    await GlobalState.users.SaveUserAsync(user);
                    return Results.Ok(new { message = $"User {userId} unbanned successfully" });
                });
            }
            UseMiddleWares();
            MakePlayerEndpoints();
            MakeUserEndpoints();
            MakeDecksEndpoints();
            MakeMatchEndpoints();
            // 11. 全局异常处理
            app.UseExceptionHandler(exceptionHandlerApp =>
            {
                exceptionHandlerApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new ErrorResponse(
                        Error: "Internal server error",
                        Message: "An unexpected error occurred",
                        StatusCode: 500
                    ));
                });
            });
            // 12. 未找到路由的处理
            app.UseStatusCodePages(async statusCodeContext =>
            {
                var response = statusCodeContext.HttpContext.Response;

                if (response.StatusCode == 404)
                {
                    await response.WriteAsJsonAsync(new ErrorResponse(
                        Error: "Not found",
                        Message: "The requested resource was not found",
                        StatusCode: 404
                    ));
                }
            });
            // 数据库初始化和清理
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                Console.WriteLine($"Application started on {config.appconfig.getAddressHttp()}");
                Console.WriteLine($"Faster 已准备");
            });
            app.Lifetime.ApplicationStopping.Register(() =>
            {
                Console.WriteLine("Application stopping. Cleaning up...");
                // 清理数据库资源
                GlobalState.users.Dispose();
            });
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("正在启动http服务器");
            Console.WriteLine("等待两秒确保初始化成功");
            if (File.Exists("./YCDR"))
                Console.WriteLine("发现持久化数据，已加载");
            Console.ForegroundColor = ConsoleColor.White;
            await app.RunAsync();
        }
    }
}
