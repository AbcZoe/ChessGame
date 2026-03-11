using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    void Start()
    {
        SceneManager.LoadScene("StartScene", LoadSceneMode.Single);
    }
}