using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MyTestScript
{
    [UnityTest]
    public IEnumerator TestFuelDepletion()
    {
        var gameObject = new GameObject();
        gameObject.AddComponent<Rigidbody2D>();
        var testPlayer = gameObject.AddComponent<PlayerMovement>();
        testPlayer.SetJetpackInput(true);

        yield return new WaitForSeconds(2.0f);

        Assert.AreEqual(true, testPlayer.GetFuel() < 100);
    }
}
