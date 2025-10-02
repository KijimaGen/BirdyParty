/**
 * @file BGMAssign.cs
 * @brief BGMのID割り当てクラス
 * @author kijima
 * @date 2025/5/15
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
//スクリプタブルオブジェクト
public class BGMAssign : ScriptableObject {
    public AudioClip[] bgmArray = null;
}
