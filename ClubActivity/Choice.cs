namespace ClubActivity;

public enum Choice
{
    Cheat,
    Cooperate
}

public record struct Round(Choice YourChoice, Choice OpponentChoice);