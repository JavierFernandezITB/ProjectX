using UnityEngine;

public class ServicesReferences : MonoBehaviour
{
    protected NetworkService networkService;
    protected EntityService entityService;
    protected TouchManagerService touchManagerService;

    protected virtual void GetServices()
    {
        networkService = GameObject.Find("/networkService")?.GetComponent<NetworkService>();
        entityService = GameObject.Find("/entityService")?.GetComponent<EntityService>();
        touchManagerService = GameObject.Find("/touchManagerService")?.GetComponent<TouchManagerService>();
    }

    protected virtual void Persist<T>() where T : UnityEngine.Object
    {
        if (FindObjectsOfType<T>().Length > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}