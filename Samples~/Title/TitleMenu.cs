using UnityEngine;
using UnityEngine.SceneManagement;

namespace elZach.GameJam.Ready
{
    public class TitleMenu : MonoBehaviour
    {
        public void LoadScene(int index)
        {
            SceneManager.LoadScene(index);
        }

        public void LoadNextScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        public void OpenURL(string url)
        {
            Application.OpenURL(url);
        }
    }
}