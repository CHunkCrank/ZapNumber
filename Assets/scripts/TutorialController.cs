using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TutorialController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BoardPresenter presenter;
    [SerializeField] private BoardLogic board;

    [Header("Overlay UI")]
    [SerializeField] private RectTransform overlayRoot;     // 自分自身でもOK
    [SerializeField] private CanvasGroup overlayGroup;      // OverlayのCanvasGroup
    [SerializeField] private TMP_Text hintText;             // HintText (TMP)
    [SerializeField] private RectTransform tapRing;         // 丸ImageのRectTransform（TMPじゃない）
    [SerializeField] private TMP_Text movesText;            // Moves Text (TMP)（任意）

    [Header("Prefs")]
    [SerializeField] private string tutorialKey = "tutorial_done_v1";

    [Header("Debug Keys (Editor only recommended)")]
    [SerializeField] private bool enableDebugKeys = true;


    CanvasGroup tapRingGroup;
    Animator tapRingAnimator;


    void ShowTapRing()
    {
        if (!tapRing) return;

        tapRing.gameObject.SetActive(true);

        // ★ここが原因対策：透明固定を解除
        if (tapRingGroup) tapRingGroup.alpha = 1f;

        // Animatorあるなら頭から再生（なくてもOK）
        if (tapRingAnimator)
        {
            tapRingAnimator.Rebind();
            tapRingAnimator.Update(0f);
            tapRingAnimator.Play("TapHint_Pulse", 0, 0f);
        }
    }

    void HideTapRing()
    {
        if (!tapRing) return;
        if (tapRingGroup) tapRingGroup.alpha = 0f;
        tapRing.gameObject.SetActive(false);
    }


    public bool IsRunning { get; private set; }

    // tutorialの「今タップして良い座標」
    private readonly HashSet<Vector2Int> allowed = new HashSet<Vector2Int>();

    // CellView 探索用
    private readonly Dictionary<Vector2Int, CellView> viewByCoord = new Dictionary<Vector2Int, CellView>();

    // コルーチン待ち用
    private int acceptedClicks = 0;
    private CellView lastAccepted;

    private Coroutine ringCycleCo;

    private void Awake()
    {
        if (overlayGroup == null) overlayGroup = GetComponentInChildren<CanvasGroup>(true);
        if (overlayRoot == null) overlayRoot = GetComponent<RectTransform>();

        if (overlayGroup != null)
        {
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;
        }

        if (tapRing != null)
        {
            tapRingGroup = tapRing.GetComponent<CanvasGroup>();
            tapRingAnimator = tapRing.GetComponent<Animator>();
        }
    }



    /// <summary>
    /// BoardPresenter.Start() から呼ぶ
    /// </summary>
    public void BeginIfNeeded()
    {
        if (PlayerPrefs.GetInt(tutorialKey, 0) == 1)
        {
            HideOverlay();
            return;
        }

        StartTutorial();
    }

    public void ResetTutorialFlag()
    {
        PlayerPrefs.DeleteKey(tutorialKey);
        PlayerPrefs.Save();
        Debug.Log($"[Tutorial] Deleted PlayerPrefs key: {tutorialKey}");
    }

    public void ForceStartTutorial()
    {
        ResetTutorialFlag();
        StartTutorial();
    }

    private void StartTutorial()
    {
        if (IsRunning) return;

        CacheViews();
        ShowOverlay();

        IsRunning = true;
        StartCoroutine(RunTutorial());
    }

    private void CacheViews()
    {
        viewByCoord.Clear();
        if (presenter == null) presenter = FindFirstObjectByType<BoardPresenter>();
        if (board == null) board = FindFirstObjectByType<BoardLogic>();

        // GridRoot配下に生成されたCellViewを拾う
        var views = FindObjectsByType<CellView>(FindObjectsSortMode.None);
        foreach (var v in views)
        {
            if (v == null) continue;
            viewByCoord[v.Coord] = v;
        }
    }

    /// <summary>
    /// BoardPresenter の OnCellClicked の先頭で呼んで、
    /// タップしていいセル以外を「無視」する
    /// 戻り値 true = ここで消費（BoardPresenterは処理しない）
    /// </summary>
    public bool InterceptCellClick(CellView v)
    {
        if (!IsRunning) return false;

        if (v == null) return true;
        if (v.IsOutZone) return true;

        // 許可されたセル以外は無視
        if (allowed.Count > 0 && !allowed.Contains(v.Coord))
            return true;

        acceptedClicks++;
        lastAccepted = v;
        return false; // 許可セルはBoardPresenter側の通常処理に流す
    }

    private IEnumerator RunTutorial()
    {
        // 生成直後の1フレーム待ち（Grid構築完了を安定させる）
        yield return null;

        // --- 3回やる（「1個→隣」×3）---
        for (int i = 0; i < 3; i++)
        {
            // ① 最初に押させるセルを1つ選ぶ（盤面の一番下から適当に）
            var first = FindAnyPlayableCell();
            if (first == null)
            {
                Debug.LogWarning("[Tutorial] No playable cell found.");
                break;
            }

            SetHint("まず、このブロックをタップ");
            AllowOnly(first.Coord);
            MoveRingTo(first);

            yield return WaitAccepted(first);

            // ② 隣接（上下左右）のどれかを押させる
            var neighbors = GetNeighbors4(first.Coord);
            neighbors.RemoveAll(c => !viewByCoord.ContainsKey(c));
            neighbors.RemoveAll(c => viewByCoord[c].IsOutZone);

            // 同じセルを2回押すのは除外
            neighbors.RemoveAll(c => c == first.Coord);

            if (neighbors.Count == 0)
            {
                // 近くに無いならこのループはスキップ
                continue;
            }

            SetHint("次に、隣のブロックをタップ");
            AllowMany(neighbors);

            // リングを候補の間で回す（それっぽい誘導）
            StartRingCycle(neighbors);

            yield return WaitAcceptedAny(neighbors);

            StopRingCycle();
            ClearAllowed();
            yield return new WaitForSeconds(0.1f);
        }

        // Next rise 表示の簡易強調（任意）
        if (movesText != null)
        {
            SetHint("ここで上がってくるまでの回数が見えるよ");
            yield return PulseRect(movesText.rectTransform, 0.2f, 1.15f, 2);
            yield return new WaitForSeconds(0.3f);
        }

        // 終了
        PlayerPrefs.SetInt(tutorialKey, 1);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] Done. Saved flag.");

        HideOverlay();
        IsRunning = false;
    }

    // --- blink ---
    private Coroutine blinkCo;
    private readonly List<CellView> blinking = new List<CellView>();

    private void StartBlink(IEnumerable<Vector2Int> coords)
    {
        StopBlink();

        foreach (var c in coords)
        {
            if (viewByCoord.TryGetValue(c, out var v) && v != null && !v.IsOutZone)
                blinking.Add(v);
        }

        if (blinking.Count > 0)
            blinkCo = StartCoroutine(BlinkRoutine());
    }

    private void StopBlink()
    {
        if (blinkCo != null)
        {
            StopCoroutine(blinkCo);
            blinkCo = null;
        }

        // 点滅解除（色戻す）
        foreach (var v in blinking)
            if (v != null) v.SetSelected(false);

        blinking.Clear();
    }

    private IEnumerator BlinkRoutine()
    {
        bool on = false;
        while (IsRunning && blinking.Count > 0)
        {
            on = !on;
            foreach (var v in blinking)
                if (v != null) v.SetSelected(on);

            yield return new WaitForSeconds(0.25f); // 点滅速度
        }
    }


    private CellView FindAnyPlayableCell()
    {
        // 盤面の下側から最初の「アウトゾーンじゃないセル」を探す
        // （値の有無まで厳密にやるなら BoardLogic.GetCell 参照でもOK）
        foreach (var kv in viewByCoord)
        {
            var v = kv.Value;
            if (v == null) continue;
            if (v.IsOutZone) continue;
            return v;
        }
        return null;
    }

    private List<Vector2Int> GetNeighbors4(Vector2Int c)
    {
        return new List<Vector2Int>
        {
            new Vector2Int(c.x + 1, c.y),
            new Vector2Int(c.x - 1, c.y),
            new Vector2Int(c.x, c.y + 1),
            new Vector2Int(c.x, c.y - 1),
        };
    }

    private IEnumerator WaitAccepted(CellView expected)
    {
        int start = acceptedClicks;
        yield return new WaitUntil(() => acceptedClicks > start && lastAccepted == expected);
    }

    private IEnumerator WaitAcceptedAny(List<Vector2Int> coords)
    {
        int start = acceptedClicks;
        yield return new WaitUntil(() =>
        {
            if (acceptedClicks <= start) return false;
            if (lastAccepted == null) return false;
            return coords.Contains(lastAccepted.Coord);
        });
    }

    private void AllowOnly(Vector2Int coord)
    {
        allowed.Clear();
        allowed.Add(coord);
        StartBlink(new[] { coord });      // ★追加
    }

    private void AllowMany(List<Vector2Int> coords)
    {
        allowed.Clear();
        foreach (var c in coords) allowed.Add(c);
        StartBlink(coords);               // ★追加
    }

    private void ClearAllowed()
    {
        allowed.Clear();
        StopBlink();                      // ★追加
    }

    private void ShowOverlay()
    {
        if (overlayGroup != null)
        {
            overlayGroup.alpha = 1f;
            overlayGroup.blocksRaycasts = false; // 重要：盤面クリックを奪わない
            overlayGroup.interactable = false;
        }
        if (tapRingGroup != null) tapRingGroup.alpha = 1f;




        if (tapRing) tapRing.gameObject.SetActive(true);
        if (hintText) hintText.gameObject.SetActive(true);
    }

    private void HideOverlay()
    {
        StopRingCycle();
        ClearAllowed();

        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;
        }
        if (tapRing) tapRing.gameObject.SetActive(false);
        if (hintText) hintText.gameObject.SetActive(false);
    }

    private void SetHint(string msg)
    {
        if (hintText) hintText.text = msg;
    }

    private void MoveRingTo(CellView v)
    {
        if (tapRing == null || v == null) return;
        var cellRt = v.transform as RectTransform;
        if (cellRt == null) return;

        // 同じCanvas配下ならワールド位置合わせでOK
        tapRing.position = cellRt.position;
    }

    private void StartRingCycle(List<Vector2Int> coords)
    {
        StopRingCycle();
        ringCycleCo = StartCoroutine(RingCycle(coords));
    }

    private void StopRingCycle()
    {
        if (ringCycleCo != null)
        {
            StopCoroutine(ringCycleCo);
            ringCycleCo = null;
        }
    }

    private IEnumerator RingCycle(List<Vector2Int> coords)
    {
        int idx = 0;
        while (IsRunning && coords != null && coords.Count > 0)
        {
            var c = coords[idx % coords.Count];
            if (viewByCoord.TryGetValue(c, out var v))
                MoveRingTo(v);

            idx++;
            yield return new WaitForSeconds(0.35f);
        }
    }

    private IEnumerator PulseRect(RectTransform rt, float stepSec, float scale, int times)
    {
        if (rt == null) yield break;

        Vector3 baseScale = rt.localScale;
        for (int i = 0; i < times; i++)
        {
            rt.localScale = baseScale * scale;
            yield return new WaitForSeconds(stepSec);
            rt.localScale = baseScale;
            yield return new WaitForSeconds(stepSec);
        }
        rt.localScale = baseScale;
    }

    private void Update()
    {
        if (!enableDebugKeys) return;

#if UNITY_EDITOR
        // T: フラグ削除（次回起動でチュートリアル復活）
        // Y: 強制即開始
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.tKey.wasPressedThisFrame) ResetTutorialFlag();
            if (Keyboard.current.yKey.wasPressedThisFrame) ForceStartTutorial();
        }
#else
        if (Input.GetKeyDown(KeyCode.T)) ResetTutorialFlag();
        if (Input.GetKeyDown(KeyCode.Y)) ForceStartTutorial();
#endif
#endif
    }
}

