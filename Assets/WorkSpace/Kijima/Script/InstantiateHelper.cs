using UnityEngine;

public static class InstantiateHelper {
    public static GameObject Instantiate(this GameObject prefab, Vector3 position, Vector3 eulerAngles) {
        return Object.Instantiate(prefab, position, Quaternion.Euler(eulerAngles));
    }

    public static T Instantiate<T>(this T prefab, Vector3 position, Vector3 eulerAngles) where T : Component {
        return Object.Instantiate(prefab, position, Quaternion.Euler(eulerAngles));
    }

    /// <summary>
    /// ���[���h���W�Ő������Ă���e�ɐݒ肷��
    /// </summary>
    public static GameObject SpawnChildWorld(this Transform parent, GameObject prefab, Vector3 worldPosition, Vector3 worldEuler) {
        // ���[���h���W�E��]�Ő���
        GameObject obj = Object.Instantiate(prefab, worldPosition, Quaternion.Euler(worldEuler));
        // �q�ɐݒ�i�ʒu�͈ێ�����j
        obj.transform.SetParent(parent, true); // �� true�Ń��[���h���W��ێ�
        return obj;
    }

    /// <summary>
    /// Component�ŁiPrefab���X�N���v�g�t���Ȃǁj
    /// </summary>
    public static T SpawnChildWorld<T>(this Transform parent, T prefab, Vector3 worldPosition, Vector3 worldEuler) where T : Component {
        T obj = Object.Instantiate(prefab, worldPosition, Quaternion.Euler(worldEuler));
        obj.transform.SetParent(parent, true);
        return obj;
    }

    /// <summary>
    /// ���[�J�����W�Ő������Ă���e�ɐݒ肷��
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="prefab"></param>
    /// <param name="localPosition"></param>
    /// <param name="localEuler"></param>
    /// <returns></returns>
    public static GameObject SpawnChildLocal(this Transform parent,GameObject prefab, Vector3 localPosition, Vector3 localEuler) {
        //��U�v���t�@�u�𐶐�(�ʒu�E��]�͐e�̃��[�J����Őݒ�)
        GameObject obj = Object.Instantiate(prefab, parent);

        //���[�J�����W�E��]��ݒ�
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.Euler(localEuler);

        return obj;
    }
}
