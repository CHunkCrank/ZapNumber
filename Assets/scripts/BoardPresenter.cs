using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class BoardPresenter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BoardLogic board;
    [SerializeField] private Transform gridRoot;
    [SerializeField] private CellView cellPrefab;
    [SerializeField] private TutorialController tutorial;

    [Header("Game Over UI")]
    [SerializeField] private GameOverUI gameOverUI;

    // ✅ 追加：インタースティシャル
    [Header("Ads")]
    [SerializeField] private InterstitialAdController interstitial;

    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true; // InspectorでON/OFF
    [SerializeField] private KeyCode resetTutorialKey = KeyCode.T;
    [SerializeField] private KeyCode forceShowTutorialKey = KeyCode.R; // 使わないなら無視でOK

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text movesText;

    // ✅ 追加：MainPanelにBest表示したい
    [SerializeField] private TMP_Text bestText;

    [Header("OutZone")]
    [SerializeField] private int outZoneRows = 1; // 上1行をアウトゾーン表示したい

    private CellView[,] views;
    private CellView selected;

    private bool inputLocked = false;
    private const string BestKey = "BEST_SCORE";

    private void Start()
    {
        BuildGrid();

        board.OnBoardChanged += Refresh;
        board.OnScoreChanged += s => { if (scoreText) scoreText.text = $"Score {s}"; };
        board.OnGameOver += HandleGameOver;

        if (gameOverUI)
        {
            gameOverUI.Hide();
            gameOverUI.OnNext += RestartGame;
            gameOverUI.OnShare += () => Debug.Log("[Share] pressed (stub)");
        }

        Refresh();
        UpdateBestText();

        tutorial?.BeginIfNeeded();
    }

    private void BuildGrid()
    {
        // 既存削除
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
            Destroy(gridRoot.GetChild(i).gameObject);

        int w = board.Width;
        int h = board.Height;

        views = new CellView[w, h];

        // ★重要：StartCorner=LowerLeft に合わせて y=0 から生成（下→上）
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var v = Instantiate(cellPrefab, gridRoot);
                var coord = new Vector2Int(x, y);
                v.Init(coord, OnCellClicked);
                views[x, y] = v;
            }
        }
    }

    private void Refresh()
    {
        int w = board.Width;
        int h = board.Height;

        // 上 outZoneRows 行をアウトゾーン扱い（クリック不可＋見た目変更）
        int outZoneStartY = h - outZoneRows;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var v = views[x, y];

            bool isOutZone = (y >= outZoneStartY);
            v.SetOutZone(isOutZone);

            int value = board.GetCell(x, y);

            // アウトゾーンは数字を出さない（見た目のライン用途）
            v.SetValue(isOutZone ? -1 : value);

            v.SetSelected(selected != null && selected == v);
        }

        if (movesText) movesText.text = $" Next rise {board.MovesUntilRise}";

        // --- 隣接マスの明るさ演出 ---
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            views[x, y].SetNeighborHint(false);

        if (selected != null)
        {
            var c = selected.Coord;
            // 4方向
            TryHint(c.x + 1, c.y);
            TryHint(c.x - 1, c.y);
            TryHint(c.x, c.y + 1);
            TryHint(c.x, c.y - 1);
        }

        void TryHint(int nx, int ny)
        {
            if (nx < 0 || nx >= w || ny < 0 || ny >= h) return;
            var v = views[nx, ny];
            if (v == null) return;
            if (v.IsOutZone) return;
            v.SetNeighborHint(true);
        }

        UpdateBestText();
    }

    private void HandleGameOver()
    {
        inputLocked = true;

        int score = board.Score;
        int best = PlayerPrefs.GetInt(BestKey, 0);
        if (score > best)
        {
            best = score;
            PlayerPrefs.SetInt(BestKey, best);
            PlayerPrefs.Save();
        }

        // ✅ ここで挿入広告
        interstitial?.ShowIfReady();

        if (gameOverUI) gameOverUI.Show(score, best);

        UpdateBestText();
    }

    private void UpdateBestText()
    {
        if (!bestText) return;
        int best = PlayerPrefs.GetInt(BestKey, 0);
        bestText.text = $"Best {best}";
    }

    private void RestartGame()
    {
        if (gameOverUI) gameOverUI.Hide();

        selected = null;
        inputLocked = false;

        board.NewGame();   // 盤面初期化
        Refresh();         // 表示更新
    }

    private void OnCellClicked(CellView v)
    {
        if (tutorial != null && tutorial.IsRunning)
        {
            if (tutorial != null && tutorial.InterceptCellClick(v)) return;
            return; // ゲーム側に流さない
        }

        OnCellClickedExternal?.Invoke(v);

        if (inputLocked) return;
        if (v.IsOutZone) return;

        if (selected == null)
        {
            selected = v;
            Refresh();
            return;
        }

        if (selected == v)
        {
            selected = null;
            Refresh();
            return;
        }

        // 2つ目クリック → 処理
        board.TryProcessPair(selected.Coord, v.Coord);
        selected = null;
        Refresh();
    }

    public event System.Action<CellView> OnCellClickedExternal;

    public CellView GetCellView(int x, int y) => views[x, y];

    public void SetAllCellsInteractable(bool canClick)
    {
        int w = board.Width;
        int h = board.Height;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var v = views[x, y];
            v.SetInteractable(canClick && !v.IsOutZone);
            v.SetTutorialHighlight(false);
        }
    }

    private void Update()
    {
        if (!enableDebugKeys) return;

        if (Keyboard.current == null) return;

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            PlayerPrefs.DeleteKey("tutorial_done");
            PlayerPrefs.Save();
            Debug.Log("[Debug] tutorial_done を削除しました（次回起動でチュートリアル表示）");
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("[Debug] チュートリアル強制（※後で呼び出し先を繋ぐ）");
        }
    }
}
