using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    //[ExecuteInEditMode]
    public class EditModeRoom : Room //this class only exists to inject DestroyImmediate
    {
        public override void DestroyObj(GameObject obj)
        {
            DestroyImmediate(obj);
        }

        public override void Rotate(bool clockwise = true)
        {
            base.Rotate();
            fixZDepth(connectors);
            fixZDepth(cells);
            fixZDepth(hatches);
            fixZDepth(backWallSprite);
        }
        public void fixZDepth<T>(List<T> objects) where T : Component
        {
            foreach (T obj in objects)
            {
                obj.transform.position = new Vector3(obj.transform.position.x,
                    obj.transform.position.y, 15);
            }
        }
        
        public void fixZDepth<T>(T obj) where T : Component
        {
            obj.transform.position = new Vector3(obj.transform.position.x,
                obj.transform.position.y, 15);
        }
    }
}
