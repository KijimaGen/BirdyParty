using UnityEngine;

public class DiceFaceDetector : MonoBehaviour
{
    private int? downFaceValue = null;
    private int? lastDownFaceValue = null;
    private float lastChangeTime = 0f;

    // 1<->6, 2<->5, 3<->4
    private static readonly System.Collections.Generic.Dictionary<int, int> Opposite = new()
    {
        {1,6},{6,1},
        {2,5},{5,2},
        {3,4},{4,3},
    };

    public void NotifyFaceContact(int faceValue)
    {
        if (downFaceValue == null || downFaceValue.Value != faceValue)
        {
            lastDownFaceValue = downFaceValue;
            downFaceValue = faceValue;
            lastChangeTime = Time.time;
        }
    }

    public void ClearContact()
    {
        downFaceValue = null;
        lastDownFaceValue = null;
        lastChangeTime = Time.time;
    }

    public bool HasContact => downFaceValue != null;

    /// <summary>ìØÇ∂ñ Ç™ stableSeconds à»è„ïœÇÌÇ¡ÇƒÇ¢Ç»Ç¢Ç©</summary>
    public bool IsStable(float stableSeconds)
    {
        if (downFaceValue == null) return false;
        return (Time.time - lastChangeTime) >= stableSeconds;
    }

    public int GetUpFaceValueOrFallback(int fallback)
    {
        if (downFaceValue == null) return fallback;
        int down = downFaceValue.Value;
        return Opposite.TryGetValue(down, out int up) ? up : fallback;
    }

    public bool TryGetUpFaceIfStable(float stableSeconds, out int upFace)
    {
        upFace = 0;
        if (!HasContact) return false;
        if (!IsStable(stableSeconds)) return false;

        upFace = GetUpFaceValueOrFallback(0); // fallbackÇÕ0
        return upFace >= 1 && upFace <= 6;
    }
}
