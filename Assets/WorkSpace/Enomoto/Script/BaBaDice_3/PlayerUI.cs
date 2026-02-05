using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text lastRollText;
    [SerializeField] private Image eliminatedMask; 

    public void Init(int playerId)
    {
        if (nameText) nameText.text = $"P{playerId + 1}";
        SetScore(0);
        SetLastRoll(0);
        SetEliminated(false);
    }

    public void SetScore(int score)
    {
        if (scoreText) scoreText.text = $"{score}";
    }

    public void SetLastRoll(int value)
    {
        if (lastRollText) lastRollText.text = $"Roll: {(value > 0 ? value.ToString() : "-")}";
    }

    public void SetEliminated(bool on)
    {
        if (eliminatedMask) eliminatedMask.gameObject.SetActive(on);
    }
}
