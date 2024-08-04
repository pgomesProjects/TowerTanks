using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class StackManager : MonoBehaviour
{
    //Classes, Enums & Structs:
    public class StackItem
    {
        [Tooltip("The interactable associated with this stack item.")] public GameObject interactablePrefab;
        [Tooltip("Identification number unique to this stack item.")]  public int uid;
        [Tooltip("How many times this item has been built.")]          public int deployments;
    }

    //STATIC STUFF:
    [Tooltip("Single persistent instance of stack manager in game.")]                                            public static StackManager main;
    [Tooltip("The stack is a list of all non-installed interactables stored in players' collective inventory.")] public static List<StackItem> stack = new List<StackItem>();
    [Tooltip("List of currently-deployed stack items (may be returned to the stack).")]                          public static List<StackItem> inactiveStack = new List<StackItem>();

    private static int lastTakenUid = 0; //Last assigned uid (uids are assigned sequentially). All uids are >0. Used to prevent duplicate uids
    /// <summary>
    /// Generates a unique identification number for a new stack item.
    /// </summary>
    public static int GetNewUid()
    {
        lastTakenUid++;      //Increment last taken uid tracker
        return lastTakenUid; //Send new uid
    }

    //Objects & Components:

    //Settings:

    //Runtime Variables:

    //RUNTIME METHODS:
    private void Awake()
    {
        //Initialization:
        if (main != null) { Destroy(this); } else { main = this; } //Singleton-ize this script in scene
    }

    //OPERATION METHODS:
    /// <summary>
    /// Adds interactable to the stack so that it can be generated from there (can be with an interactable which has always been added).
    /// </summary>
    /// <param name="interactable">Interactable to be added.</param>
    public static void AddToStack(TankInteractable interactable)
    {
        //Find/Generate stack item:
        StackItem item = null; //Initialize container to store relevant stack item
        if (interactable.stackId != 0) //Interactable has previously been a stack item
        {
            foreach (StackItem inactiveItem in inactiveStack) { if (inactiveItem.uid == interactable.stackId) item = inactiveItem; break; } //Find stack item in inactive items list
            if (item != null) //Stack item has been found in inactive stack
            {
                inactiveStack.Remove(item); //Remove item from inactive stack
            }
        }
        if (item == null) //No stack item has been found
        {
            item = new StackItem();                           //Create new stack item
            item.uid = GetNewUid();                           //Generate a new ID for stack item
            item.interactablePrefab = interactable.prefabRef; //Get reference to prefab so interactable can be respwaned later
        }
        stack.Add(item); //Add item to bottom of stack
        print("added item to stack, stack now contains " + stack.Count + " items");
    }

    /// <summary>
    /// Removes the interactable on top of the stack and returns a SPAWNED instance of it.
    /// </summary>
    public static TankInteractable BuildTopStackItem()
    {
        //Validity checks:
        if (stack.Count == 0) //Stack is empty
        {
            Debug.LogError("Tried to take item from empty stack."); //Indicate problem
            return null;                                            //Return nothing
        }

        //Move item out of stack:
        StackItem takenItem = stack[0]; //Get item on top of stack
        inactiveStack.Add(takenItem);   //Add taken item to inactive stack
        stack.RemoveAt(0);              //Remove item from main stack

        //Stack item updates:
        takenItem.deployments++; //Update deployment counter

        //Build interactable:
        TankInteractable newInteractable = Instantiate(takenItem.interactablePrefab).GetComponent<TankInteractable>(); //Instantiate new interactable from known prefab
        newInteractable.stackId = takenItem.uid;                                                                       //Assign id so that interactable may be called back later

        //Cleanup:
        print("removed item from stack, stack now contains " + stack.Count + " items");
        return newInteractable; //Return prefab from taken stack item
    }
}
