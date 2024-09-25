using ClubActivity;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

internal class Pantry : IConnection
{
    public const string PantryId = "076eafa5-01c6-4edd-b084-0714d3f2815f";
    public const string PantryUrl = "https://getpantry.cloud/apiv1/pantry/076eafa5-01c6-4edd-b084-0714d3f2815f";

    private string _basketName;
    public Pantry(string s) => _basketName = s;

    private HttpClient _httpClient = new();
    private ConcurrentQueue<string> _strings = new();
    public bool IsConnected => true;
    public void Connect() => Task.Run(RunClient);
    public void Host() => Task.Run(RunServer);

    public void SendData(string data)
    {
        _strings.Enqueue(data);
    }

    public string? TryGetData()
    {
        _strings.TryDequeue(out string? a);
        return a;
    }

    private async void RunServer()
    {
        PantryObject? s = await _httpClient.GetFromJsonAsync<PantryObject>(PantryUrl);
        for(;;)
        {
            

            Thread.Sleep(500);
        }
    }

    private void RunClient()
    {
        for (;;)
        {


            Thread.Sleep(500);
        }
    }
}

public class PantryObject
{
    public string name { get; set; }
    public string description { get; set; }
    public object[] errors { get; set; }
    public bool notifications { get; set; }
    public int percentFull { get; set; }
    public object[] baskets { get; set; }
}