using NUnit.Framework;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerTests
{
    protected PlayerMovement playerObject;

    [SetUp]
    protected virtual void Init()
    {
        LoadPlayer();
    }

    protected void LoadPlayer()
    {
        string prefabPath = "Assets/Prefabs/Player/PlayerNewMovement.prefab";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        playerObject = GameObject.Instantiate(playerPrefab).GetComponent<PlayerMovement>();

        Assert.IsNotNull(playerObject, "Player prefab has not been loaded.");
    }

    [TearDown]
    protected virtual void Cleanup()
    {
        if (playerObject != null)
            GameObject.Destroy(playerObject);
    }
}

[TestFixture]
public class OnPlayerMovement : PlayerTests
{
    [SetUp]
    protected override void Init()
    {
        base.Init();
    }

    [UnityTest]
    public IEnumerator Test_MoveLeft()
    {
        playerObject.SetCharacterMovement(Vector2.left);
        yield return new WaitForSeconds(1.0f);
        Assert.That(playerObject.transform.position.x, Is.LessThan(0.0f));
    }

    [UnityTest]
    public IEnumerator Test_MoveRight()
    {
        playerObject.SetCharacterMovement(Vector2.right);
        yield return new WaitForSeconds(1.0f);
        Assert.That(playerObject.transform.position.x, Is.GreaterThan(0.0f));
    }

    [TearDown]
    protected override void Cleanup()
    {
        base.Cleanup();
    }
}
