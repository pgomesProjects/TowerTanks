
[System.Serializable]
public class SessionStats
{
    //Tank
    public const string tankHeader = "Tank Statistics";
    public float maxHeight;
    public int roomsBuilt;
    public int totalCells;

    //Resources
    public const string resourcesHeader = "Resources";
    public int cargoSold;

    //Interactables
    public const string interactablesHeader = "Interactables";
    public int cannonsBuilt;
    public int machineGunsBuilt;
    public int mortarsBuilt;
    public int boilersBuilt;
    public int throttlesBuilt;

    //Enemies
    public const string enemiesHeader = "Enemies";
    public int enemiesKilled;
}
