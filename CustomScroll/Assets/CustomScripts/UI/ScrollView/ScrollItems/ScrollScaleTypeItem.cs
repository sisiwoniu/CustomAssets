using UnityEngine;

public class ScrollScaleTypeItem : BaseScrollItem {

    public override void UpdateContent(int CenterIndex, float CenterDist, int Dir) {
        var scale = Vector3.one * Mathf.Clamp(1 - Mathf.Abs(CenterDist), 0.3f, 1f);

        //中心の場合方向を無視
        if(CenterIndex != Index)
            scale.y *= Dir;

        transform.localScale = scale;

        //Debug.Log($"index == {Index}, CenterDist == {CenterDist}");
    }

    public override void ApplyCenterItem(int CenterIndex) {
        if(CenterIndex == Index)
            Debug.Log("CenterIndex == " + CenterIndex);
    }
}
