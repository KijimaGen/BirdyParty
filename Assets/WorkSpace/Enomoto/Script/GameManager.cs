using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public void OnBackToSelect()
    {
        // –ß‚Á‚½‚±‚Æ‚ð‹L˜^
        if (GameDataManager.Instance != null)
            GameDataManager.Instance.comeBackFromGame = true;

        SceneManager.LoadScene("Title");
    }

    public void OnExit()
    {
        #if UNITY_EDITOR
        
                UnityEditor.EditorApplication.isPlaying = false;
        
        #else
        
                        Application.Quit();
        
        #endif
    }
}
