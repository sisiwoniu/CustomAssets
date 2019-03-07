using UnityEngine;

public interface IBaseScrollItem {

    void Init(int Index);

    //スクロール開始する度に一回だけ呼ばれる
    void OnScrollStart();

    //座標更新
    void UpdatePos(Vector2 Pos);

    //中心オブジェクトを確定
    //スクロール止まった際のイベントです
    void ApplyCenterItem(int CenterIndex);

    //内容更新,おもに演出用、ここは現在の進行割合を常に分かっている
    //Dirが1の場合中心より下にいる、-1の場合中心より上にいる
    //「CenterDist」に「-1f~1f」の数値が入る、必ず
    void UpdateContent(int CenterIndex, float CenterDist, int Dir);
}
