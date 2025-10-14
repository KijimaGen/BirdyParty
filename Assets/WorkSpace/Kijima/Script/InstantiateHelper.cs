using UnityEngine;

public static class InstantiateHelper {
    public static GameObject Instantiate(this GameObject prefab, Vector3 position, Vector3 eulerAngles) {
        return Object.Instantiate(prefab, position, Quaternion.Euler(eulerAngles));
    }

    public static T Instantiate<T>(this T prefab, Vector3 position, Vector3 eulerAngles) where T : Component {
        return Object.Instantiate(prefab, position, Quaternion.Euler(eulerAngles));
    }

    /// <summary>
    /// ワールド座標で生成してから親に設定する
    /// </summary>
    public static GameObject SpawnChildWorld(this Transform parent, GameObject prefab, Vector3 worldPosition, Vector3 worldEuler) {
        // ワールド座標・回転で生成
        GameObject obj = Object.Instantiate(prefab, worldPosition, Quaternion.Euler(worldEuler));
        // 子に設定（位置は維持する）
        obj.transform.SetParent(parent, true); // ← trueでワールド座標を保持
        return obj;
    }

    /// <summary>
    /// Component版（Prefabがスクリプト付きなど）
    /// </summary>
    public static T SpawnChildWorld<T>(this Transform parent, T prefab, Vector3 worldPosition, Vector3 worldEuler) where T : Component {
        T obj = Object.Instantiate(prefab, worldPosition, Quaternion.Euler(worldEuler));
        obj.transform.SetParent(parent, true);
        return obj;
    }

    /// <summary>
    /// ローカル座標で生成してから親に設定する
    /// </summary>
    /// <param Name="parent"></param>
    /// <param Name="prefab"></param>
    /// <param Name="localPosition"></param>
    /// <param Name="localEuler"></param>
    /// <returns></returns>
    public static GameObject SpawnChildLocal(this Transform parent,GameObject prefab, Vector3 localPosition, Vector3 localEuler) {
        //一旦プレファブを生成(位置・回転は親のローカル基準で設定)
        GameObject obj = Object.Instantiate(prefab, parent);

        //ローカル座標・回転を設定
        obj.transform.localPosition = localPosition;
        obj.transform.localRotation = Quaternion.Euler(localEuler);

        return obj;
    }
}
