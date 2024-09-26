def Choose(previousRounds):
    if len(previousRounds) == 0:
        return "Cooperate"
    if previousRounds[-1] == "Cheat":
        return "Cooperate"
    else
        return "Cheat"