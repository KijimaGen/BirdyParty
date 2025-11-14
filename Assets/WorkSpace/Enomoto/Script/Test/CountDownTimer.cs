using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CountDownTimer : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float countTime;

    // テスト用に一時的に使用
    void Update()
    {
        StartCountDown();
    }

    // カウントダウン開始
    private void StartCountDown()
    {
        float timer = countTime - Time.time;
        int seconds = Mathf.FloorToInt(timer % 60);
        fillImage.fillAmount = Mathf.InverseLerp(0, countTime, timer);
        timerText.text = seconds.ToString("00");
    }
}
