/**
 * @file ColorChanger.cs
 * @brief �F�����R���݂ɂ������
 * @author Sum1r3
 * @date 2025/10/6
 */
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ColorChanger : SystemObject
{
    [SerializeField] Material material;

    public float colorSpeed = 1f;

    private void Update() {
        if (material == null) return;

        float t = Mathf.PingPong(Time.time * colorSpeed, 1f);
        Color rainbow = Color.HSVToRGB(t, 1f, 1f); // HSV�ł��邮��
        material.color = rainbow;
    }

    public override async UniTask Initialize() {
        await UniTask.CompletedTask;
    }
}
