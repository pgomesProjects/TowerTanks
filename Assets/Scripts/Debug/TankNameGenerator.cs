using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankNameGenerator
{
    public string[] adjectives = {
        "Jolly",
        "Massive",
        "Gorgon",
        "Dangerous",
        "Firey",
        "Super",
        "Supreme",
        "Strapping",
        "Bellowing",
        "Fierce"
    };

    public string[] nouns = {
        "Roger",
        "Machine",
        "Guzzler",
        "Demon",
        "Frontier",
        "Striker",
        "Shooter",
        "Fellow",
        "Bulger",
        "Flanker"
    };

    public string GenerateRandomName()
    {
        string name = "The ";

        int random = Random.Range(0, adjectives.Length);

        name += adjectives[random];

        name += " ";

        random = Random.Range(0, nouns.Length);

        name += nouns[random];

        return name;
    }
}
