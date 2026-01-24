namespace fyserver
{
        record msg
        {
            public string match_id="";
            public string message="";
            public string channel = "";
            public string context = "";
            public long timestamp= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            public int sender;
            public string receiver = "";
        }
}
