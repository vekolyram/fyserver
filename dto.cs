namespace fyserver
{
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
    public record  User
    {
        //async store()
        //{
        //    await users.put(this.user_name, JSON.stringify(this));
        //    await users.put("" + this.id, JSON.stringify(this));
        //}
        public int id = config.appconfig.rand.Next(1000000);
        public string user_name = "XDLG";
        public string name = "<anon>";
        public string locale = "zh-Hans";
        public int tag = config.appconfig.rand.Next(900000);
        //decks: { [key: number]: Deck
        //};
        //equipped_item: Item[];
        //items;
        public bool banned = false;
    }

}
