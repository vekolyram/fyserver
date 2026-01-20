using System.Text.Json.Serialization;
using Newtonsoft.Json;
namespace fyserver
{
    public class config
    {
        public int portWs=5231;
        public int portHttp = 5232;
        public bool banchaeat = false;
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
