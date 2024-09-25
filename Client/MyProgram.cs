﻿using ClubActivity;

namespace Client;

internal class MyProgram
{
    public Choice Choose(Round[] rounds)
    {
        if(rounds.Length == 0)
        {
            return Choice.Cooperate;
        }

        Round lastRound = rounds.Last();

        return lastRound.OpponentChoice;
    }
}