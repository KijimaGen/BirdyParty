using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UITextMover : MonoBehaviour
{
    [Header("�Q�Ɛݒ�")]
    public RectTransform textTransform;    // TextMeshProUGUI �� RectTransform
    public TextMeshProUGUI textMeshPro;    // TextMeshProUGUI �R���|�[�l���g
    public RectTransform maskImage;        // ���ɔz�u���ꂽ Image �� Mask �� RectTransform

    [Header("�e�L�X�g�ݒ�")]
    [TextArea(3, 10)]
    public string infoText;                // ���sOK
    public float spaceBetweenLines = 8f;   // ���s�Ԃ̃X�y�[�X
    public float scrollSpeed = 100f;       // �X�N���[�����x
    public float fadeDistance = 60f;       // �t�F�[�h�A�E�g�J�n����
    public float fadeSpeed = 2f;           // �t�F�[�h���x
    public float restartDelay = 0.5f;      // �ďo���܂ł̑҂�����

    private float screenRightX;
    private float maskLeftX;
    private Vector3 startPos;
    private bool resetting = false;
    private float[] charAlpha;

    void Start()
    {
        if (textTransform == null || textMeshPro == null || maskImage == null)
        {
            Debug.LogError("�Q�Ƃ�����܂���BtextTransform, textMeshPro, maskImage �����蓖�ĂĂ��������B");
            enabled = false;
            return;
        }

        // ���s�����Ԋu�ŃX�y�[�X��������1�s��
        string[] lines = infoText.Split('\n');
        textMeshPro.text = string.Join(new string(' ', Mathf.RoundToInt(spaceBetweenLines)), lines);

        // ��ʉE�[�� mask ���[�����[���h���W�Ŏ擾
        screenRightX = Screen.width + textMeshPro.preferredWidth / 2f;
        maskLeftX = maskImage.position.x - maskImage.rect.width * 0.5f;

        textMeshPro.ForceMeshUpdate();
        charAlpha = new float[textMeshPro.textInfo.characterCount];
        for (int i = 0; i < charAlpha.Length; i++) charAlpha[i] = 1f;

        // �e�L�X�g����ʉE�[����X�^�[�g
        Vector3 worldPos = textTransform.position;
        worldPos.x = screenRightX + 200.0f;
        textTransform.position = worldPos;
    }

    void Update()
    {
        if (resetting) return;

        // ���֊��炩�Ɉړ�
        textTransform.position += Vector3.left * scrollSpeed * Time.deltaTime;

        // �t�F�[�h���폜����
        UpdateFade();

        // ���S�Ƀ}�X�N��ʉ߂����烊�Z�b�g
        float textRight = textTransform.position.x + textMeshPro.preferredWidth * 0.5f;
        if (textRight < maskLeftX - 50f)
        {
            StartCoroutine(RestartAfterDelay());
        }
    }

    private void UpdateFade()
    {
        textMeshPro.ForceMeshUpdate();
        var textInfo = textMeshPro.textInfo;

        if (charAlpha.Length != textInfo.characterCount)
        {
            charAlpha = new float[textInfo.characterCount];
            for (int i = 0; i < charAlpha.Length; i++) charAlpha[i] = 1f;
        }

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int vi = textInfo.characterInfo[i].vertexIndex;
            int mi = textInfo.characterInfo[i].materialReferenceIndex;
            var colors = textInfo.meshInfo[mi].colors32;

            // �e�����̃��[���h�ʒu�����߂�
            float charRightX = textTransform.TransformPoint(textInfo.characterInfo[i].topRight).x;

            // �}�X�N�E�[���� fadeDistance ���ɓ�������t�F�[�h�A�E�g
            if (charRightX <= maskLeftX + fadeDistance && charRightX > maskLeftX)
                charAlpha[i] = Mathf.MoveTowards(charAlpha[i], 0f, fadeSpeed * Time.deltaTime);
            else if (charRightX > maskLeftX + fadeDistance)
                charAlpha[i] = Mathf.MoveTowards(charAlpha[i], 1f, fadeSpeed * Time.deltaTime);

            byte a = (byte) (Mathf.Clamp01(charAlpha[i]) * 255);
            for (int j = 0; j < 4; j++) colors[vi + j].a = a;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.colors32 = meshInfo.colors32;
            textMeshPro.UpdateGeometry(meshInfo.mesh, i);
        }
    }

    private System.Collections.IEnumerator RestartAfterDelay()
    {
        resetting = true;
        yield return new WaitForSeconds(restartDelay);

        // �e�L�X�g���Ĕz�u�i��ʉE�[�֖߂��j
        Vector3 pos = textTransform.position;
        pos.x = screenRightX + 200.0f;
        textTransform.position = pos;

        // �����x�����Z�b�g
        for (int i = 0; i < charAlpha.Length; i++) charAlpha[i] = 1f;

        resetting = false;
    }
}