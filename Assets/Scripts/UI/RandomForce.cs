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
        float randomX = Random.Range(-7f, 7f);
        if (randomX <= 0) randomX -= 3f;
        if (randomX > 0) randomX += 3f;
        float randomY = Random.Range(4f, 10f);
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
