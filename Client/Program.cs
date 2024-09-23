using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location!)!;
string cfgPath = Path.Combine(dir!, "config.txt");
Config? config = null;
Console.WriteLine("Press Enter to continue");
string? res = Console.ReadLine();
string meta;
if(File.Exists(cfgPath) && res != "r")
{
    config = JsonSerializer.Deserialize<Config>(meta = File.ReadAllText(cfgPath));
}
else
{
    config = new Config(Ask("What is your name?", s => !string.IsNullOrWhiteSpace(s)), 
        Ask("IP:", s => IPAddress.TryParse(s, out _)), 
        Ask("What Lang? C# (cs) or Python (p)", s => s == "cs" || s == "p") == "p" ? Lang.Python : Lang.CSharp);
    File.WriteAllText(cfgPath, meta = JsonSerializer.Serialize(config));
}
var client = new TcpClient(config!.IP, 8996);
NetworkStream output = client.GetStream();
string file = File.ReadAllText(Directory.EnumerateFiles(dir).Where(s => s.EndsWith(LangToString(config.Lang))).First());
output.Write(Encoding.ASCII.GetBytes($"{meta}|{file}"));
output.Close();

Console.Clear();
Console.WriteLine("Done");
Thread.Sleep(1000);
static string Ask(string q, Predicate<string>? validator)
{
    validator ??= s => true;
    while(true)
    {
        Console.WriteLine(q);
        string? attempt = Console.ReadLine();
        if(attempt is not null && validator(attempt))
        {
            return attempt;
        }
        Console.WriteLine("Invalid");
    }
}

static string LangToString(Lang l) => l switch
{
    Lang.CSharp => ".cs",
    Lang.Python => ".py",
    _ => throw new Exception()
};

internal record class Config(string Name, string IP, Lang Lang);

internal enum Lang
{
    CSharp,
    Python,
}

