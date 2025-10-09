using UnityEngine;
using UnityEngine.SceneManagement;


public class ButtonManager : MonoBehaviour
{
    [Header("UI定義")]
    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject modeUI;
    [SerializeField] private GameObject optionUI;
    [SerializeField] private GameObject netWorkUI;
    [SerializeField] private GameObject onlineUI;
    [SerializeField] private GameObject minigameSelectUI;
    [SerializeField] private GameObject playerSelectUI;

    [Header("通信切り替え時の表示用")]
    [SerializeField] private GameObject offLine;
    [SerializeField] private GameObject onLine;

    private void Start()
    {
        titleUI.SetActive(true);
    }

    public void Open(GameObject openUI)
    {
        if (openUI == modeUI) titleUI.SetActive(false);
        if (openUI == optionUI) modeUI.SetActive(false);
        if (openUI == netWorkUI) modeUI.SetActive(false);
        if (openUI == onlineUI) netWorkUI.SetActive(false);
        if (openUI == minigameSelectUI) modeUI.SetActive(false);
        if (openUI == playerSelectUI)
        {
            minigameSelectUI.SetActive(false);
            modeUI.SetActive(false);
        }

            openUI.SetActive(true);
    }

    public void Back(GameObject closeUI)
    {
        closeUI.SetActive(false);

        if (closeUI == modeUI) titleUI.SetActive(true);
        if (closeUI == optionUI) modeUI.SetActive(true);
        if (closeUI == netWorkUI) modeUI.SetActive(true);
        if (closeUI == onlineUI) netWorkUI.SetActive(true);
        if (closeUI == minigameSelectUI) modeUI.SetActive(true);
        if (closeUI == playerSelectUI) modeUI.SetActive(true);
    }

    public void localSetting()
    {
        netWorkUI.SetActive(false);
        modeUI.SetActive(true);
        offLine.SetActive(true);
        onLine.SetActive(false);
    }

    public void roomOpen()
    {
        onlineUI.SetActive(false);
        modeUI.SetActive(true);
        offLine.SetActive(false);
        onLine.SetActive(true);
    }

    public void startGame(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
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
