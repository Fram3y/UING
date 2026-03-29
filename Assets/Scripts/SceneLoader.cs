using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneName;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            loadScene(sceneName);
        }
    }

    void loadScene (string scene)
    {
        SceneManager.LoadScene(scene);
    }
}
