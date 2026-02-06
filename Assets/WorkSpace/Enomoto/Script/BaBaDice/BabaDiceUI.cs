using Photon.Pun.Demo.PunBasics;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BabaDiceUI : MonoBehaviour
{
    [Header("Top")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Slider timerSlider;

    [Header("BABA")]
    [SerializeField] private TMP_Text babaText;
    [SerializeField] private GameObject[] babaFaces;

    [Header("Players")]
    [SerializeField] private PlayerUI[] playerUIs;

    [Header("Result")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;

    public void Init(int[] playerIds)
    {
        // 使う人数だけ初期化・表示
        for (int i = 0; i < playerUIs.Length; i++)
        {
            bool active = i < playerIds.Length;
            playerUIs[i].gameObject.SetActive(active);
            if (active) playerUIs[i].Init(i);
        }

        ShowResult(false);
        SetBaba(1);
        SetTurn(1, 5);
        SetTimer(0, 1);
    }

    public void SetTurn(int turn, int max)
    {
        if (turnText) turnText.text = $"Turn {turn}/{max}";
    }

    public void SetTimer(float remain, float max)
    {
        if (timerText) timerText.text = $"{Mathf.CeilToInt(remain)}";
        if (timerSlider)
        {
            timerSlider.maxValue = max;
            timerSlider.value = remain;
        }
    }

    public void SetBaba(int baba)
    {
        if (babaText) babaText.text = $"BABA : {baba}";

        // Face1..6 の画像を切り替え
        if (babaFaces != null && babaFaces.Length >= 6)
        {
            for (int i = 0; i < 6; i++)
                if (babaFaces[i]) babaFaces[i].SetActive(i == baba - 1);
        }
    }

    public void SetPlayerLastRoll(int playerId, int value)
    {
        if (playerId < 0 || playerId >= playerUIs.Length) return;
        playerUIs[playerId].SetLastRoll(value);
    }

    public void SetPlayerEliminated(int playerId, bool on)
    {
        if (playerId < 0 || playerId >= playerUIs.Length) return;
        playerUIs[playerId].SetEliminated(on);
    }

    public void UpdateScores(List<BabaDiceGameManager.PlayerState> players)
    {
        foreach (var p in players)
        {
            if (p.id >= 0 && p.id < playerUIs.Length)
                playerUIs[p.id].SetScore(p.score);
        }
    }

    public void ClearLastRolls(List<BabaDiceGameManager.PlayerState> players)
    {
        foreach (var p in players)
        {
            if (p.id >= 0 && p.id < playerUIs.Length)
                playerUIs[p.id].SetLastRoll(0);
        }
    }

    public void ShowResult(bool on)
    {
        if (resultPanel) resultPanel.SetActive(on);
    }

    public void SetResult(List<BabaDiceGameManager.PlayerState> ranking)
    {
        if (!resultText) return;

        // 1位→表示
        var lines = ranking.Select((p, idx) =>
            $"{idx + 1}位 : P{p.id + 1}  Score : {p.score} {(p.alive ? "" : "(脱落)")}");

        resultText.text = string.Join("\n", lines);
    }

    public void ShowStartRoll()
    {
        // 演出したいならここでアニメ/SEなど
    }
}
