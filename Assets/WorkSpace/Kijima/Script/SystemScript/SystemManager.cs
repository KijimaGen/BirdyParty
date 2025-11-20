/**
 * @file SystemManager.cs
 * @brief ゲーム全体で使用する機能の管理
 * @author yao
 * @date 2025/10/02
 */

using Cysharp.Threading.Tasks;
using UnityEngine;

public class SystemManager : MonoBehaviour {
    /// <summary>
    /// 管理するシステムオブジェクトのリスト
    /// </summary>
    [SerializeField]
    private SystemObject[] _systemObjectList = null;
    //重複を防ぐためのinstance
    public static SystemManager instance;

    private void Awake() {
        if (instance != null) {
            Destroy(gameObject);
            return;
        }
        UniTask task = Initialize();
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 初期化処理
    /// </summary>
    /// <returns></returns>
    private async UniTask Initialize() {
        // 全システムオブジェクトの生成、初期化
        for (int i = 0, max = _systemObjectList.Length; i < max; i++) {
            SystemObject origin = _systemObjectList[i];
            if (origin == null) continue;
            // システムオブジェクト生成
            SystemObject createObject = Instantiate(origin, transform);
            // 初期化
            await createObject.Initialize();
        }
        // 音楽の再生
        AudioManager.instance.PlayBGM(0);
        // フェードインの呼び出し
        await FadeManager.instance.FadeIn();
    }

}
