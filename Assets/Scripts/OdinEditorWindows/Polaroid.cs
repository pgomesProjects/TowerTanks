#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace TowerTanks.Scripts.OdinTools
{
    [ExecuteInEditMode]
    public class Polaroid : OdinEditorWindow
    {
        //Objects & Components:
        private Camera cam;                           //Camera used to take snapshots
        [PreviewField(250)] public RenderTexture tex; //Temp texture camera renders to

        //CONTROLS:
        [Button("Take Snapshot")]
        private void TakeSnapshot()
        {
            //Initialize:
            tex = new RenderTexture(outputResolution, outputResolution, 25);                                      //Update texture width and height
            cam.targetTexture = tex;                                                                              //Reset camera target texture
            Texture2D outputTex = new Texture2D(outputResolution, outputResolution, TextureFormat.RGBA32, false); //Create a texture to store the output
            Rect rect = new Rect(0, 0, outputResolution, outputResolution);                                       //Create a rectangle to specify region of texture to render

            //Render to texture:
            cam.Render(); //Force cam to render once
            RenderTexture currentRenderTex = RenderTexture.active; //Save active render texture so it can be switched back to later
            RenderTexture.active = tex;                            //Set camera output texture to active
            outputTex.ReadPixels(rect, 0, 0);                      //Read pixels onto our output texture
            outputTex.Apply();                                     //Apply change in pixels
            RenderTexture.active = currentRenderTex;               //Change current render texture back to original
            tex = new RenderTexture(100, 100, 25);                 //Reset render texture values
            cam.targetTexture = tex;                               //Reset camera target texture

            //Store texture as sprite:
            Sprite outputSprite = Sprite.Create(outputTex, rect, Vector2.zero); //Generate output sprite
            var spritePath = "Assets/" + saveLocation + saveName + ".asset";    //Generate name of path to save sprite to
            var texPath = "Assets/" + saveLocation + saveName + "_tex.asset";   //Generate name of path to save texture to
            AssetDatabase.CreateAsset(outputTex, texPath);                      //Save texture as asset in designated folder
            AssetDatabase.CreateAsset(outputSprite, spritePath);                //Save sprite as asset in designated folder
        }
        [SerializeField, Tooltip("Width and height of output texture (in pixels)."), Min(1)] private int outputResolution = 1000;
        [SerializeField, Tooltip("How large the camera frame is."), Min(0)] private float size = 1;
        [SerializeField, Tooltip("Offset amount between camera and subject.")] private Vector2 offset;
        [SerializeField, Tooltip("Name of sprite asset to save.")] private string saveName = "NewPolaroid";
        [SerializeField, Tooltip("Path in assets to folder where sprites will be saved.")] private string saveLocation = "Art Assets/Polaroids/";
        [Space()]
        [SerializeField, Tooltip("If not null, camera will generate an instance of this prefab to photograph.")] private GameObject subject;

        //Runtime Vars:
        private GameObject currentSubject; //Last known photo subject

        //UNITY METHODS:
        private void OnEnable()
        {
            base.OnEnable();

            //Generate camera:
            if (cam == null) //System does not already have a camera
            {
                //Setup camera:
                cam = new GameObject().AddComponent<Camera>();  //Create a temporary camera in scene
                cam.name = "PolaroidCam";                       //Name camera
                cam.transform.position = Vector3.forward * 100; //Move camera so that it always captures subject field (but misses normal game stuff)
                tex = new RenderTexture(100, 100, 25);          //Generate new render texture with default values
                cam.targetTexture = tex;                        //Set target texture
                cam.orthographic = true;                        //Make camera orthographic
                cam.orthographicSize = size;                    //Apply default size setting
            }
        }
        private void OnValidate()
        {
            //Settings updates:
            cam.orthographicSize = size; //Adjust camera size

            //Update subject:
            if (subject != null) //Subject is designated
            {
                if (currentSubject == null || currentSubject.name.Replace("(Clone)", "") != subject.name) //Subject has changed
                {
                    if (currentSubject != null) DestroyImmediate(currentSubject); //Destroy current subject if valid
                    currentSubject = Instantiate(subject);                        //Instantiate new subject
                }

                //Update subject position:
                Vector3 newSubjectPos = cam.transform.position;    //Get base position from camera
                newSubjectPos.z = cam.transform.position.z + 100;  //Put subject on plane in front of camera
                newSubjectPos += (Vector3)offset;                  //Offset subject position according to setting
                currentSubject.transform.position = newSubjectPos; //Apply positional change
            }
            else if (currentSubject != null) //Subject needs to be removed
            {
                DestroyImmediate(currentSubject); //Destroy current subject
            }
        }
        public void OnDestroy()
        {
            base.OnDestroy();
            DestroyImmediate(cam.gameObject);                             //Destroy camera
            if (currentSubject != null) DestroyImmediate(currentSubject); //Destroy current subject if valid
        }

        //UTLILTY METHODS:
        [MenuItem("Tools/Polaroid")] //Allows menu to be opened from the Tools dropdown menu
        private static void OpenWindow()
        {
            //NOTE: Copied from Sirenix tutorial (https://www.youtube.com/watch?v=O6JbflBK4Fo)
            GetWindow<Polaroid>().Show(); //Open this window in the Unity editor
        }
    }
}
#endif
