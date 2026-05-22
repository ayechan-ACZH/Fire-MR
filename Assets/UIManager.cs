using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManager : MonoBehaviour
{   

    [SerializeField] private GameObject[] UIpages;
    // Start is called before the first frame update

    [SerializeField] private GameObject gameOverSlide;
    public void hideAllPages()
    {
        foreach (GameObject page in UIpages)
        {
            page.SetActive(false);
        }
    }  

    public void showPage(int pageIndex)
    {
        hideAllPages();
        UIpages[pageIndex].SetActive(true);
    }

    public void RestartCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public void QuitApplication()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void finishSlide()
    {
        showPage(UIpages.Length - 1);
    }

    public void showGameOverSlide()
    {
        gameOverSlide.SetActive(true);
    }

    


}
