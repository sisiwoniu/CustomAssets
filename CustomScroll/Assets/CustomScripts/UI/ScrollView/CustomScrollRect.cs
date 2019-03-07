using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public delegate void OnScrollCompleted();

public delegate void OnScrollWillStart();

//スクロールコンポネント本体
sealed public class CustomScrollRect : ScrollRect {

    //完了した時点のイベント
    public event OnScrollCompleted OnScrollCompletedEvent;

    //スクロール開始前のイベント
    public event OnScrollWillStart OnScrollWillStartEvent;

    private bool snap = true;

    private bool inDragging = false;

    private bool isVertical = false;

    private bool scrollCompleted = true;

    private float dragMaxSpeed;

    private float cellInterval;

    private float autoTargetPos = float.MinValue;

    private float snapVelocity = 0.3f;

    private float snapDuration = 0.2f;

    private int zeroElementIndex = 0;

    public void Init(int ZeroPosEleIndex, bool Snap, bool Vertical, float DragMaxSpeed, float CellInterval) {
        dragMaxSpeed = DragMaxSpeed * 100f;

        snap = Snap;

        cellInterval = CellInterval;

        isVertical = Vertical;

        horizontal = !isVertical;

        vertical = isVertical;

        zeroElementIndex = ZeroPosEleIndex;
    }

    public override void OnBeginDrag(PointerEventData eventData) {
        base.OnBeginDrag(eventData);

        autoTargetPos = float.MinValue;

        snapDuration = 0.2f;

        inDragging = true;

        scrollCompleted = false;

        //スクロール開始イベント
        OnScrollWillStartEvent?.Invoke();
    }

    public override void OnDrag(PointerEventData eventData) {
        base.OnDrag(eventData);

        onValueChanged.Invoke(Vector2.zero);
    }

    public override void OnEndDrag(PointerEventData eventData) {
        base.OnEndDrag(eventData);

        //スクロール最大速度を限定する、これによりすごく早い初速度によりチラつきを防止
        Vector2 vel = velocity;

        if(isVertical) {
            vel.y = Mathf.Clamp(vel.y, -dragMaxSpeed, dragMaxSpeed);
        } else {
            vel.x = Mathf.Clamp(vel.x, -dragMaxSpeed, dragMaxSpeed);
        }

        velocity = vel;

        inDragging = false;
    }

    public void JumpTo(int Index, float SnapDuration = 1f) {
        var p = (Index - zeroElementIndex) * cellInterval;

        var diff = (isVertical ? content.anchoredPosition.y : content.anchoredPosition.x) - p;

        //まったく同じの場合
        if(diff < 0.1f && diff > -0.1f) {
            return;
        }

        autoTargetPos = p;

        snapDuration = SnapDuration;

        inDragging = false;

        scrollCompleted = false;

        //スクロール開始イベント
        OnScrollWillStartEvent?.Invoke();
    }

    protected override void LateUpdate() {
        base.LateUpdate();

        if(snap && !inDragging && !scrollCompleted) {
            if(Mathf.Approximately(autoTargetPos, float.MinValue)) {
                var v = isVertical ? velocity.y : velocity.x;

                if(v <= 10f && v >= -10f) {
                    var p = Mathf.RoundToInt((isVertical ? content.anchoredPosition.y : content.anchoredPosition.x) / cellInterval) * cellInterval;

                    //二つの座標が違う場合だけ実行
                    autoTargetPos = p;
                }
            } else {
                var contentPos = content.anchoredPosition;

                var targetPos = contentPos;

                if(isVertical)
                    targetPos.y = Mathf.SmoothDamp(contentPos.y, autoTargetPos, ref snapVelocity, snapDuration, Mathf.Infinity, Time.deltaTime);
                else
                    targetPos.x = Mathf.SmoothDamp(contentPos.x, autoTargetPos, ref snapVelocity, snapDuration, Mathf.Infinity, Time.deltaTime);

                content.anchoredPosition = targetPos;

                //ここもイベント呼び出す
                onValueChanged.Invoke(Vector2.zero);

                var diff = (isVertical ? targetPos.y : targetPos.x) - autoTargetPos;

                if(diff < 0.5f && diff > -0.5f) {
                    //最後は強制に指定座標をセット
                    if(isVertical)
                        targetPos.y = autoTargetPos;
                    else
                        targetPos.x = autoTargetPos;

                    content.anchoredPosition = targetPos;

                    autoTargetPos = float.MinValue;

                    inDragging = false;

                    snapDuration = 0.2f;

                    scrollCompleted = true;

                    //スクロール終了のイベント
                    OnScrollCompletedEvent?.Invoke();
                }
            }
        }
    }

    protected override void OnDestroy() {
        onValueChanged.RemoveAllListeners();

        OnScrollCompletedEvent = null;

        OnScrollWillStartEvent = null;
    }

//#if UNITY_EDITOR

//    protected override void OnValidate() {
//        base.OnValidate();
//        Debug.Log(11);
//    }

//#endif
}