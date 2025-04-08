using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankNameGenerator
{
    TankNames names;

    public string GenerateRandomName(TankNames nameType = null)
    {
        names = nameType;
        if (names == null) names = Resources.Load<TankNames>("TankNames/TestNames");

        string name = "The ";

        string adjective = GetRandomAdjective();

        name += adjective;

        name += " ";

        string noun = GetRandomNoun();

        name += noun;

        return name;
    }

    public string GetRandomAdjective()
    {
        int random = Random.Range(0, names.adjectives.Length);
        string adjective = names.adjectives[random];

        return adjective;
    }

    public string GetRandomNoun()
    {
        int random = Random.Range(0, names.nouns.Length);
        string noun = names.nouns[random];

        return noun;
    }
}
