using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using TMPro;

namespace TowerTanks.Scripts
{
    public class StackManager : MonoBehaviour
    {
        //Classes, Enums & Structs:
        public class StackItem
        {
            //Basic Data:
            [Tooltip("Name of prefab used to spawn interactable.")] public string interactableName;
            [Tooltip("Identification number unique to this stack item.")] public int uid;
            [Tooltip("How many times this item has been built.")] public int deployments;

            //References:
            [Tooltip("Interactable prefab associated with this item.")] public GameObject prefab;
            [Tooltip("UI object representing this stack item.")] public RectTransform uiPanel;

            //Animation Variables:
            [Tooltip("Tracker for time since panel position data has been updated.")] public float timeSinceAnimUpdate;
            [Tooltip("Position at start of current animation.")] public Vector2 prevPosition;

            [Tooltip("The current progress bar shown for the item being built.")] private TaskProgressBar buildProgressBar;
            [Tooltip("The list of player indices selecting the current stack item.")] private List<int> playersSelecting;

            //OPERATION METHODS:
            /// <summary>
            /// Generates and fills in data for a UI panel representing this stack item.
            /// </summary>
            /// <param name="currentStackIndex">The index for the UI panel in the stack.</param>
            public void GenerateUIPanel(int currentStackIndex)
            {
                Debug.Log("Generating UI Panel At Index " + currentStackIndex);

                //Initialize:
                if (activeStackUI == null || stackUIContainer == null) { Debug.LogError("Tried to generate a UI panel while stackUI was not active"); return; }               //Prevent system from generating UI while HUD is inactive
                RectTransform newPanel = Instantiate(StackManager.main.stackItemUIPrefab, stackUIContainer).GetComponent<RectTransform>();  //Generate empty UI panel in stack UI object
                TankInteractable interactableScript = prefab.GetComponent<TankInteractable>();                                                    //Get script from prefab so data can be parsed from it

                //Set panel properties:
                newPanel.GetChild(1).GetComponent<Image>().sprite = interactableScript.uiImage;           //Set UI image
                newPanel.GetChild(2).GetComponent<TextMeshProUGUI>().text = interactableScript.stackName; //Set UI name
                newPanel.gameObject.name = interactableScript.stackName;                                  //Set UI GameObject name
                uiPanel = newPanel;                                                                       //Give item a reference to its UI panel

                //Prep animation:
                if (currentStackIndex == 0) //This is the first item in the stack
                {
                    newPanel.localPosition = GetTargetPos();            //Set position instantly
                    timeSinceAnimUpdate = StackManager.main.ySlideTime; //Do not set to animate
                }
                else //Other items are already in the stack
                {
                    newPanel.localPosition = stack[currentStackIndex - 1].uiPanel.localPosition; //Set position to that of previous item in stack
                    timeSinceAnimUpdate = 0;                                  //Have animation begin
                }
                prevPosition = newPanel.localPosition; //Simply set previous position to current

                playersSelecting = new List<int>();
            }
            /// <summary>
            /// Deactivates UI panel (for when item is built).
            /// </summary>
            public void HideUIPanel()
            {
                uiPanel.gameObject.SetActive(false); //Make panel invisible
            }
            /// <summary>
            /// Activate or update UI panel (for when item is destroyed (again)).
            /// </summary>
            public void ShowUIPanel()
            {
                uiPanel.gameObject.SetActive(true); //Make panel visible

                //Prep animation:
                if (stack.IndexOf(this) == 0) //This is the first item in the stack
                {
                    uiPanel.localPosition = GetTargetPos();             //Set position instantly
                    timeSinceAnimUpdate = StackManager.main.ySlideTime; //Do not set to animate
                }
                else //Other items are already in the stack
                {
                    uiPanel.localPosition = stack[^2].uiPanel.localPosition; //Set position to that of previous item in stack
                    timeSinceAnimUpdate = 0;                                 //Have animation begin
                }
                prevPosition = uiPanel.localPosition; //Simply set previous position to current
            }
            /// <summary>
            /// Updates animation status of UI panel (for when an item is removed).
            /// </summary>
            public void UpdateUIPanel()
            {
                prevPosition = uiPanel.localPosition; //Set current position to previous so that panel can animate from here
                timeSinceAnimUpdate = 0;              //Have panel begin animating and let GetTargetPos do the work
            }

            /// <summary>
            /// Starts the build progress bar.
            /// </summary>
            /// <param name="duration">The duration of the build time.</param>
            public void StartBuildProgressBar(float duration)
            {
                //If there's a progress bar already active, end it
                if (buildProgressBar != null)
                    buildProgressBar.EndTask();

                buildProgressBar = GameManager.Instance.UIManager.AddRadialTaskBar(uiPanel.gameObject, new Vector2(123.3f, 0f), duration, false);
            }

            /// <summary>
            /// Cancels the build progress bar.
            /// </summary>
            public void CancelBuildProgressBar()
            {
                buildProgressBar?.EndTask();
                buildProgressBar = null;
            }

            public void ClearPlayersSelected()
            {
                foreach (Transform trans in uiPanel.GetChild(3))
                    Destroy(trans.gameObject);

                playersSelecting.Clear();
            }

            //DATA METHODS:
            /// <summary>
            /// Returns the local position this stack item's UI should be at based on its position in the stack.
            /// </summary>
            public Vector2 GetTargetPos() { return new Vector2(StackManager.main.firstItemPosition.x, StackManager.main.firstItemPosition.y - (StackManager.main.targetItemSeparation * stack.IndexOf(this))); }
        }

        //STATIC STUFF:
        [Tooltip("Single persistent instance of stack manager in game.")] public static StackManager main;
        [Tooltip("The stack is a list of all non-installed interactables stored in players' collective inventory.")] public static List<StackItem> stack = new List<StackItem>();
        [Tooltip("List of currently-deployed stack items (may be returned to the stack).")] public static List<StackItem> inactiveStack = new List<StackItem>();
        [Tooltip("Reference to active stackUI object (needs to be set by HUD controller).")] public static GameObject activeStackUI;
        [Tooltip("Reference to active stackUI container (needs to be set by HUD controller).")] public static Transform stackUIContainer;

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
        [SerializeField, Tooltip("Reference to prefab for stack item UI instances.")] private GameObject stackItemUIPrefab;

        //Settings:
        [Header("Stack UI Properties:")]
        [SerializeField, Tooltip("Base UI position of item on top of stack.")] private Vector2 firstItemPosition;
        [SerializeField, Tooltip("Target Y distance between stack items.")] private float targetItemSeparation;
        [Space()]
        [SerializeField, Tooltip("Time panel elements have to slide to new positions after an item is removed from or added to the stack."), Min(0.01f)] private float ySlideTime;
        [SerializeField, Tooltip("Curve describing movement of Y slide animation.")] private AnimationCurve ySlideCurve;

        //Runtime Variables:

        //RUNTIME METHODS:
        private void Awake()
        {
            //Initialization:
            if (main != null) { Destroy(this); } else { main = this; } //Singleton-ize this script in scene
        }
        private void Start()
        {
            GenerateExistingStack();
        }

        public void GenerateExistingStack()
        {
            //Populate stack UI:
            //Iterate through entire stack
            for (int i = 0; i < stack.Count; i++)
            {
                DisplayNewUIPanel(stack[i], i);
            }
        }

        private void Update()
        {
            //Update stack item positions:
            foreach (StackItem item in stack) //Iterate through items in the stack
            {
                if (item.timeSinceAnimUpdate < ySlideTime) //Animate until animation time has been completed
                {
                    item.timeSinceAnimUpdate = Mathf.Min(item.timeSinceAnimUpdate + Time.deltaTime, ySlideTime);    //Update time tracker
                    float timeValue = ySlideCurve.Evaluate(item.timeSinceAnimUpdate / ySlideTime);                  //Get interpolation value
                    Vector2 newPosition = Vector2.LerpUnclamped(item.prevPosition, item.GetTargetPos(), timeValue); //Get new position for stack item
                    item.uiPanel.transform.localPosition = newPosition;                                             //Set new position
                }
            }
        }

        //OPERATION METHODS:
        /// <summary>
        /// Adds interactable to the stack so that it can be generated from there (can be with an interactable which has always been added).
        /// </summary>
        /// <param name="currentInteractable">Interactable to be added.</param>
        public static void AddToStack(INTERACTABLE currentInteractable)
        {
            TankInteractable interactable = GameManager.Instance.interactableList[(int)currentInteractable];
            AddToStack(interactable);
        }

        /// <summary>
        /// Adds interactable to the stack so that it can be generated from there (can be with an interactable which has always been added).
        /// </summary>
        /// <param name="currentInteractable">Interactable to be added.</param>
        public static void AddToStack(TankInteractable currentInteractable)
        {
            //Find/Generate stack item:
            StackItem item = null; //Initialize container to store relevant stack item
            if (currentInteractable.stackId != 0) //Interactable has previously been a stack item
            {
                foreach (StackItem inactiveItem in inactiveStack) { if (inactiveItem.uid == currentInteractable.stackId) item = inactiveItem; break; } //Find stack item in inactive items list
                if (item != null) //Stack item has been found in inactive stack
                {
                    inactiveStack.Remove(item); //Remove item from inactive stack
                }
            }
            if (item == null) //No stack item has been found
            {
                //Basic stack item characteristics:
                item = new StackItem();                                                                                                   //Create new stack item
                item.uid = GetNewUid();                                                                                                   //Generate a new ID for stack item
                item.interactableName = currentInteractable.name.Replace("(Clone)", "");                                                         //Get reference to prefab so interactable can be respwaned later
                item.prefab = Resources.Load<RoomData>("RoomData").interactableList.FirstOrDefault(x => x.name == item.interactableName); //Get prefab reference so interactable script can be checked for more info
            }
            stack.Add(item); //Add item to bottom of stack
            print("added " + item.interactableName + " to stack, stack now contains " + stack.Count + " items");

            //UI Update:
            if (activeStackUI != null || stackUIContainer != null) //UI system is active
            {
                DisplayNewUIPanel(item, stack.Count - 1);
            }
        }

        /// <summary>
        /// Displays a new UI panel in the stack.
        /// </summary>
        /// <param name="item">The new stack item to show.</param>
        /// <param name="index">The position of the new stack item in the stack.</param>
        private static void DisplayNewUIPanel(StackItem item, int index)
        {
            if (item.uiPanel == null) item.GenerateUIPanel(index);                //Generate a ui panel for item if it doesn't have one
            else if (!item.uiPanel.gameObject.activeInHierarchy) item.ShowUIPanel();        //Simply make item visible otherwise
            item.uiPanel.SetAsFirstSibling();                                               //Have this panel render under others
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
            takenItem.deployments++;                                //Update deployment counter
            takenItem.HideUIPanel();                                //Hide UI
            foreach (StackItem item in stack) item.UpdateUIPanel(); //Update positions of all other items in the stack

            //Build interactable:
            TankInteractable newInteractable = Instantiate(takenItem.prefab).GetComponent<TankInteractable>(); //Instantiate new interactable from known prefab
            newInteractable.stackId = takenItem.uid;                                                           //Assign id so that interactable may be called back later

            //Cleanup:
            print("removed item from stack, stack now contains " + stack.Count + " items");
            return newInteractable; //Return prefab from taken stack item
        }

        public static TankInteractable GetTopStackItem()
        {
            TankInteractable interactable = null;

            if (stack.Count > 0)
            {
                StackItem item = stack[0];
                interactable = item.prefab.GetComponent<TankInteractable>();
            }

            return interactable;
        }

        public static void StartBuildingStackItem(int index, PlayerMovement player, float duration)
        {
            StackItem item = stack[index];
            //If there is no item, return
            if (item == null)
                return;

            item.StartBuildProgressBar(duration);
        }

        public static void EndBuildingStackItem(int index, PlayerMovement player)
        {
            StackItem item = stack[index];
            //If there is no item, return
            if (item == null)
                return;

            item.CancelBuildProgressBar();
        }

        /// <summary>
        /// Clears the stack information completely.
        /// </summary>
        public static void ClearStack()
        {
            stack.Clear();
            inactiveStack.Clear();
        }
    }
}
