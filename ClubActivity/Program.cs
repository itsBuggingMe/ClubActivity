namespace ClubActivity;

class Program
{
    public static void Main()
    {
        Server.Instance.Run();
        while(true)
        {
            if(Server.Instance.Messages.TryDequeue(out string? s))
                Console.WriteLine(s);
            Thread.Sleep(100);
        }
    }
}