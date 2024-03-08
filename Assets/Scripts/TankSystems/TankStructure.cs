using System.Collections.Generic;

[System.Serializable]
public class TankStructure
{
    private Dictionary<int, List<int>> adjacencyList;

    public TankStructure()
    {
        adjacencyList = new Dictionary<int, List<int>>();
    }

    /// <summary>
    /// Adds a room to the total list of rooms.
    /// </summary>
    /// <param name="roomID">The ID for the new room (must be unique).</param>
    public void AddRoom(int roomID)
    {
        if (!adjacencyList.ContainsKey(roomID))
        {
            adjacencyList[roomID] = new List<int>();
        }
    }

    /// <summary>
    /// Adds an adjacency between two nodes.
    /// </summary>
    /// <param name="firstRoomID">First room ID.</param>
    /// <param name="secondRoomID">Second room ID.</param>
    public void AddAdjacency(int firstRoomID, int secondRoomID)
    {
        //If the room does not exist already, add the rooms
        if (!adjacencyList.ContainsKey(firstRoomID))
            AddRoom(firstRoomID);

        if (!adjacencyList.ContainsKey(secondRoomID))
            AddRoom(secondRoomID);

        //Add each room to their respective dictionary
        adjacencyList[firstRoomID].Add(secondRoomID);
        adjacencyList[secondRoomID].Add(firstRoomID);
    }

    /// <summary>
    /// Gets the path between two rooms using a breadth-first search.
    /// </summary>
    /// <param name="startingRoom">The starting room.</param>
    /// <param name="targetRoom">The target room.</param>
    /// <returns>The path between the starting room and the target room.</returns>
    public List<int> GetPathBetweenRooms(int startingRoom, int targetRoom)
    {
        Dictionary<int, int> distance = new Dictionary<int, int>();
        Dictionary<int, int> previous = new Dictionary<int, int>();
        Queue<int> queue = new Queue<int>();

        foreach (var room in adjacencyList.Keys)
        {
            distance[room] = int.MaxValue;
            previous[room] = -1;
        }

        //Enqueue the starting room
        distance[startingRoom] = 0;
        queue.Enqueue(startingRoom);

        //Keep the queue going while it's not empty
        while (queue.Count > 0)
        {
            //Dequeue the current room
            int currentRoom = queue.Dequeue();

            //For each adjacent room, enqueue them
            foreach (int neighbor in adjacencyList[currentRoom])
            {
                if (distance[neighbor] == int.MaxValue)
                {
                    distance[neighbor] = distance[currentRoom] + 1;
                    previous[neighbor] = currentRoom;
                    queue.Enqueue(neighbor);

                    //If the target room is reached, construct the path
                    if (neighbor == targetRoom)
                        return ReconstructPath(previous, targetRoom);
                }
            }
        }

        //No path found
        return null;
    }

    /// <summary>
    /// Creates the path between rooms.
    /// </summary>
    /// <param name="previous">The shortest path stored from the search.</param>
    /// <param name="targetRoom">The target room.</param>
    /// <returns>Returns the path between the starting room and the target room, with the starting room being first and the target room being last.</returns>
    private List<int> ReconstructPath(Dictionary<int, int> previous, int targetRoom)
    {
        List<int> path = new List<int>();
        int currentRoom = targetRoom;

        //Reverse the path, since the BFS will have the path backwards
        while (currentRoom != -1)
        {
            path.Insert(0, currentRoom);
            currentRoom = previous[currentRoom];
        }

        PrintPath(path);
        return path;
    }

    /// <summary>
    /// Displays the path between two rooms.
    /// </summary>
    /// <param name="path">The list of rooms for the path.</param>
    private void PrintPath(List<int> path)
    {
        string pathString = "Path: ";
        foreach (var room in path)
        {
            pathString += room + " -> ";
        }
        pathString = pathString.Remove(pathString.Length - 4);
        Debug.Log(pathString);
    }

    /// <summary>
    /// Prints the tank structure in its entirety.
    /// </summary>
    public void PrintTankStructure()
    {
        foreach (var node in adjacencyList)
        {
            PrintAdjacencies(node.Key);
        }
    }

    /// <summary>
    /// Gets the adjacent rooms when given a room ID.
    /// </summary>
    /// <param name="roomID">The room ID to check for adjacencies.</param>
    /// <returns>The list of rooms adjacent to the given room.</returns>
    public List<int> GetAdjacencies(int roomID)
    {
        if (adjacencyList.ContainsKey(roomID))
            return adjacencyList[roomID];
        else
            return null;
    }

    /// <summary>
    /// Prints the adjacencies for the room given.
    /// </summary>
    /// <param name="roomID">The room ID to print the adjacencies for.</param>
    public void PrintAdjacencies(int roomID)
    {
        if (adjacencyList.ContainsKey(roomID))
        {
            string roomDisplay = "Room ID #" + roomID + " is connected to: ";

            foreach (var neighbor in adjacencyList[roomID])
            {
                roomDisplay += neighbor + " ";
            }
            
            Debug.Log(roomDisplay);
        }
        else
        {
            Debug.Log("Room ID #" + roomID + " does not exist.");
        }
    }
}