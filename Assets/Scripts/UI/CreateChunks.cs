using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateChunks : MonoBehaviour
{
    public int chunksMin;
    public int chunksMax;
    public GameObject chunk;

    // Start is called before the first frame update
    void Start()
    {
        int random = Random.Range(chunksMin, chunksMax + 1);
        for (int i = 0; i < random; i++) {
            Instantiate(chunk, transform.position, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
