using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BabaGameManager : MonoBehaviour
{
    [Header("サイコロ参照")]
    public BabaDiceRoll babaDice;    // BABA専用ダイス
    public DiceRoll playerDice;      // プレイヤー用ダイス（1つ）

    [Header("プレイヤーリスト")]
    public List<PlayerController> players = new List<PlayerController>();

    [Header("設定")]
    public int totalTurns = 5;

    private int currentTurn = 0;
    private int currentBabaValue = -1;

    private void Start()
    {
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        Debug.Log("🎲 BABAダイスゲーム開始！");

        for (currentTurn = 1; currentTurn <= totalTurns; currentTurn++)
        {
            Debug.Log($"====================\nターン {currentTurn}\n====================");

            // --- BABAダイスを振る ---
            babaDice.RollDice();
            yield return new WaitUntil(() => !babaDice.IsRollingDone); // 止まるまで待機

            currentBabaValue = babaDice.ResultValue;
            Debug.Log($"このターンのBABAは【{currentBabaValue}】です！");

            // --- 各プレイヤーの番 ---
            foreach (var p in players)
            {
                if (p.isEliminated) continue;

                Debug.Log($"▶ {p.playerName} のターン");

                if (p.isNPC)
                {
                    yield return new WaitForSeconds(1f);
                    playerDice.RollDice();
                }
                else
                {
                    Debug.Log("スペースキーでダイスを転がしてください！");
                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
                    playerDice.RollDice();
                }

                yield return new WaitUntil(() => playerDice.IsRollingDone);

                int result = playerDice.ResultValue;
                Debug.Log($"{p.playerName} の出目：{result}");

                if (result == currentBabaValue)
                {
                    p.Eliminate();
                    Debug.Log($"{p.playerName} はBABA（{result}）を出して脱落！");
                }
                else
                {
                    p.AddScore(result);
                }

                yield return new WaitForSeconds(1f);
            }

            // --- 生き残りチェック ---
            int survivors = players.FindAll(p => !p.isEliminated).Count;
            if (survivors <= 1)
            {
                Debug.Log("💥 1人になったためゲーム終了！");
                break;
            }

            yield return new WaitForSeconds(2f);
        }

        AnnounceWinner();
    }

    private void AnnounceWinner()
    {
        List<PlayerController> alive = players.FindAll(p => !p.isEliminated);
        if (alive.Count == 1)
        {
            Debug.Log($"🏆 勝者：{alive[0].playerName}！");
        }
        else
        {
            int maxScore = -1;
            PlayerController winner = null;
            foreach (var p in alive)
            {
                if (p.totalScore > maxScore)
                {
                    maxScore = p.totalScore;
                    winner = p;
                }
            }
            Debug.Log($"🏁 勝者：{winner.playerName}（合計スコア {winner.totalScore}）");
        }
    }
}