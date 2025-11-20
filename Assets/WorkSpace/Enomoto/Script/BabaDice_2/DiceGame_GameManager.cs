using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiceGame_GameManager : MonoBehaviourPunCallbacks
{
    public static DiceGame_GameManager instance;

    [Header("Game Settings")]
    public int maxTurns = 5;
    public float turnTimeLimit = 10f;

    [Header("References")]
    public Transform[] playerSpawnPoints;   // プレイヤーの生成位置
    public TextMeshProUGUI infoText;        // ゲーム状況表示用テキスト
    public TextMeshProUGUI timerText;       // タイマー表示用テキスト
    public TextMeshProUGUI babaNumberText;  // BABAナンバー表示用
    public TextMeshProUGUI resultText;      // 結果表示用

    // 内部ステート
    private int currentTurn = 0;
    private int currentBabaNumber = -1;
    private float currentTimer = 0f;
    private bool isRollingPhase = false;

    // プレイヤーデータ管理 (Key: ActorNumber)
    private Dictionary<int, int> playerScores = new Dictionary<int, int>();
    private Dictionary<int, bool> playerAliveStatus = new Dictionary<int, bool>();
    private Dictionary<int, int> currentTurnRolls = new Dictionary<int, int>(); // そのターンの出目

    // 参加人数
    private int totalPlayers = 0;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (!PhotonNetwork.IsConnected) return;

        // プレイヤーデータの初期化
        foreach (var player in PhotonNetwork.PlayerList)
        {
            playerScores[player.ActorNumber] = 0;
            playerAliveStatus[player.ActorNumber] = true;
        }
        totalPlayers = PhotonNetwork.PlayerList.Length;

        // マスタークライアントのみがゲームループを開始
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(GameLoop());
        }
    }

    private void Update()
    {
        // マスタークライアントが時間を管理して同期する手もあるが、
        // ここでは簡易的にマスターのタイマーを表示用に使う、あるいはOnPhotonSerializeViewで同期推奨
        // 今回は要件の「時間はマスターが観測」に従い、マスターで処理します
        if (PhotonNetwork.IsMasterClient && isRollingPhase)
        {
            currentTimer -= Time.deltaTime;
            photonView.RPC(nameof(UpdateTimerUI), RpcTarget.All, currentTimer);

            if (currentTimer <= 0)
            {
                isRollingPhase = false;
                // 時間切れ処理：まだ振っていない人に強制ロール命令などを出す
                photonView.RPC(nameof(ForceStopRolling), RpcTarget.All);
            }
        }
    }

    // ゲームのメインループ (Masterのみ実行)
    private IEnumerator GameLoop()
    {
        // 全員がロード完了するまで少し待つなど必要であればここで待機
        yield return new WaitForSeconds(2f);

        for (currentTurn = 1; currentTurn <= maxTurns; currentTurn++)
        {
            // 生存者が1人以下なら終了
            if (GetAlivePlayerCount() <= 1 && totalPlayers > 1) break;

            // --- ターン開始 ---
            // BABAダイス決定 (1-6)
            currentBabaNumber = Random.Range(1, 7);
            currentTurnRolls.Clear();

            // 全員にターン開始とBABAを通知
            photonView.RPC(nameof(StartTurnRPC), RpcTarget.All, currentTurn, currentBabaNumber);

            // ロール時間
            currentTimer = turnTimeLimit;
            isRollingPhase = true;

            // 全員がダイスを振り終わる or 時間切れまで待機
            // (WaitUntilで全員分のRoll報告が来るのを待つ)
            yield return new WaitUntil(() => !isRollingPhase || currentTurnRolls.Count >= GetAlivePlayerCount());

            // 念のため少し待つ（物理演算の完全停止など）
            yield return new WaitForSeconds(2f);

            // --- 集計と脱落判定 ---
            CalculateTurnResult();

            // 結果共有
            photonView.RPC(nameof(ShowTurnResultRPC), RpcTarget.All);
            yield return new WaitForSeconds(3f);
        }

        // --- ゲーム終了 ---
        EndGame();
    }

    // 生存者数をカウント
    private int GetAlivePlayerCount()
    {
        return playerAliveStatus.Count(x => x.Value == true);
    }

    // --- RPCs ---

    [PunRPC]
    public void StartTurnRPC(int turn, int babaNum)
    {
        infoText.text = $"ターン {turn}/{maxTurns}";
        babaNumberText.text = $"BABADice: {babaNum}";
        resultText.text = "";

        // 自分のダイスの入力許可などをここで行う通知を出しても良い
        Debug.Log($"Turn {turn} Start! BABA is {babaNum}");
    }

    [PunRPC]
    public void UpdateTimerUI(float time)
    {
        timerText.text = $"Time: {Mathf.Max(0, time):F1}";
    }

    [PunRPC]
    public void ForceStopRolling()
    {
        // クライアント側でまだ振っていない場合強制的に振るなどの処理をDiceControllerに委譲
        // 今回は単純にフェーズを終わらせるトリガーとして使用
    }

    // クライアントから出目の報告を受ける (Masterのみ実行)
    [PunRPC]
    public void ReportRollResult(int actorNumber, int diceNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"Player {actorNumber} rolled {diceNumber}");
        if (!currentTurnRolls.ContainsKey(actorNumber))
        {
            currentTurnRolls.Add(actorNumber, diceNumber);
        }
    }

    // 集計ロジック (Masterのみ)
    private void CalculateTurnResult()
    {
        string log = "";

        foreach (var entry in currentTurnRolls)
        {
            int pActor = entry.Key;
            int roll = entry.Value;

            if (!playerAliveStatus[pActor]) continue; // 既に脱落している

            if (roll == currentBabaNumber)
            {
                // BABAを引いた -> 脱落
                playerAliveStatus[pActor] = false;
                log += $"Player {pActor} OUT (Rolled {roll})\n";
            }
            else
            {
                // セーフ -> 得点加算
                playerScores[pActor] += roll;
                log += $"Player {pActor} +{roll} pts\n";
            }
        }
        Debug.Log(log);
    }

    [PunRPC]
    public void ShowTurnResultRPC()
    {
        // 現状のスコアなどを表示
        // 実際にはもっとリッチなUIで演出する
        string display = "Turn Results:\n";
        foreach (var p in PhotonNetwork.PlayerList)
        {
            int score = playerScores.ContainsKey(p.ActorNumber) ? playerScores[p.ActorNumber] : 0;
            bool isAlive = playerAliveStatus.ContainsKey(p.ActorNumber) ? playerAliveStatus[p.ActorNumber] : true;
            string status = isAlive ? "SAFE" : "OUT";
            display += $"{p.NickName}: {score}pts [{status}]\n";
        }
        resultText.text = display;
    }

    private void EndGame()
    {
        // 最終結果発表
        // 生存者の中で最も点数が高い人が勝ち
        var survivors = playerScores.Where(x => playerAliveStatus[x.Key]).OrderByDescending(x => x.Value);

        string winnerName = "No One";
        if (survivors.Any())
        {
            int winnerActor = survivors.First().Key;
            Player p = PhotonNetwork.CurrentRoom.GetPlayer(winnerActor);
            winnerName = p != null ? p.NickName : $"Player {winnerActor}";
        }

        photonView.RPC(nameof(ShowFinalResult), RpcTarget.All, winnerName);
    }

    [PunRPC]
    public void ShowFinalResult(string winner)
    {
        infoText.text = "GAME OVER";
        resultText.text = $"WINNER: {winner}!";
        babaNumberText.text = "";
    }
}
