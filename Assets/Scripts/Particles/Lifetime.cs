using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lifetime : MonoBehaviour
{
    public float lifeTime = 2f;

    // Update is called once per frame
    void Update()
    {
        if (lifeTime > 0) lifeTime -= Time.deltaTime;
        else Destroy(gameObject);
    }


}
