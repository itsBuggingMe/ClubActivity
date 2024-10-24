using ClubActivity;
using System.Linq;

namespace Client;

internal class MyProgram
{
    public Choice Choose(Round third, Round second, Round last, int totalRoundCount)
    {
        return last.OpponentChoice;
    }
}
