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
        "Fiery",
        "Super",
        "Supreme",
        "Strapping",
        "Bellowing",
        "Fierce",
        "Perfect",
        "Power",
        "Destructive",
        "Scrappy",
        "Patchwork",
        "Octane",
        "Ardent",
        "Mammoth",
        "Greasy",
        "Queen's"
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
        "Flanker",
        "Beast",
        "Rigger",
        "Dodger",
        "Boi",
        "Tankard",
        "Vessel",
        "Revenge",
        "Corsair",
        "Jalopy",
        "Heap",
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
