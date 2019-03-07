using UnityEngine;

//ここで必要の情報を取得する
[RequireComponent(typeof(RectTransform))]
public class BaseScrollItem : MonoBehaviour, IBaseScrollItem {

    private RectTransform rectTransform;

    public Vector2 NowPos {
        get;
        protected set;
    }

    public int Index {
        get;
        protected set;
    }

    //同時に大量(1000個ぐらい)アクセスするとコストが無視できない
    //結局スクロールが開始したら、ここが集中アクセスされる
    public Vector2 WorldPos {
        get {
            return rectTransform.position;
        }
    }

    public virtual void Init(int Index) {
        this.Index = Index;

        if(rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }

    public virtual void OnScrollStart() { }

    public virtual void ApplyCenterItem(int CenterIndex) {}

    public virtual void UpdateContent(int CenterIndex, float CenterDist, int Dir) {}

    public void UpdatePos(Vector2 Pos) {
        NowPos = Pos;

        rectTransform.anchoredPosition = Pos;
    }
}
