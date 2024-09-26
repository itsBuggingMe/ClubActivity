using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Extensions.Logging;
using ClubActivity;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System.Reflection;

class Program
{
    static string thisFolder = Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).FullName;
    private static RoslynCompiler _compiler;
    private static IConnection _connection;
    private static Stack<Entry> _participants = new();
    private static bool _break;

    public static void Main()
    {
        string[] AllowedNamespaces = new string[]
        {
            "System.Linq",
            "System",
            "System.Collections",
            "netstandard",
        };
        string[] AllowedAssemblies = new string[]
        {
            System.Reflection.Assembly.GetEntryAssembly().Location,
        };


        _compiler = new RoslynCompiler(AllowedNamespaces, AllowedAssemblies);

        _connection = new Pantry("cs_club_p");
        _connection.Host();
        Task.Run(() => { Console.ReadLine(); _break = true; });
        while(!_break)
        {
            UpdatePartcipants();
            Thread.Sleep(100);
        }

        foreach(var item in _participants)
        {
            Choice c = item.Function.Choose(new Round[] { });
        }
    }


    static void UpdatePartcipants()
    {
        string? s = _connection.TryGetData();
        if (s is null)
            return;

        string final = Encoding.ASCII.GetString(Convert.FromBase64String(s));
        NetData? data = JsonSerializer.Deserialize<NetData>(final);
        if (data is null)
            return;

        if (data.Extension == ".cs")
        {
            try
            {
                var asm = _compiler.Compile(data.Code);
                Type entry = asm.GetTypes().First(t => t.GetMethod("Choose") is not null);
                _participants.Push(new Entry(data.Name, data.Extension, new FuncWrapper(RuntimeHelpers.GetUninitializedObject(entry))));
                Console.WriteLine($"{data.Name} with language C#");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        if(data.Extension == ".py")
        {
            _participants.Push(new Entry(data.Name, data.Extension, new PythonWrapper(data.Code)));
            Console.WriteLine($"{data.Name} with language Python");
        }
    }

    internal class FuncWrapper
    {
        private object o;
        private MethodInfo _method;
        public FuncWrapper(object o)
        {
            _method = o.GetType().GetMethod("Choose");
            this.o = o;
        }

        public Choice Choose(Round[] previousRounds)
        {
            return (Choice)_method.Invoke(o, new object[] { previousRounds });
        }
    }

    internal class PythonWrapper
    {
        private static readonly ScriptEngine _engine = Python.CreateEngine();
        private string _script;
        private dynamic func;
        public PythonWrapper(string code)
        {
            _script = code;
            var scope = _engine.CreateScope();
            _engine.Execute(_script, scope);

            func = scope.GetVariable("Choose");
        }

        public Choice Choose(Round[] previousRounds)
        {

            var pythonRounds = previousRounds
                .Select(r => new { self = r.YourChoice.ToString(), other = r.OpponentChoice.ToString() })
                .ToList();

            var result = func(pythonRounds);

            if (result.ToString().Equals("Cooperate", StringComparison.OrdinalIgnoreCase))
            {
                return Choice.Cooperate;
            }
            else if (result.ToString().Equals("Cheat", StringComparison.OrdinalIgnoreCase))
            {
                return Choice.Cheat;
            }
            return Choice.Cheat;
        }
    }

}
internal record class Entry(string Name, string Extension, dynamic Function);