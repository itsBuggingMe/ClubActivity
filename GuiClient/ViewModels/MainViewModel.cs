using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using ClubActivity;
using System.Text.Json;
using Avalonia;
using System.Threading;

namespace GuiClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly string FilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!;
    private IConnection _connection;

    public MainViewModel()
    {
        Languages = new ObservableCollection<string> { "C#", "Python" };
        SelectedLanguage = Languages[0];
        var files = Directory.EnumerateFiles(FilePath);
        _code = File.ReadAllText(files.First(f => f.EndsWith(".cs")));
        _connection = new Pantry("cs_club_p1");
        _connection.Connect();
    }

    [ObservableProperty]
    private ObservableCollection<string> _languages;

    [ObservableProperty]
    private string _selectedLanguage;

    [ObservableProperty]
    private string _code;


    [ObservableProperty]
    private string? _name;

    [RelayCommand]
    public void Finish()
    {
        if(string.IsNullOrWhiteSpace(Name) || Name == "Enter a name!")
        {
            Name = "Enter a name!";
            return;
        }

        string s = JsonSerializer.Serialize(new NetData(Code, Ext(SelectedLanguage), Name));
        _connection.SendData(s);
        Thread.Sleep(2000);
        Environment.Exit(0);
    }

    partial void OnSelectedLanguageChanged(string? oldValue, string newValue)
    {
        string ext = Ext(newValue);

        var files = Directory.EnumerateFiles(FilePath);
        Code = File.ReadAllText(files.First(f => f.EndsWith(ext)));
    }

    private static string Ext(string name) => name switch
    {
        "C#" => ".cs",
        "Python" => ".py",
        _ => throw new NotImplementedException()
    };
}
