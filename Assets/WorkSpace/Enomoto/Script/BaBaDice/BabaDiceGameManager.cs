using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameConst;

public class BabaDiceGameManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerState
    {
        public int id;
        public DiceActor dice;
        public bool alive = true;
        public int score = 0;
        public int lastRoll = 0;

        // 脱落管理
        public int eliminatedTurn = -1;
        public int eliminatedOrder = -1; // 0,1,2... の順で脱落
    }

    [Header("Setup")]
    [SerializeField] private Transform[] spawnPoints; // 最大4
    [SerializeField] private DiceActor dicePrefab;
    [SerializeField] private Material[] playerMats;   // 人数分（色違い）

    [Header("Rules")]
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private int maxTurns = 5;
    [SerializeField] private float turnTimeLimit = 8f; // 制限時間

    [Header("UI")]
    [SerializeField] private BabaDiceUI ui;

    private List<PlayerState> players = new();
    private int turnIndex = 0; // 1..5表示は+1
    private int babaNumber = 1;
    private int eliminatedCount = 0;

    private void Start()
    {
        SetupPlayers(maxPlayers);
        StartCoroutine(GameLoop());
    }

    private void Update()
    {
        // 任意ロール（キーボード）
        if (Input.GetKeyDown(KeyCode.Alpha1)) TryRoll(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TryRoll(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TryRoll(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TryRoll(3);
    }

    private void TryRoll(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return;

        var p = players[playerIndex];
        if (!p.alive) return;

        if (p.lastRoll > 0) return;

        // ダイスが無ければ召喚
        if (p.dice == null)
            SpawnDiceForPlayer(playerIndex);

        p.dice.RollNow();
    }

    private void SpawnDiceForPlayer(int playerIndex)
    {
        var spawn = spawnPoints[playerIndex];
        var dice = Instantiate(dicePrefab, spawn.position, spawn.rotation);

        var mat = (playerMats != null && playerIndex < playerMats.Length) ? playerMats[playerIndex] : null;
        dice.Setup(playerIndex, mat);
        dice.OnRollFinalized += OnPlayerRollFinalized;

        players[playerIndex].dice = dice;
    }

    private void SetupPlayers(int count)
    {
        players.Clear();
        count = Mathf.Clamp(count, 1, Mathf.Min(4, spawnPoints.Length));

        for (int i = 0; i < count; i++)
        {
            var dice = Instantiate(dicePrefab, spawnPoints[i].position, spawnPoints[i].rotation);
            var mat = (playerMats != null && i < playerMats.Length) ? playerMats[i] : null;
            dice.Setup(i, mat);
            dice.OnRollFinalized += OnPlayerRollFinalized;

            players.Add(new PlayerState { id = i, dice = dice });
        }

        ui?.Init(players.Select(p => p.id).ToArray());
        ui?.UpdateScores(players);
        ui?.ShowResult(false);
    }

    private IEnumerator GameLoop()
    {
        eliminatedCount = 0;
        turnIndex = 0;

        // 5ターン
        while (turnIndex < maxTurns)
        {
            // 生存者が1人なら即終了
            if (AlivePlayers().Count <= 1)
                break;

            turnIndex++;
            babaNumber = Random.Range(1, 7); // 1..6

            // ターン開始
            foreach (var p in players) 
            {
                p.lastRoll = 0;
                if (p.dice != null) p.dice.BeginTurn();
            }
            ui?.SetTurn(turnIndex, maxTurns);
            ui?.SetBaba(babaNumber);
            ui?.UpdateScores(players);
            ui?.ClearLastRolls(players);

            // ロール開始合図
            ui?.ShowStartRoll();

            // タイマー開始
            float remain = turnTimeLimit;
            while (remain > -1f)
            {
                ui?.SetTimer(remain, turnTimeLimit);

                // 全員（生存者）が「ロール確定」したら次へ
                if (AllAliveFinalized())
                    break;

                remain -= Time.deltaTime;
                yield return null;
            }

            // 時間切れ：未ロールだけ強制ロール
            foreach (var p in AlivePlayers())
            {
                if (p.dice == null) continue;

                // 未確定の人だけ
                if (!p.dice.HasFinalizedThisTurn)
                    p.dice.ForceRoll();
            }

            // 強制ロール後：着地（確定）を待つ
            float finalizeWait = 6f; // ここは好みで 4〜8 秒
            while (finalizeWait > 0f && !AllAliveDiceFinalized())
            {
                finalizeWait -= Time.deltaTime;
                yield return null;
            }

            // ---- それでも確定しない人がいた時の保険（物理事故対策）
            if (!AllAliveDiceFinalized())
            {
                foreach (var p in AlivePlayers())
                {
                    if (p.dice == null) continue;

                    if (!p.dice.HasFinalizedThisTurn)
                    {
                        // 強制確定（ランダムで決める）
                        int fallback = Random.Range(1, 7);
                        p.dice.ForceFinalizeForSafety(fallback);
                    }
                }

                // 保険確定を反映させるため 1フレーム待つ
                yield return null;
            }

            // ここまでで確定しない場合の保険（物理が不安定な時）
            // → それでも決まらないならランダム確定にする等も可
            yield return new WaitForSeconds(0.3f);
        }

        // 終了 → 順位計算
        var ranking = BuildRanking();
        ui?.ShowResult(true);
        ui?.SetResult(ranking);

        Debug.Log("GAME END");

        //画面遷移
        DiceFinish();
    }

    private void OnPlayerRollFinalized(DiceActor dice, int value)
    {
        var p = players.First(x => x.id == dice.PlayerId);
        if (!p.alive) return;

        p.lastRoll = value;
        ui?.SetPlayerLastRoll(p.id, value);

        if (value == babaNumber)
        {
            // 脱落
            p.alive = false;
            p.eliminatedTurn = turnIndex;
            p.eliminatedOrder = eliminatedCount++;
            p.dice.Eliminate();

            ui?.SetPlayerEliminated(p.id, true);

            // 生存1人になったらこのターン中でも終了へ寄せたいなら
            // → GameLoop側の「AlivePlayers().Count<=1」で次ループ前に止まる
        }
        else
        {
            // 加点
            p.score += value;
            ui?.UpdateScores(players);
        }
    }

    private List<PlayerState> AlivePlayers() => players.Where(p => p.alive).ToList();

    private bool AllAliveFinalized()
    {
        // aliveの人が全員 lastRoll > 0 になったらOK
        return AlivePlayers().All(p => p.lastRoll > 0);
    }

    private bool AllAliveDiceFinalized()
    {
        return AlivePlayers().All(p => p.dice != null && p.dice.HasFinalizedThisTurn);
    }

    private List<PlayerState> BuildRanking()
    {
        // 要件:
        // - 脱落した人は「その時点で最下位から順に割り当て」
        //   → 先に脱落した人ほど下位（=最下位側）
        // - 生存者はスコアで上位

        var alive = players.Where(p => p.alive).OrderByDescending(p => p.score).ToList();
        var eliminated = players.Where(p => !p.alive).OrderBy(p => p.eliminatedOrder).ToList();

        // eliminatedOrder: 0(最初に脱落) が “最下位”
        // 表示を「1位→」にしたいので、下位に回す
        var ranking = new List<PlayerState>();
        ranking.AddRange(alive);
        ranking.AddRange(eliminated.Reverse<PlayerState>()); // ここは表示好みで調整
        // もし「最初に脱落した人が最下位」表示にしたいなら Reverse しない

        return ranking;
    }

    private async void DiceFinish()
    {
        await AfterGoal();
    }

    private async UniTask AfterGoal()
    {

        //五秒ほど待って
        await UniTask.Delay(5000);

        if (GameManager.instance != null && GameManager.instance.isPartyMode && PartyModeManager.instance != null)
        {
            // パーティ：次へ進めてルーレット（タイトル）へ戻す
            PartyModeManager.instance.OnMiniGameFinishedAndReturnToRoulette();
            return;
        }

        //画面遷移
        SceneManager.LoadScene(TITLE_SCENE_NAME);
    }
}
