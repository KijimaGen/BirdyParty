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
    [SerializeField] private GameObject gameReadyUI;

    [Header("通信切り替え時の表示用")]
    [SerializeField] private GameObject offLine;
    [SerializeField] private GameObject onLine;

    private static ButtonManager instance;
    private string currentMiniGame = null;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

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
        if (openUI == gameReadyUI)
        {
            playerSelectUI.SetActive(false);
            minigameSelectUI.SetActive(false);
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
        if (closeUI == gameReadyUI) minigameSelectUI.SetActive(true);
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

    // ? ミニゲーム開始（Additive方式）
    public void startGame(string sceneName)
    {
        currentMiniGame = sceneName;
        // UI全部閉じる（またはローディング表示）
        titleUI.SetActive(false);
        modeUI.SetActive(false);
        optionUI.SetActive(false);
        netWorkUI.SetActive(false);
        onlineUI.SetActive(false);
        minigameSelectUI.SetActive(false);
        playerSelectUI.SetActive(false);
        gameReadyUI.SetActive(false);

        // ミニゲームをAdditiveで読み込み
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    // ? ミニゲーム終了・UI選択に戻る
    public void ReturnToSelect()
    {
        if (!string.IsNullOrEmpty(currentMiniGame))
        {
            SceneManager.UnloadSceneAsync(currentMiniGame);
            currentMiniGame = null;
        }

        // UIを再表示
        titleUI.SetActive(false);
        modeUI.SetActive(false);
        optionUI.SetActive(false);
        netWorkUI.SetActive(false);
        onlineUI.SetActive(false);

        // 選択UIを再表示
        minigameSelectUI.SetActive(true);
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
