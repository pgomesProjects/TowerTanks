using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class particlecaller : MonoBehaviour
{
    // Start is called before the first frame update

    void Start()
    {
        ParticleSystem part = GetComponent<ParticleSystem>();
        part.Play(true);
        //Destroy(gameObject, part.main.duration);

    }

    // Update is called once per frame
    void Update()
    {
        
        

    }
}
