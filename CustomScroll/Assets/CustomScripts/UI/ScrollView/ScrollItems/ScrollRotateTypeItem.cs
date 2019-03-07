using UnityEngine;

//回転タイプのスクロールアイテム
public class ScrollRotateTypeItem : BaseScrollItem {

    //回転角度限度値
    [SerializeField, Range(0f, 360f)]
    private float Angle = 40f;

    //上下の角度を反転させるならtrue
    [SerializeField]
    private bool Inverted = false;

    [SerializeField]
    private RotateAxis Axis = RotateAxis.X;

    private Vector2 useAngle = Vector2.zero;

    private Vector3 rotateAxis;

    public override void Init(int Index) {
        base.Init(Index);

        //遷移可能の値の範囲
        useAngle.x = -Angle;

        useAngle.y = Angle;

        switch(Axis) {
            case RotateAxis.Y:
                rotateAxis = Vector3.up;
                break;
            case RotateAxis.Z:
                rotateAxis = Vector3.forward;
                break;
            case RotateAxis.X:
            default:
                rotateAxis = Vector3.right;
                break;
        }
    }

    public override void OnScrollStart() {
        Debug.Log("スクロール開始した");
    }

    public override void UpdateContent(int CenterIndex, float CenterDist, int Dir) {

        //閾値を切る
        if(CenterDist < 0.01f && CenterDist > -0.01f)
            CenterDist = 0f;

        //ここは単純に座標空間を整えるだけ、ここに来る値が「-1~1」になっていて、これを「0~1」に変換する
        //（CenterDist * 2f）は単純に係数を強くするだけ
        var t = Mathf.Clamp((CenterDist * 2f + 1f) * 0.5f, 0f, 1f);

        var angle = Mathf.Lerp(useAngle.x, useAngle.y, t) * (Inverted ? -1f : 1f);

        //Debug.Log($"index == {Index}, angle == {angle}, dir == {Dir}");

        transform.localRotation = Quaternion.AngleAxis(angle, rotateAxis);
    }

    [System.Serializable]
    enum RotateAxis {
        Z,
        X,
        Y
    }
}
