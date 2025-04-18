#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Linq;
using System.Reflection;
using Random = UnityEngine.Random;

namespace TowerTanks.Scripts
{
    public class TankStudio : OdinEditorWindow
    {
        private Camera editorCamera;
        private RenderTexture renderTexture;

        private int tankDesignWindowWidth = 700;
        private int tankDesignWindowHeight = 700;

        private GameObject currentTank;
        private Room currentRoom;
        private Vector3 tankPos = new (500, 0, 15);

        private GameObject[] roomPrefabs;          
        private Texture2D[] roomPreviewImages;   
        private int selectedRoomIndex = -1;      

        private Vector2 scrollPosition;         
        private List<GameObject> cleanup = new ();

        [MenuItem("Tools/Tank Studio")]
        private static void OpenWindow()
        {
            GetWindow<TankStudio>().Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            roomPrefabs = new GameObject[0];
            GameObject cameraObject = new GameObject("Editor Camera");
            editorCamera = cameraObject.AddComponent<Camera>();


            editorCamera.orthographic = true;        
            editorCamera.orthographicSize = 5;       
            editorCamera.clearFlags = CameraClearFlags.SolidColor;
            editorCamera.backgroundColor = Color.cyan;
            
            editorCamera.transform.rotation = Quaternion.identity;    

            // the render texture is how we display the camera to an editor window
            renderTexture = new RenderTexture(tankDesignWindowWidth, tankDesignWindowHeight, 16, RenderTextureFormat.ARGB32);
            editorCamera.targetTexture = renderTexture;

            Vector3 cameraPos = tankPos;
            cameraPos.y += 3.45f;
            cameraPos.z -= 5;
            editorCamera.transform.position = cameraPos;

            GameObject tankPrefab = Resources.Load<GameObject>("TankPrefabs/Tank1");
            currentTank = Instantiate(tankPrefab);
            currentTank.name = "Tank1_Instance";
            currentTank.transform.position = tankPos;
            
            CallAwakeAndStart(currentTank, cullOldRooms: true);
            
            LoadRoomPrefabs();
            
            EditorApplication.update += Repaint;
        }
        
        private void CallAwakeAndStart(GameObject tank, bool cullOldRooms = false)
        {   // this uses a cool technique i looked into called reflection which lets you call methods without knowing their names at compile time.
            //useful for this scenario where we want to initialize mono-behaviours outside of the normal Unity lifecycle without having to know type names n stuff
            if (cullOldRooms)
            {
                Room[] allRooms = tank.GetComponentsInChildren<Room>(true);
                for (int i = 0; i < allRooms.Length; i++)
                {
                    if (allRooms[i].name != "CoreRoom_A")
                    {
                        EditModeRoom newRoomScript = allRooms[i].gameObject.AddComponent<EditModeRoom>();
                        newRoomScript.assetKit = Resources.Load<RoomAssetKit>("RoomKits/RoomKit_Proto");
                        DestroyImmediate(allRooms[i]);
                    }
                }
            }
            
            List<MonoBehaviour> allMonoBehaviours = tank.GetComponentsInChildren<MonoBehaviour>(true).ToList();
            foreach (MonoBehaviour monoBehaviour in allMonoBehaviours)
            {
                if (monoBehaviour != null)
                {
                    // manually calls Awake using reflection (if not already called)
                    MethodInfo awakeMethod = monoBehaviour.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (awakeMethod != null)
                    {
                        awakeMethod.Invoke(monoBehaviour, null);
                    }

                    // Manually call Start using reflection (if not already called)
                    MethodInfo startMethod = monoBehaviour.GetType().GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (startMethod != null)
                    {
                        startMethod.Invoke(monoBehaviour, null);
                    }

                    void BackWallSetter(SpriteRenderer backWallRenderer)
                    {
                        Color color = backWallRenderer.color;
                        color.a = 0f;
                        backWallRenderer.color = color;
                    }
                    if (monoBehaviour is Cell cell)
                    {
                        SpriteRenderer backWallRenderer = cell.backWall.GetComponent<SpriteRenderer>();
                        BackWallSetter(backWallRenderer);
                    }
                    else if (monoBehaviour is Connector connector)
                    {
                        SpriteRenderer backWallRenderer = connector.backWall.GetComponent<SpriteRenderer>();
                        BackWallSetter(backWallRenderer);
                    }
                }
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (editorCamera)
                DestroyImmediate(editorCamera.gameObject);
            if (renderTexture)
                DestroyImmediate(renderTexture);
            if (currentTank)
                DestroyImmediate(currentTank);

            foreach (var obj in cleanup)
            {
                DestroyImmediate(obj);
            }
            
            EditorApplication.update -= Repaint;
        }

        protected override void OnImGUI()
        {
            base.OnImGUI();
            if (editorCamera == null || renderTexture == null)
                return;
            
            GUILayout.BeginArea(new Rect(0, 0, position.width, position.height));

            //sets up the layout for the dropdown for rooms and the camera view to be next to each other
            GUILayout.BeginHorizontal();

            // Shows the dropdown (Room Selector) on the left
            GUILayout.BeginVertical(GUILayout.Width(300)); // change the width here for the scroll box's width
            GUILayout.Label("Rooms", EditorStyles.boldLabel);

            // creates our room scroll area
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUILayout.ExpandHeight(true), GUILayout.Width(260)); // Adjust width here

            GUILayout.Label("Select Room", EditorStyles.label);
            
            GUILayout.BeginHorizontal();
            for (int i = 0; i < roomPrefabs.Length; i++)
            {
                // this creates a 3 column layout (the 3 changes how many buttons are in a row)
                if (i % 3 == 0 && i > 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                // creates button for each room
                if (GUILayout.Button(new GUIContent(roomPreviewImages[i], roomPrefabs[i].name), GUILayout.Width(60), GUILayout.Height(60)))
                {
                    selectedRoomIndex = i;
                    LoadRoomIntoScene();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView(); // End of the scrollable area

            GUILayout.EndVertical();

            // The camera preview should go to the right
            GUILayout.BeginVertical(GUILayout.Width(tankDesignWindowWidth)); // Right section for the camera view
            GUILayout.Label("Tank Design:", EditorStyles.boldLabel);
            Rect textureRect = GUILayoutUtility.GetRect(tankDesignWindowWidth, tankDesignWindowHeight);
            GUI.DrawTexture(textureRect, renderTexture, ScaleMode.ScaleToFit);
            GUILayout.EndVertical();
            DrawTankDetailsColumn();
            GUILayout.EndHorizontal(); // this ends the original hoz layout, ending it here lets us put the render texture to the right of the dropdown
            
            GUILayout.EndArea();
            
            Vector2 mousePos = Event.current.mousePosition;
            if (textureRect.Contains(mousePos))
            {
                HandleCameraInputs();
                if (currentRoom != null)
                {
                    Vector3 worldPos = editorCamera.ScreenToWorldPoint(GetMousePositionInRenderTexture(textureRect));
                    currentRoom.SnapMove(worldPos);
                    currentRoom.transform.position = new Vector3(currentRoom.transform.position.x,
                        currentRoom.transform.position.y,
                        15);
                    HandleBuildInputs();
                }
            } 
        }
        
        private void DrawTankDetailsColumn()
        {
            GUILayout.BeginVertical(GUILayout.Width(200)); // Set the width of the column
            GUILayout.Label("Tank Details", EditorStyles.boldLabel);

            // Tank Name input field
            GUILayout.BeginHorizontal();
            GUILayout.Label("Tank Name:", GUILayout.Width(80));
            currentTank.name = GUILayout.TextField(currentTank.name, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            // Randomize Name button
            if (GUILayout.Button("Randomize Name", GUILayout.Width(200)))
            {
                string newName = GenerateName(); // Generate a new name
                currentTank.name = newName; // Update the tank's name
            }
            if (GUILayout.Button("Save Design!", GUILayout.Width(200)))
            {
                SaveDesign();
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Design saved to: Assets/Resources/TankDesigns/{currentTank.name}", GUILayout.Width(80));
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Load Design", GUILayout.Width(200)))
            {
                string filePath = EditorUtility.OpenFilePanel("Select a JSON File", "Assets/Resources/TankDesigns", "json");
                if (!string.IsNullOrEmpty(filePath))
                {
                    Debug.Log($"Selected file: {filePath}");
                    TankController tc = currentTank.GetComponent<TankController>();
                    
                    string jsonContent = File.ReadAllText(filePath);
                    if (jsonContent != null)
                    {
                        TankDesign _design = JsonUtility.FromJson<TankDesign>(jsonContent);
                        tc.Build(_design, usedFromEditor:true);
                    }
                }
                else
                {
                    Debug.LogWarning("No file selected.");
                }
            }

            GUILayout.EndVertical();
        }
        
        public string GenerateName()
        {
            var generator = new TankNameGenerator();
            TankNames nameType = null;
            int randomNumber = Random.Range(1, 4);
            switch (randomNumber)
            {
                case 1:
                    nameType = Resources.Load<TankNames>("TankNames/PirateNames");
                    break;
                case 2:
                    nameType = Resources.Load<TankNames>("TankNames/RobotNames");
                    break;
                case 3:
                    nameType = Resources.Load<TankNames>("TankNames/TestNames");
                    break;
            }
            return generator.GenerateRandomName(nameType);
            //choose random number 1-4
        }
        
        public void SaveDesign()
        {
            // Get the current design
            TankController tc = currentTank.GetComponent<TankController>();
            TankDesign design = tc.GetCurrentDesign();
            if (design != null)
            {
                Debug.Log("Saving design...");
                string json = JsonUtility.ToJson(design, true);
                string directoryPath = "Assets/Resources/TankDesigns/";
                string filePath = directoryPath + currentTank.name + ".json";

                // Ensure the directory exists
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    Debug.Log($"Created directory: {directoryPath}");
                }

                try
                {
                    File.WriteAllText(filePath, json);
                    Debug.Log($"Design saved successfully to: {filePath}");
#if UNITY_EDITOR
                    AssetDatabase.Refresh();
#endif
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to save design: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning("No design found to save.");
            }
        }

        private void HandleBuildInputs()
        {
            Event e = Event.current;
            switch (e.type) //input handling
            {
                case EventType.MouseDown:
                    currentRoom.Mount();
                    if (currentRoom.mounted) //if the mount was successful
                    {
                        currentRoom = null;
                    }
                    break;
                case EventType.KeyDown:
                    switch (e.keyCode) //which key was pressed?
                    {
                        case KeyCode.R:
                            currentRoom.Rotate();
                            break;
                    }
                    break;
            }
        }

        private void HandleCameraInputs()
        {
            if (Event.current.type == EventType.MouseDrag && Event.current.button == 1) // rmb
            {
                Vector3 delta = new Vector3(-Event.current.delta.x, Event.current.delta.y, 0) * 0.02f; // .02 is the panning speed
                editorCamera.transform.position += delta;
                Event.current.Use(); // this stops the event from being processed further, that can cause errors
            }

            // Camera zooming with scroll wheel
            if (Event.current.type == EventType.ScrollWheel)
            {
                float zoomDelta = Event.current.delta.y * 0.1f; // Adjust zoom sensitivity
                editorCamera.orthographicSize = Mathf.Clamp(editorCamera.orthographicSize + zoomDelta, 1f, 20f); // Clamp zoom range
                Event.current.Use(); // Consume the event
            }
        }

        private Vector2 GetMousePositionInRenderTexture(Rect textureRect)
        {
            Vector2 mousePos = Event.current.mousePosition;
            
            if (!textureRect.Contains(mousePos))
            {
                return Vector2.negativeInfinity; // Returns an invalid position if outside
            }

            // local mouse position relative to the render texture
            Vector2 localMousePos = new Vector2(mousePos.x - textureRect.x, mousePos.y - textureRect.y);

            //we need to flip the Y-axis to match the render texture's coordinate system
            localMousePos.y = textureRect.height - localMousePos.y;

            return localMousePos;
        }
        
        private void LoadRoomIntoScene()
        {
            var curr = Instantiate(roomPrefabs[selectedRoomIndex]);
            Room oldRoomScript = curr.GetComponent<Room>();
            EditModeRoom newRoomScript = curr.AddComponent<EditModeRoom>();
            newRoomScript.assetKit = Resources.Load<RoomAssetKit>("RoomKits/RoomKit_Proto");
            DestroyImmediate(oldRoomScript);
            newRoomScript.Awake();
            newRoomScript.Start();
            currentRoom = curr.GetComponent<Room>();
            currentRoom.transform.position = tankPos;
            cleanup.Add(curr);
            CallAwakeAndStart(currentRoom.gameObject);
        }
        private void LoadRoomPrefabs()
        {
            var roomPool = Resources.LoadAll<GameObject>("TankPrefabs/RoomPool");
            roomPrefabs = roomPool;   
            roomPreviewImages = new Texture2D[roomPrefabs.Length];

            // loads preview images
            for (int i = 0; i < roomPrefabs.Length; i++)
            {
                // assumes each prefab has a preview image in the TankIcons/Rooms folder
                Sprite roomSprite = Resources.Load<Sprite>("TankIcons/Rooms/" + roomPrefabs[i].name);

                //if the sprite is found, assign its texture to the preview images array
                if (roomSprite != null)
                {
                    roomPreviewImages[i] = roomSprite.texture;
                }
                else
                {
                    Debug.LogWarning($"Sprite for {roomPrefabs[i].name} not found!");
                }
            }
        }
    }
}
#endif
