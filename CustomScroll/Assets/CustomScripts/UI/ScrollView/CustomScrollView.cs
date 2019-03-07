using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public delegate void OnScrollInitCompleteEvent();

public delegate void OnStartMoveEvent();

public delegate void OnApplyCenterEvent(int CenterIndex);

//現在ScrollViewのPivotが「0.5、0.5」でないとおかしいな表示になるかもしれない
//現在X軸に無限ループの際にスペースチェック処理に表示の更新タイミングが足りないとかがあるかも
//御覧の通り動的に要素追加対応しないつもり
[RequireComponent(typeof(CustomScrollRect))]
public class CustomScrollView : UIBehaviour {

    //スクロールingのイベント
    public event OnStartMoveEvent OnMoveEvent;

    public event OnApplyCenterEvent OnApplyCenterItEvent;

    public event OnScrollInitCompleteEvent OnInitCompleteEvent;

    [SerializeField, Tooltip("スクロールビュー")]
    private RectTransform scrollView;

    [SerializeField, Tooltip("スクロール移動本体")]
    private RectTransform itemParent;

    [SerializeField, Tooltip("スクロール要素")]
    private GameObject itemPrefab;

    //要素の間隔
    [SerializeField, Tooltip("セル同士の間隔")]
    private float cellInterval = 0f;

    [SerializeField, Tooltip("作成する際にずらす座標量")]
    private float cellOffSet = 0f;

    [SerializeField, Range(5, 20), Tooltip("ドラッグにより発生する加速度の最大値")]
    private float dragMaxSpeed = 10f;

    [SerializeField, Tooltip("更新に必要の移動量、基本CellIntervalと同じでよい")]
    private float itemUpdateInterval = 0f;

    //これはtrueの場合自動で要素作成する
    [SerializeField, Tooltip("起動時自動にリスト表示の場合true")]
    private bool autoInit = false;

    //自動で作成する場合の作成カウンター
    [SerializeField, Tooltip("自動作成するリストの要素数")]
    private int autoInitCount = 0;

    [SerializeField, Tooltip("縦方向の場合true、falseだと横方向")]
    private bool vertical = true;

    [SerializeField, Tooltip("無限ループの場合true")]
    private bool loop = false;

    [SerializeField]
    private bool snap = true;

#if UNITY_EDITOR
    [SerializeField]
    private int debugJumpToIndex = 0;
#endif

    private CustomScrollRect scrollR;

    private Rect scrollVR;

    private int centerIndex = 0;

    private float itemParentPos;

    private float limitPos = 0f;

    private bool inited = false;

    //スクロール要素
    private LinkedList<BaseScrollItem> scrollLt = new LinkedList<BaseScrollItem>();

    public void Init(int CreateCount, int JumpToIndex) {
        //2度と初期化させない
        if(inited)
            return;

        if(itemPrefab == null) {
            Debug.LogWarning("作成するスクロールプレハブが設定されていないため、スクロールビュー初期化失敗します");
            return;
        }

        scrollR = GetComponent<CustomScrollRect>();

        scrollVR = scrollView.rect;

        Vector3 pos = Vector3.zero;

        var downPos = (vertical ? itemParent.rect.size.y : itemParent.rect.size.x) * 0.5f - cellOffSet;

        if(vertical)
            pos.y = downPos;
        else
            pos.x = downPos;

        //丁度0に配置されたオブジェクト
        int zeroPosIndex = -1;

        for(int i = 0; i < CreateCount; i++) {
            var item = Instantiate(itemPrefab, itemParent);

            var baseItem = item.GetComponent<BaseScrollItem>();

            baseItem.Init(i);

            baseItem.GetComponentInChildren<UnityEngine.UI.Text>().text = i.ToString();

            //要素の座標設定
            baseItem.UpdatePos(pos);

            item.SetActive(false);

            //中心のインデックスを取得
            if(zeroPosIndex == -1) {

                var data = ApplyNormalizeCenterDist(item.transform.position).x;

                if(data < 0.05f && data > -0.05f)
                    zeroPosIndex = i;
            }

            if(vertical)
                pos.y -= cellInterval;
            else
                pos.x -= cellInterval;

            //中心オブジェクト確定イベント登録
            OnApplyCenterItEvent += baseItem.ApplyCenterItem;

            scrollLt.AddLast(baseItem);
        }

        centerIndex = zeroPosIndex;

        limitPos = CreateCount * cellInterval;

        scrollR.Init(zeroPosIndex, snap, vertical, dragMaxSpeed, cellInterval);

        //スクロールイベント
        scrollR.OnScrollCompletedEvent += OnScrollCompletedEvent;

        scrollR.OnScrollWillStartEvent += OnScrollWillStartEvent;

        scrollR.onValueChanged.AddListener(_ => { OnScrollEvent(); });

        scrollR.JumpTo(JumpToIndex, 0f);

        //初期位置がぴったり合う場合のため
        OnScrollAnimEvent();

        OnInitCompleteEvent?.Invoke();

        inited = true;
    }

    protected override void Start() {
        if(autoInit) {
            Init(autoInitCount, 0);
        }

        itemParentPos = vertical ? itemParent.anchoredPosition.y : itemParent.anchoredPosition.x;
    }

    protected override void OnDestroy() {
        scrollLt.Clear();

        if(scrollR != null)
            scrollR.onValueChanged.RemoveAllListeners();

        OnMoveEvent = null;

        OnApplyCenterItEvent = null;

        OnInitCompleteEvent = null;
    }

    //スクロール開始イベント
    //実際のスクロールと関係なく、ドラッグ発生したらイベントが呼ばれる
    protected virtual void OnScrollWillStartEvent() {
        Debug.Log("スクロールが発生した");
    }

    //スクロール完了イベント
    protected virtual void OnScrollCompletedEvent() {
        //スクロール終了時で最大値に超えているかどうかをチェック
        var newP = vertical ? itemParent.anchoredPosition.y : itemParent.anchoredPosition.x;

        //リミット超えたらリセット
        if(newP > limitPos || newP < -limitPos) {
            newP = newP % limitPos;

            var pos = Vector2.zero;

            if(vertical)
                pos.y = newP;
            else
                pos.x = newP;

            itemParent.anchoredPosition = pos;

            //リセット後ですぐにリフレッシュ
            OnScrollEvent();
        }


        //スクロール終了したら中心オブジェクトへ更新する
        UpdateCenterItemEvent();
        //Debug.Log("スクロール終了イベント");
    }

    //スクロールビューの中心から離れた距離の正規化した数値
    //xは距離、ｙは方向
    protected Vector2 ApplyNormalizeCenterDist(Vector2 CheckPos) {
        Vector2 result;

        //UV空間の結果なので、元の原点が「0，0」で、これを「0，0.5」の原点に変換
        var data = Rect.PointToNormalized(scrollView.rect, CheckPos);

        //ここの結果が「-0.5f ~ 0.5f」
        var baseValue = (vertical ? data.y : data.x) - 0.5f;//0.5f - Rect.PointToNormalized(scrollView.rect, CheckPos).y;

        //「*5」は適当に掛けた係数
        result.x = Mathf.Clamp(baseValue * 5f, -1f, 1f);//Mathf.Clamp(1f - (Mathf.Abs(baseValue) * 5f), -1f, 1f);

        //[baseValue < 0f] == 下にいる
        //方向を渡す必要がないかもしれないが、とりあえず取っておく
        result.y = Mathf.Sign(baseValue);//baseValue > 0f ? 1f : 1f;

        return result;
    }

    //アイテムをループさせる
    private void LoopItem(bool UpDir, int Num) {
        for(int i = 0; i < Num; i++) {
            if(UpDir) {
                var lastP = scrollLt.Last.Value.NowPos;

                var item = scrollLt.First.Value;

                scrollLt.RemoveFirst();

                if(vertical) {
                    lastP.y -= cellInterval;
                } else {
                    lastP.x -= cellInterval;
                }

                item.UpdatePos(lastP);

                item.transform.SetAsLastSibling();

                scrollLt.AddLast(item);

                centerIndex = centerIndex + 1 > scrollLt.Count - 1 ? 0 : centerIndex + 1;
            } else {
                var fristP = scrollLt.First.Value.NowPos;

                var item = scrollLt.Last.Value;

                scrollLt.RemoveLast();

                if(vertical) {
                    fristP.y += cellInterval;
                } else {
                    fristP.x += cellInterval;
                }

                item.UpdatePos(fristP);

                scrollLt.AddFirst(item);

                item.transform.SetAsFirstSibling();

                centerIndex = centerIndex - 1 < 0 ? scrollLt.Count - 1 : centerIndex - 1;
            }
        }
    }

    //スクロール発生した（している）場合に呼ばれるイベント
    private void OnScrollEvent() {
        var newP = vertical ? itemParent.anchoredPosition.y : itemParent.anchoredPosition.x;

        if(loop) {
            var diff = newP - itemParentPos;

            int num = Mathf.RoundToInt(newP / cellInterval);

            LoopItem(diff > 0f, Mathf.Abs(Mathf.RoundToInt(diff / cellInterval)));

            itemParentPos = num * cellInterval;
        }

        //移動イベント
        OnMoveEvent?.Invoke();

        OnScrollAnimEvent();
    }

    //スクロール中の要素動画
    private void OnScrollAnimEvent() {
        //具体的なアニメーションは要素に任せる
        foreach(var item in scrollLt) {
            if(scrollVR.Contains(item.WorldPos)) {
                if(!item.gameObject.activeSelf)
                    item.gameObject.SetActive(true);
                
                //現在の表示中の要素が中心から離れた距離
                var ys = ApplyNormalizeCenterDist(item.WorldPos);

                item.UpdateContent(centerIndex, ys.x, (int)ys.y);

                continue;
            }

            if(item.gameObject.activeSelf)
                item.gameObject.SetActive(false);
        }
    }

    //スクロール終了したら中心要素入れ替わったらイベントを開始する
    private void UpdateCenterItemEvent() {
        OnApplyCenterItEvent?.Invoke(centerIndex);

        //Debug.Log("center index == " + centerIndex);
    }

#if UNITY_EDITOR

    private void LateUpdate() {
        if(Input.GetKeyDown(KeyCode.D)) {
            scrollR.JumpTo(Mathf.Clamp(debugJumpToIndex, 0, scrollLt.Count - 1));
        }
    }

#endif
}