/**
 * @file FadeManager.cs
 * @brief �t�F�[�h�̊Ǘ��N���X
 * @author kazusa
 * @date 2025/5/15
 */

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : SystemObject {
    //�t�F�[�h�p���摜
    [SerializeField]
    private Image _fadeImage;
    public static FadeManager instance { get; private set; } = null;

    //�ǂ̂��炢�̎��Ԃ������ăt�F�[�h�C���t�F�[�h�A�E�g���邩
    private const float _DEFAULT_FADE_DURATION = 0.5f;

    /// <summary>
    /// ������
    /// </summary>
    /// <returns></returns>
    public override async UniTask Initialize() {
        instance = this;
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// �t�F�[�h�A�E�g�A�Â�����
    /// </summary>
    /// <param Name="duration"></param>
    /// <returns></returns>
    public async UniTask FadeOut(float duration = _DEFAULT_FADE_DURATION) {
        await FadeTargetAlpha(1.0f, duration);
    }

    /// <summary>
    /// �t�F�[�h�C���A�ǂ����H���邭�Ȃ������낤�H
    /// </summary>
    /// <param Name="duration"></param>
    /// <returns></returns>
    public async UniTask FadeIn(float duration = _DEFAULT_FADE_DURATION) {
        await FadeTargetAlpha(0.0f, duration);
    }

    /// <summary>
    /// �t�F�[�h�摜���w��̕s�����x�ɕω�������
    /// </summary>
    /// <param Name="targetAlpha"></param>
    /// <param Name="duration"></param>
    /// <returns></returns>
    private async UniTask FadeTargetAlpha(float targetAlpha, float duration) {
        float elapsedTime = 0.0f;//�o�ߎ���
        float startAlpha = _fadeImage.color.a;  //�J�n�����x
        Color targetColor = _fadeImage.color;
        while (elapsedTime < duration) {
            elapsedTime += Time.deltaTime;
            //�ۊǂ����s�����x���t�F�[�h�摜�ɐݒ�
            float t = elapsedTime / duration;

            targetColor.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            _fadeImage.color = targetColor;
            //1�t���[���҂�
            await UniTask.Delay(1);
        }
        targetColor.a = targetAlpha;
        _fadeImage.color = targetColor;
    }
}
