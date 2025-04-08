#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Linq;

namespace TowerTanks.Scripts.OdinTools
{
    public class RoomGenerationWizard : OdinEditorWindow
    {
        //Objects & Components:


        //Settings:


        //Runtime Variables:


        //MENU ELEMENTS:
        [SerializeField, Tooltip("List of all spawnable prefabs in room pool folder")] private GameObject[] roomPrefabs;
        [Button("Refresh Room List")]
        private void RefreshRoomList()
        {
            IEnumerable<GameObject> foundPrefabs = GetPrefabs("Assets/Prefabs/Tanks/RoomPool");                            //Return array of room prefabs
            roomPrefabs = (from prefab in foundPrefabs where !prefab.GetComponent<Room>().isCore select prefab).ToArray(); //Eliminate core rooms from list
        }

        [PropertySpace(20)]
        [SerializeField, Tooltip("Tank in scene which spawned rooms will be attached to.")] private TankController targetTank;
        [SerializeField, Tooltip("The room currently being manipulated by the wizard")] private Room selectedRoom;
        [Button("Spawn Room", buttonSize: ButtonSizes.Large)]
        private void SpawnRoom()
        {
            //Valitity checks:
            if (roomPrefabs.Length == 0) { Debug.LogError("Tried to spawn room when room list is empty. Try hitting the refresh button!."); return; } //Make sure room prefab list is populated

            var currentScene = EditorSceneManager.GetActiveScene();                    //Get active scene (use for validity checks later)
            GameObject roomToSpawn = roomPrefabs[Random.Range(0, roomPrefabs.Length)]; //Get room to spawn
            selectedRoom = Instantiate(roomToSpawn).GetComponent<Room>();              //Spawn room and get reference to it
            if (targetTank != null) //Target tank is selected
            {
                selectedRoom.transform.parent = targetTank.coreRoom.transform.parent; //Child room to tank's room parent
                selectedRoom.targetTank = targetTank;                                 //Set target tank
                selectedRoom.transform.localPosition = Vector3.zero;                  //Zero out position relative to tank
            }
        }

        //UTLILTY METHODS:
        [MenuItem("Tools/Room Generation Wizard")] //Allows menu to be opened from the Tools dropdown menu
        private static void OpenWindow()
        {
            //NOTE: Copied from Sirenix tutorial (https://www.youtube.com/watch?v=O6JbflBK4Fo)
            GetWindow<RoomGenerationWizard>().Show(); //Open this window in the Unity editor
        }
        public static List<GameObject> GetPrefabs(string path)
        {
            //NOTE: Copied from Sirenix tutorial (https://www.youtube.com/watch?v=O6JbflBK4Fo)

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:prefab", new[] { path });
            List<GameObject> prefabs = new List<GameObject>();

            foreach (var guid in guids)
            {
                UnityEditor.AssetDatabase.GUIDFromAssetPath(guid);
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                prefabs.Add(UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject);
            }

            return prefabs;
#else
            return new List<GameObject>();
#endif
        }
    }
}
#endif
