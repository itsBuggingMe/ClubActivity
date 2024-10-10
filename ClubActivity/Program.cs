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

        _connection = new Pantry("cs_club_pm_2");
        _connection.Host();
        Task.Run(() => { Thread.Sleep(1000); Console.ReadLine(); _break = true; });
        while(!_break)
        {
            UpdatePartcipants();
            Thread.Sleep(100);
        }

        RunGame();
    }

    static void RunGame()
    {
        List<Player> entries = _participants.Select(t => new Player(t)).ToList();
        Player[] array = entries.ToArray();
        Round[][] arrPoolLeft = Enumerable.Range(0, 25).Select(i => new Round[i]).ToArray();
        Round[][] arrPoolRight = Enumerable.Range(0, 25).Select(i => new Round[i]).ToArray();

        int round = 1;
        while(true)
        {
            round++;

            for(int i = 0; i < entries.Count; i++)
            {
                for(int j = i + 1; j < entries.Count; j++)
                {
                    var left = entries[i];
                    var right = entries[j];
                    Round[] arrL = arrPoolLeft[0];
                    Round[] arrR = arrPoolRight[0];

                    for(int r = 0; r < 10 || (Random.Shared.Next(10) < 5); r++)
                    {
                        Choice leftChoice;
                        try
                        { leftChoice = (Choice)left.Entry.Function.Choose(arrL); }
                        catch
                        {
                            left.Score = 0;
                            leftChoice = Choice.Cheat;
                        }
                        Choice rightChoice;
                        try
                        { rightChoice = (Choice)right.Entry.Function.Choose(arrR); }
                        catch
                        {
                            right.Score = 0;
                            rightChoice = Choice.Cheat;
                        }
                        //Console.WriteLine($"{r}: {left.Entry.Name} vs {right.Entry.Name}");

                        Resize(ref arrL, new Round(leftChoice, rightChoice), arrPoolLeft);
                        Resize(ref arrR, new Round(rightChoice, leftChoice), arrPoolRight);

                        var (x, y) = (leftChoice, rightChoice) switch
                        {
                            (Choice.Cooperate, Choice.Cheat) => (0, 5),
                            (Choice.Cheat, Choice.Cooperate) => (5, 0),
                            (Choice.Cheat, Choice.Cheat) => (1, 1),
                            (Choice.Cooperate, Choice.Cooperate) => (3, 3),
                            _ => (0, 0)
                        };
                        left.Score += x;
                        right.Score += y;

                        if (r >= 20)
                            break;

                        static void Resize(ref Round[] toResize, Round newElement, Round[][] pool)
                        {
                            var newArr = pool[toResize.Length + 1];

                            for(int i = 0; i < toResize.Length; i++)
                            {
                                newArr[i] = toResize[i];
                            }

                            toResize = newArr;
                            toResize[^1] = newElement;
                        }
                    }
                }
            }

            Array.Sort(array, (a, b) => b.Score.CompareTo(a.Score));

            double aggregate = 0;
            Dictionary<string, int> scores = new Dictionary<string, int>();
            foreach (var player in array)
            {
                aggregate += player.Score;
                if(scores.ContainsKey(player.Entry.Name))
                {
                    scores[player.Entry.Name]++;
                }
                else
                {
                    scores[player.Entry.Name] = 0;
                }
            }

            int counter = 1;
            Console.Clear();
            foreach(var score in scores.OrderByDescending(kvp => kvp.Value))
            {
                Console.WriteLine($"#{counter++}. {score.Key}: {score.Value}");
            }
            Thread.Sleep(1000);
            const double Total = 100;
            List<Player> newPlayers = new List<Player>();
            foreach (var player in array)
            {
                int estimatedCount = (int)Math.Round(player.Score / aggregate * Total);
                for(int i = 0; i < estimatedCount; i++)
                {
                    newPlayers.Add(new Player(player.Entry));
                }
            }
            array = newPlayers.ToArray();
            entries = newPlayers;
        }
    }

    internal class Player
    {
        public readonly Entry Entry;
        public int Score;

        public Player(Entry entry)
        {
            Entry = entry;
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
            var item = LastThree(previousRounds);
            return (Choice)_method.Invoke(o, new object[] { item.Item1, item.Item2, item.Item3, previousRounds.Length });
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
            var r = LastThree(previousRounds);

            var result = func(r.Item1.ToString(), r.Item2.ToString(), r.Item3.ToString(), previousRounds.Length);


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


    public static (Round, Round, Round) LastThree(Round[] rounds)
    {
        int count = rounds.Length;
        Round thirdLast = count >= 3 ? rounds[count - 3] : default;
        Round secondLast = count >= 2 ? rounds[count - 2] : default;
        Round last = count >= 1 ? rounds[count - 1] : default;
        return (thirdLast, secondLast, last);
    }
}
internal record class Entry(string Name, string Extension, dynamic Function);
