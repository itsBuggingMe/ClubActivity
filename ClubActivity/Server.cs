using ClubActivity;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

//not actually using tcp
//using pantry so i can do this anywhere
public class Pantry : IConnection
{
    public const string PantryId = "076eafa5-01c6-4edd-b084-0714d3f2815f";
    public const string PantryUrl = "https://getpantry.cloud/apiv1/pantry/076eafa5-01c6-4edd-b084-0714d3f2815f";
    private string Basket => $"{PantryUrl}/basket/{_basketName}";
    private string _basketName;
    public Pantry(string s) => _basketName = s;

    private HttpClient _httpClient = new();
    private ConcurrentQueue<string> _strings = new();
    private Dictionary<string, string> existingKeys = new Dictionary<string, string>();
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
        if (s is { } nn && !nn.baskets.Any(n => n.name == _basketName))
        {
            await _httpClient.PostAsync(Basket, null);
        }
        for (; ; )
        {
            try
            {
                string content = await _httpClient.GetStringAsync(Basket);
                Dictionary<string, string>? dict = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                if (dict is null)
                {
                    return;
                }

                foreach (var kvp in dict)
                {
                    if (existingKeys.ContainsKey(kvp.Key))
                    {
                        continue;
                    }

                    _strings.Enqueue(kvp.Value);
                    existingKeys.Add(kvp.Key, kvp.Value);
                }
            }
            catch
            {

            }

            Thread.Sleep(4000);
        }
    }

    private void RunClient()
    {
        for (; ; )
        {
            if (_strings.TryDequeue(out var sendME))
            {
                string base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(sendME));
                string jsonString = $"{{ \"{Random.Shared.Next()}\" : \"{base64}\" }}";

                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                _httpClient.PutAsync(Basket, content);
            }

            Thread.Sleep(500);
        }
    }
}

public class PantryObject
{
    public string name { get; set; }
    public string description { get; set; }
    public string[] errors { get; set; }
    public bool notifications { get; set; }
    public int percentFull { get; set; }
    public Basket[] baskets { get; set; }
}

public class Basket
{
    public string name { get; set; }
    public int ttl { get; set; }
}