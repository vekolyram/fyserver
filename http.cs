using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json;

namespace fyserver
{
    public class http
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        async public Task StartHttpServer()
        {
            a.InitLibrary("./deckCodeIDsTable2.json");
            builder.Services.AddOpenApi();
            builder.WebHost.UseUrls(config.appconfig.getAddressHttp());
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            });
            // 配置JSON序列化
            var app = builder.Build();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            app.Use(async (context, next) =>
            {
                if (context.Response.ContentType?.Contains("charset") == true)
                {
                    context.Response.ContentType = context.Response.ContentType.Replace("; charset=utf-8", "");
                }
                Console.WriteLine($"{context.Request.Method} {context.Request.Path} from {context.Connection.RemoteIpAddress}");
                await next();
            });
            // 辅助方法
            int GetPlayerIdFromAuth(HttpContext context)
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != null && authHeader.StartsWith("JWT "))
                {
                    return GlobalState.users.GetByUserNameAsync(authHeader["JWT ".Length..]).Id;
                }
                return 0;
            }
            // 1. 会话管理
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
                    Jwt: $"{session.Username}",
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
                    ServerOptions: JsonConvert.SerializeObject(new ServerOptions() { Websocketurl = config.appconfig.getAddressWs() }),
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
            async Task<User?> GetUserFromAuthAsync(HttpContext context)
            {
                var playerId = GetPlayerIdFromAuth(context);
                if (playerId > 0)
                {
                    // 纯粹的数据获取，不需要注入 UserStore
                    return await GlobalState.users.GetByIdAsync(playerId);
                }
                return null;
            }
            // 2. 配置和基本信息
            app.MapGet("/", async (HttpContext context) =>
            {
                var auth = (context.Request.Headers["Authorization"].FirstOrDefault());
                Console.WriteLine(auth);
                if (auth == null)
                {
                    auth = "JWT 1939Mother";
                }
                string authToken = auth["JWT ".Length..];
                User? user = await GlobalState.users.GetByUserNameAsync(authToken);
                if (user == null)
                    try
                    {
                        user = await GlobalState.users.CreateUserAsync(authToken);
                        Console.WriteLine($"Created new user: {authToken}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.InnerException.Message);
                        // 用户已存在或其他错误
                        return Results.BadRequest(ex.Message);
                    }
                return Results.Ok(await config.appconfig.getConfigAsync(auth));
            });// 3. 玩家管理
            //TODO: 完善entitlements接口,用于处理所有权
            //TODO: 完善fp接口,用于处理商店
            app.MapGet("/fp/", (HttpContext context) =>
            {
            });
            //TODO：http://kards.live.1939api.com//store/v2/?provider=xsolla HTTP/1.1，用于处理商店
            //
            //
            app.Use(async (context, next) =>
            {
                // 在处理请求之前或之后添加
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["Content-Type"] = context.Response.Headers["Content-Type"].ToString().Replace("; charset=utf-8", "");
                    return Task.CompletedTask;
                });
                var originalPath = context.Request.Path;
                var normalizedPath = originalPath.Value.Replace("//", "/");
                if (string.IsNullOrEmpty(normalizedPath))
                {
                    normalizedPath = "/";
                }
                Console.WriteLine(normalizedPath);
                if (originalPath.Value != normalizedPath)
                {
                    context.Request.Path = normalizedPath;
                }
                await next();
            });
            // 中间件：规范化路径（移除多余斜杠）
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
                return Results.Ok(a.Library);
            });

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

            app.MapPut("/players/{player_id}/decks/{deck_id}", async (string player_id, int deck_id, [FromBody]DeckAction action) =>
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

            // 5. 物品装备
            app.MapGet("/items/{id}", async (string id) =>
            {
                var user = await GlobalState.users.GetByIdAsync(int.Parse(id));
                if (user == null)
                    return Results.NotFound($"User with ID {id} not found");



                if (user.Items == null || user.Items.Count == 0)
                {
                    user.Items = a.Items.ToList();
                    await GlobalState.users.SaveUserAsync(user);
                }

                var response = new ItemsResponse(

                    EquippedItems: user.EquippedItem,
                    Items: user.Items
                );

                return Results.Ok(response);
            });

            app.MapPost("/items/{id}", async (string id, Item item) =>
            {
                var user = await GlobalState.users.GetByIdAsync(int.Parse(id));
                if (user == null)
                    return Results.NotFound($"User with ID {id} not found");



                if (user.EquippedItem == null)
                    user.EquippedItem = new List<Item>();

                // 移除相同槽位的装备
                user.EquippedItem.RemoveAll(i => i.Slot == item.Slot);
                // 添加新装备
                user.EquippedItem.Add(item);

                await GlobalState.users.SaveUserAsync(user);

                return Results.Ok(item);
            });
            // 6. 匹配系统
            app.MapPost("/lobbyplayers", async (LobbyPlayer lobbyPlayer, HttpContext context) =>
             {
                 var user = await GlobalState.users.GetByIdAsync(lobbyPlayer.PlayerId);
                 if (user == null || user.Name == "<anon>")
                 {
                     // TODO: WebSocket断开连接消息
                     //context.Connection.Close();
                     return Results.BadRequest("请改名");
                 }



                 // 检查卡组有效性（简化）
                 if (!user.Decks.TryGetValue(lobbyPlayer.DeckId, out var deck))
                 {
                     // context.Connection.Close();
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
                     config.appconfig.WaitingPlayers1.Add(lobbyPlayer);
                     if (config.appconfig.WaitingPlayers1.Count >= 2)
                     {
                         var matchId = Random.Shared.Next(100000, 999999);
                         var matchInfo = new MatchInfo(matchId, config.appconfig.WaitingPlayers1[0], config.appconfig.WaitingPlayers1[1]);
                         config.appconfig.WaitingPlayers1.RemoveAt(0);
                         config.appconfig.WaitingPlayers1.RemoveAt(0);
                         config.appconfig.MatchedPairs[matchId] = matchInfo;
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
                     }
                 }

                 return Results.Ok("OK");
             });

            //app.MapDelete("/lobbyplayers", (LobbyPlayer lobbyPlayer) =>
            //{
            //    config.appconfig.WaitingPlayers1.RemoveAll(p => p.PlayerId == lobbyPlayer.PlayerId);
            //    config.appconfig.WaitingPlayers2.RemoveAll(p => p.PlayerId == lobbyPlayer.PlayerId);

            //    foreach (var code in config.appconfig.BattleCodePlayers.Keys)
            //    {
            //        config.appconfig.BattleCodePlayers[code].RemoveAll(p => p.PlayerId == lobbyPlayer.PlayerId);
            //    }

            //    return Results.Ok(new { status = 200 });
            //});

            app.MapGet("/matches/v2", async (HttpContext context) =>
            {
                var user = await GetUserFromAuthAsync(context);
                if (user == null)
                    return Results.Unauthorized();

                MatchInfo? match = null;
                foreach (var kvp in config.appconfig.MatchedPairs)
                {
                    if (string.IsNullOrEmpty(kvp.Value.WinnerSide) && kvp.Value.HasPlayer(user.Id))
                    {
                        match = kvp.Value;
                        break;
                    }
                }

                if (match == null)
                    return Results.Ok("null");

                // TODO: 实现makeMatchStartingInfo逻辑
                return Results.Ok(match.MatchStartingInfo ?? "null");
            });

            app.MapGet("/matches/v2/{id}", (int id) =>
            {
                return Results.Ok("running");
            });

            app.MapPut("/matches/v2/{id}", async (int id, MatchAction matchAction, HttpContext context) =>
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
                        match.RightActions.Add(matchAction);
                    else
                        match.LeftActions.Add(matchAction);
                }

                return Results.Ok("OK");
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

                if (actions.Count > 0)
                {
                    result["actions"] = actions.ToArray();
                    actions.Clear();
                }

                result["match"] = new
                {
                    player_status_left = match.PlayerStatusLeft,
                    player_status_right = match.PlayerStatusRight,
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
                        PlayerId: opponentId?.ToString(),
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

                return Results.Ok(result);
            });
            app.MapGet("/config", (HttpContext context) =>
            {
                return Results.Ok(new CloseConfig(XserverClosed: "路几把"));
            });// 3. 玩家管理
            app.MapPost("/matches/v2/{id}/actions", async (int id, MatchAction matchAction, HttpContext context) =>
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
                        match.RightActions.Add(matchAction);
                    else
                        match.LeftActions.Add(matchAction);
                }

                return Results.Ok("OK");
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

                foreach (var cardId in mulliganCards.DiscardedCardIds)
                {
                    foreach (var card in hand)
                    {
                        if (card.CardId == cardId)
                        {
                            var randomIndex = Random.Shared.Next(result.Deck.Count);

                            //// 交换位置
                            //(result.Deck[randomIndex].Location, card.Location) =
                            //    (card.Location, result.Deck[randomIndex].Location);

                            //(result.Deck[randomIndex].LocationNumber, card.LocationNumber) =
                            //    (card.LocationNumber, result.Deck[randomIndex].LocationNumber);

                            result.ReplacementCards.Add(result.Deck[randomIndex]);
                            result.Deck[randomIndex] = card;
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
                    return Results.Ok("null");

                var mulligan = location == "left" ? match.MulliganLeft : match.MulliganRight;
                return mulligan == null ? Results.Ok("null") : Results.Ok(mulligan);
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

                    return Results.Ok(response);
                }

                return Results.NotFound();
            });

            // 10. 管理API端点
            app.MapGet("/admin/GlobalState.users/count", async () =>
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

            app.MapDelete("/admin/GlobalState.users/{userId}", async (int userId) =>
            {
                var user = await GlobalState.users.GetByIdAsync(userId);
                if (user == null)
                    return Results.NotFound($"User with ID {userId} not found");

                await GlobalState.users.DeleteUserAsync(userId);
                return Results.Ok(new { message = $"User {userId} deleted successfully" });
            });

            app.MapPost("/admin/GlobalState.users/{userId}/ban", async (int userId) =>
            {
                var user = await GlobalState.users.GetByIdAsync(userId);
                if (user == null)
                    return Results.NotFound($"User with ID {userId} not found");

                user.Banned = true;

                await GlobalState.users.SaveUserAsync(user);
                return Results.Ok(new { message = $"User {userId} banned successfully" });
            });

            app.MapPost("/admin/GlobalState.users/{userId}/unban", async (int userId) =>
            {
                var user = await GlobalState.users.GetByIdAsync(userId);
                if (user == null)
                    return Results.NotFound($"User with ID {userId} not found");

                user.Banned = false;

                await GlobalState.users.SaveUserAsync(user);
                return Results.Ok(new { message = $"User {userId} unbanned successfully" });
            });

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
                Console.WriteLine($"RocksDB database initialized at ./db/GlobalState.users.db");
            });

            app.Lifetime.ApplicationStopping.Register(() =>
            {
                Console.WriteLine("Application stopping. Cleaning up...");
                // 清理数据库资源
                GlobalState.users.Dispose();
            });

            //Console.WriteLine(app.Map);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("正在启动http服务器");
            Console.ForegroundColor = ConsoleColor.White;
            await app.RunAsync();

        }
    }
}
