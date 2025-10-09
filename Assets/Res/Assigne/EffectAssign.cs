/**
 * @file EffectAssign.cs
 * @brief エフェクトのID割り当てクラス
 * @author Sum1r3
 * @date 2025/10/8
 */

using UnityEngine;

[CreateAssetMenu]
//スクリプタブルオブジェクト
public class EffectAssign : ScriptableObject {
    public ParticleSystem[] effectArray = null;
}
