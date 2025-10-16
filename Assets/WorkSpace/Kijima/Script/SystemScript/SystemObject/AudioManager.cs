/**
 * @file SoundManager.cs
 * @brief サウンドの管理クラス
 * @author SuM1R3
 * @date 2025/5/15
 */
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

using static CommonModule;

public class AudioManager : SystemObject {
    public static AudioManager instance { get; private set; } = null;

    //BGM再生用コンポーネント
    [SerializeField]
    private AudioSource _bgmAudioSource = null;
    //BGMのリスト
    [SerializeField]
    private BGMAssign _bgmAssign = null;

    //SE再生用コンポーネント
    [SerializeField]
    private AudioSource[] _seAudioSource = null;
    //SEのリスト
    [SerializeField]
    private SEAssign _seAssign = null;
    public override async UniTask Initialize() {
        instance = this;
        await UniTask.CompletedTask;
        _token = this.GetCancellationTokenOnDestroy();
    }

    //中断用トークン
    private CancellationToken _token;

    /// <summary>
    /// BGM再生
    /// </summary>
    /// <param Name="bgmID"></param>
    public void PlayBGM(int bgmID) {
        if (!IsEnableIndex(_bgmAssign.bgmArray, bgmID)) return;
        _bgmAudioSource.clip = _bgmAssign.bgmArray[bgmID];
        _bgmAudioSource.Play();
    }

    /// <summary>
    /// BGM停止
    /// </summary>
    public void StopBGM() {
        _bgmAudioSource.Stop();
    }

    public async UniTask PlaySE(int seID) {
        if (!IsEnableIndex(_seAssign.seArray, seID)) return;
        //再生中でないオーディオソースを探してそれで再生
        for (int i = 0, max = _seAudioSource.Length; i < max; i++) {
            AudioSource audioSource = _seAudioSource[i];
            if (audioSource == null ||
                audioSource.isPlaying) continue;
            //再生中でないオーディオソースが見つかったので再生
            audioSource.clip = _seAssign.seArray[seID];
            audioSource.Play();
            //SEの終了待ち
            while (audioSource.isPlaying) {
                await UniTask.DelayFrame(1, PlayerLoopTiming.Update, _token);
            }
            return;
        }
    }
}
