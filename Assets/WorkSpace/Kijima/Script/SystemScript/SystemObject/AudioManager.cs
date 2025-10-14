/**
 * @file SoundManager.cs
 * @brief �T�E���h�̊Ǘ��N���X
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

    //BGM�Đ��p�R���|�[�l���g
    [SerializeField]
    private AudioSource _bgmAudioSource = null;
    //BGM�̃��X�g
    [SerializeField]
    private BGMAssign _bgmAssign = null;

    //SE�Đ��p�R���|�[�l���g
    [SerializeField]
    private AudioSource[] _seAudioSource = null;
    //SE�̃��X�g
    [SerializeField]
    private SEAssign _seAssign = null;
    public override async UniTask Initialize() {
        instance = this;
        await UniTask.CompletedTask;
        _token = this.GetCancellationTokenOnDestroy();
    }

    //���f�p�g�[�N��
    private CancellationToken _token;

    /// <summary>
    /// BGM�Đ�
    /// </summary>
    /// <param Name="bgmID"></param>
    public void PlayBGM(int bgmID) {
        if (!IsEnableIndex(_bgmAssign.bgmArray, bgmID)) return;
        _bgmAudioSource.clip = _bgmAssign.bgmArray[bgmID];
        _bgmAudioSource.Play();
    }

    /// <summary>
    /// BGM��~
    /// </summary>
    public void StopBGM() {
        _bgmAudioSource.Stop();
    }

    public async UniTask PlaySE(int seID) {
        if (!IsEnableIndex(_seAssign.seArray, seID)) return;
        //�Đ����łȂ��I�[�f�B�I�\�[�X��T���Ă���ōĐ�
        for (int i = 0, max = _seAudioSource.Length; i < max; i++) {
            AudioSource audioSource = _seAudioSource[i];
            if (audioSource == null ||
                audioSource.isPlaying) continue;
            //�Đ����łȂ��I�[�f�B�I�\�[�X�����������̂ōĐ�
            audioSource.clip = _seAssign.seArray[seID];
            audioSource.Play();
            //SE�̏I���҂�
            while (audioSource.isPlaying) {
                await UniTask.DelayFrame(1, PlayerLoopTiming.Update, _token);
            }
            return;
        }
    }
}
