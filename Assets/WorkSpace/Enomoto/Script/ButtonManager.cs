using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ButtonManager : MonoBehaviour
{
    [Header("UI定義")]
    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject modeUI;
    [SerializeField] private GameObject minigameSelectUI;
    [SerializeField] private GameObject optionUI;
    [SerializeField] private GameObject netWorkUI;
    [SerializeField] private GameObject onlineUI;
    [SerializeField] private GameObject playerSelectUI;
    [SerializeField] private GameObject gameReadyUI;

    [Header("オンライン定義")]
    [SerializeField] private GameObject online;
    [SerializeField] private GameObject offline;

    public bool playOnline = false;

    // 開いたUIを保存して戻れるように
    private Stack<GameObject> uiHistory = new Stack<GameObject>();

    private void Start()
    {
        // ゲームから戻ってきたかどうかでUIを切り替え
        if (GameDataManager.Instance != null && GameDataManager.Instance.comeBackFromGame)
        {
            titleUI.SetActive(false);
            modeUI.SetActive(false);
            minigameSelectUI.SetActive(true);

            uiHistory.Clear();
            uiHistory.Push(titleUI);
            uiHistory.Push(modeUI);
            uiHistory.Push(minigameSelectUI);

            GameDataManager.Instance.comeBackFromGame = false;
        }
        else
        {
            titleUI.SetActive(true);
            modeUI.SetActive(false);
            minigameSelectUI.SetActive(false);

            uiHistory.Clear();
            uiHistory.Push(titleUI);
        }
    }

    public void Open(GameObject openUI)
    {
        if (uiHistory.Count > 0)
        {
            GameObject current = uiHistory.Peek();
            current.SetActive(false);
        }

        openUI.SetActive(true);
        uiHistory.Push(openUI);

        if (openUI == modeUI)
        {
            playOnline = false;
        }
        else
        {
            playOnline = true;
        }
    }

    public void Back()
    {
        if (uiHistory.Count > 1)
        {
            GameObject closing = uiHistory.Pop();
            closing.SetActive(false);

            GameObject previous = uiHistory.Peek();
            previous.SetActive(true);
        }
    }

    public void PlayStyle()
    {
        if (playOnline)
        {
            online.SetActive(true);
            offline.SetActive(false);
        }
        else
        {
            online.SetActive(false);
            offline.SetActive(true);
        }
    }

    // ミニゲーム開始
    public void StartGame(string sceneName)
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.selectedMiniGame = sceneName;
            GameDataManager.Instance.comeBackFromGame = true;
        }

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
