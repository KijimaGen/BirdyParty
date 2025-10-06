/**
 * @file ColorChanger.cs
 * @brief êFÇé©óRé©ç›Ç…Ç∑ÇÈÇ‡ÇÃ
 * @author Sum1r3
 * @date 2025/10/6
 */
using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    [SerializeField] Material material;

    public float colorSpeed = 1f;

    private void Update() {
        if (material == null) return;

        float t = Mathf.PingPong(Time.time * colorSpeed, 1f);
        Color rainbow = Color.HSVToRGB(t, 1f, 1f); // HSVÇ≈ÇÆÇÈÇÆÇÈ
        material.color = rainbow;
    }
}
