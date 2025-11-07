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
using UnityEngine.SceneManagement;

public class GameManager : SystemObject{
    //オンラインかどうか
    [SerializeField]
    private bool isOnline;
    //
    public static GameManager instance;
    //オンラインマネージャーとオフラインマネージャー
    [SerializeField]
    private GameObject OnlineManager;
    [SerializeField]
    private GameObject OfflineManager;

    public override async UniTask Initialize() {
        isOnline = true;
        instance = this;

        SetIsOnline(true);

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
        if (t) {
            OnlineManager.SetActive(true);
            OfflineManager.SetActive(false);
        }
        else {
            OnlineManager.SetActive(false);
            OfflineManager.SetActive(true);
        }
    }

    public void OnBackToSelect() {
        // 戻ったことを記録
        if (GameDataManager.instance != null)
            GameDataManager.instance.comeBackFromGame = true;

        SceneManager.LoadScene("Title");
    }
}
