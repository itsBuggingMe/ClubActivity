namespace ClubActivity;

public enum Choice
{
    Cooperate,
    Cheat,
}

public record struct Round(Choice YourChoice, Choice OpponentChoice);