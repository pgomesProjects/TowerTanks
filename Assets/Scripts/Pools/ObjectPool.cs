using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<O> : MonoBehaviour where O : Component
{
    [SerializeField, Tooltip("The object to use for the object pool.")] private O poolObject;
    [SerializeField, Tooltip("The initial pool size of the object pool.")] private int initialPoolSize = 10;

    //Object pool lists
    private Queue<O> objectQueue;
    private List<O> activeObjects;

    private void Awake() => InitializeObjectPool();

    /// <summary>
    /// Initializes the object pool.
    /// </summary>
    private void InitializeObjectPool()
    {
        //Create the lists for the pool
        objectQueue = new Queue<O>();
        activeObjects = new List<O>();

        //Preload objects and add them to the queue
        for(int i = 0; i < initialPoolSize; i++)
        {
            O newObject = CreateObject();
            objectQueue.Enqueue(newObject);
        }
    }

    /// <summary>
    /// Creates a new pool object.
    /// </summary>
    /// <returns>The GameObject created.</returns>
    private O CreateObject()
    {
        //Create an object and child it to the object pool
        O newObject = Instantiate(poolObject, transform);
        newObject.name = poolObject.name;

        //Hide the object and return the component
        newObject.gameObject.SetActive(false);
        return newObject;
    }

    /// <summary>
    /// Gets an object from the pool.
    /// </summary>
    /// <param name="position">The world position of the object.</param>
    /// <param name="rotation">The quaternion rotation of the object.</param>
    /// <param name="parent">The parent of the object.</param>
    /// <returns>The GameObject retrieved from the object pool.</returns>
    public O GetObject(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        O newObject;
        //If there are objects in the queue, dequeue an object from it
        if (objectQueue.Count > 0)
            newObject = objectQueue.Dequeue();
        //Otherwise, make a new object
        else
            newObject = CreateObject();

        //Apply properties to the object
        newObject.transform.position = position;
        newObject.transform.rotation = rotation;
        newObject.transform.SetParent(parent);

        //Show the object and return the component
        newObject.gameObject.SetActive(true);
        activeObjects.Add(newObject);
        return newObject;
    }

    //Overloaded signatures for GetObject
    public O GetObject(Vector3 position, Transform parent = null) => GetObject(position, Quaternion.identity, parent);
    public O GetObject(Transform parent = null) => GetObject(Vector3.zero, Quaternion.identity, parent);

    /// <summary>
    /// Returns an object from the pool.
    /// </summary>
    /// <param name="obj">The object to return.</param>
    public void ReturnObject(O obj)
    {
        //Hide the object
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        obj.gameObject.SetActive(false);

        //Remove the object from the active list and add it back to the queue
        activeObjects.Remove(obj);
        objectQueue.Enqueue(obj);
    }
}
