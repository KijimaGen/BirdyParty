/**
 * @file GameManager.cs
 * @brief ゲーム全体で管理したい物の管理者
 * @author Sum1r3
 * @date 2025/10/14
 */
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : SystemObject{
    //オンラインかどうか
    [SerializeField]
    private bool isOnline;
    //自身のインスタンス
    public static GameManager instance;
    //オンラインマネージャーとオフラインマネージャー
    [SerializeField]
    private GameObject OnlineManager;
    [SerializeField]
    private GameObject OfflineManager;
    //インプットマネージャー
    PlayerInput playerInput;
    //パーティモードかどうか
    public bool isPartyMode { get; private set; }

    public const string PREF_PARTY_RUNNING = "PartyModeRunning";

    [Header("InputManagerのプレファブ")]
    [SerializeField]
    GameObject InputManagerPrefab;

    private void Awake(){
        isPartyMode = PlayerPrefs.GetInt(PREF_PARTY_RUNNING, 0) == 1;
    }

    public override async UniTask Initialize() {
        
        instance = this;

        SetIsOnline(isOnline);

        playerInput = OfflineManager.GetComponent<PlayerInput>();

        await UniTask.CompletedTask;
    }

    /// <summary>
    /// オンラインかどうか
    /// </summary>
    /// <returns></returns>
    public bool IsOnline() {
        return isOnline;
    }

    /// <summary>
    /// オンラインかどうかをセット
    /// </summary>
    public void SetIsOnline(bool t) {
        isOnline = t;

        //オンラインマネージャーなかったら取得
        if(OnlineManager == null) {
            Transform childTransform = transform.Find("NetWorkManager");
            OnlineManager = childTransform.gameObject;
        }

        //オフラインマネージャーなかったら取得
        GetOfflineManager();

        if (t) {
            //オンラインマネージャーをアクティブにして、オフラインマネージャーを破壊
            OnlineManager.SetActive(true);
            
            Destroy(OfflineManager);
        }
        else {
            //オンラインマネージャーをかくして、オフラインマネージャーを再生性
            OnlineManager.SetActive(true);
            //重複しないように
            if(OfflineManager == null)
            Instantiate(InputManagerPrefab,this.gameObject.transform);
        }
    }

    public void OnBackToSelect() {
        // 戻ったことを記録
        if (GameDataManager.instance != null)
            GameDataManager.instance.comeBackFromGame = true;

        SceneManager.LoadScene("Title");
    }

    public void SetPartyMode(bool enabled)
    {
        isPartyMode = enabled;
        PlayerPrefs.SetInt(PREF_PARTY_RUNNING, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// オフラインマネージャーを取得
    /// </summary>
    private void GetOfflineManager() {
        if (OfflineManager == null) {
            Transform childTransform = transform.Find("PlayerInputManager");
            if (childTransform != null)
                OfflineManager = childTransform.gameObject;
        }
    }
}
