/**
 * @file PartyModeManager.cs
 * @brief パーティモードの管理者
 * @author Sum1r3
 * @date 2026/01/14
 */
using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PartyModeManager : SystemObject
{
    // 抽選されたシーン
    private List<string> ChoicedSceneList = new List<string>();

    // 今何個目のゲームをやっているか（0開始）
    public int NowGameIndex { get; private set; }

    // プレイヤーのランキングリスト(必要かどうかは不明)
    public List<PlayerInfomation> playerRankingList = new List<PlayerInfomation>();

    // 一応こいつにも何回抽選を行うかを保存してもらう
    private int GameChoiceCount = 3; // <- 一時的に三つにしておく

    // ルーレットに戻る時にButtonManagerに分岐させるためのキー
    public const string PREF_BACK_TO_PARTY = "ComeBackFromParty";

    public const string PREF_PARTY_RUNNING = "PartyModeRunning";


    private const string ROOMPROP_CHOICED_LIST = "ChoicedGameList";
    private const string ROOMPROP_CHOICED_INDEX = "ChoicedGameIndexList";

    // タイトル（ルーレットがあるシーン名）※プロジェクトに合わせて変更してください
    private const string TITLE_SCENE_NAME = "Title";

    // ゲームをランダム抽選してくれる奴
    [SerializeField] private SystemObject gamePickerPrefab;

    // 自身のインスタンス
    public static PartyModeManager instance;

    // フォトンビューの参照
    PhotonView pv;

    private void Start()
    {
        _ = Initialize();
    }

    // 初期化処理
    public override async UniTask Initialize()
    {
        // インスタンスを作成
        if (instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        instance = this;

        // NowGameIndexの初期化
        NowGameIndex = 0;

        // ゲーム選択アイテム生成
        SystemObject createObject = Instantiate(gamePickerPrefab, transform);
        // 初期化
        await createObject.Initialize();

        // フォトンビューの参照の取得
        if (GameManager.instance.IsOnline())
            pv = GetComponent<PhotonView>();

        // 自身を非破壊オブジェクトに
        DontDestroyOnLoad(gameObject);

        await UniTask.CompletedTask;
    }

    /// <summary>
    /// ゲームリストの作成
    /// </summary>
    public void MakeGameList()
    {
        NowGameIndex = 0;

        PlayerPrefs.SetInt(PREF_PARTY_RUNNING, 1);
        PlayerPrefs.SetInt(PREF_BACK_TO_PARTY, 0);
        PlayerPrefs.Save();

        // オフラインは今まで通りローカル抽選でOK
        if (!GameManager.instance.IsOnline())
        {
            PartyModeGamePicker.instance.SetRandomCount(GameChoiceCount);
            ChoicedSceneList = PartyModeGamePicker.instance.BuildGameIndexs();

            DebugPrintPartyGameList(" (Offline Lottery)");
            return;
        }

        // オンライン：Master以外は抽選しない
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[PartyMode] Not Master: skip lottery. wait room properties.");
            return;
        }

        // Masterが抽選する
        PartyModeGamePicker.instance.SetRandomCount(GameChoiceCount);
        ChoicedSceneList = PartyModeGamePicker.instance.BuildGameIndexs();

        // デバッグ用 ゲームリスト表示
        DebugPrintPartyGameList(" (Master Lottery)");

        // sprites用 index リストも保存する
        int[] indexArray = PartyModeGamePicker.instance.GetGameIndexList().ToArray();

        // ルームのカスタムプロパティに両方入れる
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["ChoicedGameList"] = ChoicedSceneList.ToArray();
        props["ChoicedGameIndexList"] = indexArray;

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        Debug.Log($"[DBG] IndexListCount={PartyModeGamePicker.instance.GetGameIndexList().Count}");
    }


    public override void OnRoomPropertiesUpdate(Hashtable props) {
        if (props.ContainsKey("ChoicedGameList")) {
            string[] list = (string[]) props["ChoicedGameList"];
            ChoicedSceneList = new List<string>(list);
        }
    }

    [PunRPC]
    public void SetChoicedGameList(string[] ChoicedGameList)
    {
        // 一度リストを初期化
        ChoicedSceneList.Clear();

        for (int i = 0, max = ChoicedGameList.Length; i < max; i++)
            ChoicedSceneList.Add(ChoicedGameList[i]);

        // ネット受信側でも開始地点は0に揃える
        NowGameIndex = 0;
    }

    public List<string> GetChoicedGameList() => ChoicedSceneList;

    /// <summary>次のゲームへ進める</summary>
    public void IncreaseNowGameIndex()
    {
        NowGameIndex++;
    }

    /// <summary>
    /// 次にルーレットで表示すべきゲームの index（sprites 側の配列番号用）
    /// </summary>
    public int GetNextGameIndex()
    {
        // 範囲外なら安全に0返し（※本当は終了扱い）
        if (NowGameIndex < 0) return 0;
        if (PartyModeGamePicker.instance == null) return 0;

        var idxList = PartyModeGamePicker.instance.GetGameIndexList();
        if (idxList == null || idxList.Count == 0) return 0;
        if (NowGameIndex >= idxList.Count) return 0;

        return idxList[NowGameIndex];
    }

    /// <summary>
    /// ButtonManager の NextSceneName を次のゲームに更新
    /// </summary>
    public void SetNextSceneName()
    {
        if (ButtonManager.instance == null) return;
        if (ChoicedSceneList == null || ChoicedSceneList.Count == 0) return;
        if (NowGameIndex < 0 || NowGameIndex >= ChoicedSceneList.Count) return;

        ButtonManager.instance.SetNextSceneName(ChoicedSceneList[NowGameIndex]);
    }

    public List<PlayerInfomation> GetPlayerRankList() => playerRankingList;

    /// <summary>
    /// ミニゲーム終了時に呼ぶ：次へ進めてルーレット（タイトル）へ戻す
    /// </summary>
    public void OnMiniGameFinishedAndReturnToRoulette()
    {
        NowGameIndex++;

        // 全部終わったか？
        if (NowGameIndex >= ChoicedSceneList.Count)
        {
            // ===== パーティ終了 =====
            Debug.Log("[PartyMode] Party Finished");

            // ここが超重要
            GameManager.instance?.SetPartyMode(false);

            // パーティ用フラグも消す
            PlayerPrefs.SetInt(PREF_BACK_TO_PARTY, 0);
            PlayerPrefs.SetInt(PREF_PARTY_RUNNING, 0);
            PlayerPrefs.Save();

            // 通常のタイトルへ
            SceneManager.LoadScene(TITLE_SCENE_NAME);
            return;
        }

        // ===== まだ続く =====
        PlayerPrefs.SetInt(PREF_BACK_TO_PARTY, 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene(TITLE_SCENE_NAME);
    }

    private void LoadTitleScene()
    {
        if (GameManager.instance != null && GameManager.instance.IsOnline())
        {
            PhotonNetwork.LoadLevel(TITLE_SCENE_NAME);
        }
        else
        {
            SceneManager.LoadScene(TITLE_SCENE_NAME);
        }
    }

    public void ApplySyncedSceneList(string[] arr)
    {
        ChoicedSceneList.Clear();
        ChoicedSceneList.AddRange(arr);

        // 非Master側もインデックス開始を揃える
        NowGameIndex = 0;
    }

    public void DebugPrintPartyGameList(string label = "")
    {
        if (ChoicedSceneList == null || ChoicedSceneList.Count == 0)
        {
            Debug.Log($"[PartyMode]{label} GameList is EMPTY");
            return;
        }

        string log = $"[PartyMode]{label} Game Order => ";
        for (int i = 0; i < ChoicedSceneList.Count; i++)
        {
            log += $"[{i}] {ChoicedSceneList[i]} ";
        }

        Debug.Log(log);
    }

    /// <summary>
    /// プレイヤーのランキングリストをポイント順に並び替える
    /// </summary>
    public void SortPlayerRanking() {
        //一度キャッシュ
        var newRankingList = playerRankingList;
        //ポイント順に並び替える
        newRankingList.Sort((a,b) => a.myPoint.CompareTo(b.myPoint));
        //ランキングでは逆順なのでさかさまにする
        newRankingList.Reverse();
        //出来上がったリストを反映させて完成
        playerRankingList = newRankingList;
    }

    /// <summary>
    /// プレイヤーランキングリストに追加
    /// </summary>
    /// <param name="player"></param>
    public void AddPlayerRanking(PlayerInfomation player) {
        playerRankingList.Add(player);
    }

}
