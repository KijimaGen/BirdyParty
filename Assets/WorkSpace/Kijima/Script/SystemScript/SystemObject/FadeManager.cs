/**
 * @file FadeManager.cs
 * @brief フェードの管理クラス
 * @author kazusa
 * @date 2025/5/15
 */

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : SystemObject {
    //フェード用黒画像
    [SerializeField]
    private Image _fadeImage;
    public static FadeManager instance { get; private set; } = null;

    //どのくらいの時間をかけてフェードインフェードアウトするか
    private const float _DEFAULT_FADE_DURATION = 0.5f;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <returns></returns>
    public override async UniTask Initialize() {
        instance = this;
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// フェードアウト、暗くする
    /// </summary>
    /// <param Name="duration"></param>
    /// <returns></returns>
    public async UniTask FadeOut(float duration = _DEFAULT_FADE_DURATION) {
        await FadeTargetAlpha(1.0f, duration);
    }

    /// <summary>
    /// フェードイン、どうだ？明るくなっただろう？
    /// </summary>
    /// <param Name="duration"></param>
    /// <returns></returns>
    public async UniTask FadeIn(float duration = _DEFAULT_FADE_DURATION) {
        await FadeTargetAlpha(0.0f, duration);
    }

    /// <summary>
    /// フェード画像を指定の不透明度に変化させる
    /// </summary>
    /// <param Name="targetAlpha"></param>
    /// <param Name="duration"></param>
    /// <returns></returns>
    private async UniTask FadeTargetAlpha(float targetAlpha, float duration) {
        float elapsedTime = 0.0f;//経過時間
        float startAlpha = _fadeImage.color.a;  //開始透明度
        Color targetColor = _fadeImage.color;
        while (elapsedTime < duration) {
            elapsedTime += Time.deltaTime;
            //保管した不透明度をフェード画像に設定
            float t = elapsedTime / duration;

            targetColor.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            _fadeImage.color = targetColor;
            //1フレーム待ち
            await UniTask.Delay(1);
        }
        targetColor.a = targetAlpha;
        _fadeImage.color = targetColor;
    }
}
