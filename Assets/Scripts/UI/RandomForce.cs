using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomForce : MonoBehaviour
{
    public Rigidbody2D rb;
    float randomRotation;

    // Start is called before the first frame update
    void Start()
    {
        
        randomRotation = Random.Range(-10f, 10f);
        float randomS = Random.Range(1.0f, 1.8f);
        float randomX = Random.Range(-15f, 15f);
        if (randomX <= 0) randomX -= 10f;
        if (randomX > 0) randomX += 10f;
        float randomY = Random.Range(20f, 30f);
        transform.localScale *= randomS;
        rb.AddForce(new Vector2(randomX, randomY), ForceMode2D.Impulse);
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0f, 0f, randomRotation);

        if (transform.position.y <= -30f) Destroy(gameObject);
    }
}
