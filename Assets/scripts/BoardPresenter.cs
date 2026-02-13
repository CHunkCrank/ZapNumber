using System.Collections;
using System.Collections.Generic;
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

    [Header("Ads")]
    [SerializeField] private InterstitialAdController interstitial;

    [Header("Debug")]
    [SerializeField] private bool enableDebugKeys = true;
    [SerializeField] private KeyCode resetTutorialKey = KeyCode.T;
    [SerializeField] private KeyCode forceShowTutorialKey = KeyCode.R;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text movesText;
    [SerializeField] private TMP_Text bestText;

    [Header("OutZone")]
    [SerializeField] private int outZoneRows = 1;

    [Header("Feedback")]
    [SerializeField, Range(0.05f, 0.35f)] private float resolvePairDelay = 0.16f;

    private CellView[,] views;
    private CellView selected;

    private bool inputLocked = false;
    private bool pairResolving = false;
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
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
            Destroy(gridRoot.GetChild(i).gameObject);

        int w = board.Width;
        int h = board.Height;

        views = new CellView[w, h];

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

        int outZoneStartY = h - outZoneRows;

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var v = views[x, y];

            bool isOutZone = (y >= outZoneStartY);
            v.SetOutZone(isOutZone);

            int value = board.GetCell(x, y);
            v.SetValue(isOutZone ? -1 : value);

            v.SetSelected(selected != null && selected == v);
        }

        if (movesText) movesText.text = $" Next rise {board.MovesUntilRise}";

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            views[x, y].SetNeighborHint(false);

        if (selected != null)
        {
            var c = selected.Coord;
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
        pairResolving = false;

        board.NewGame();
        Refresh();
    }

    private void OnCellClicked(CellView v)
    {
        if (tutorial != null && tutorial.IsRunning)
        {
            if (tutorial.InterceptCellClick(v)) return;
            return;
        }

        OnCellClickedExternal?.Invoke(v);

        if (inputLocked || pairResolving) return;
        if (v.IsOutZone) return;

        if (selected == null)
        {
            selected = v;
            selected.PlayTapFeedback();
            Refresh();
            return;
        }

        if (selected == v)
        {
            selected = null;
            Refresh();
            return;
        }

        StartCoroutine(CoResolvePairWithFeedback(selected, v));
    }

    private IEnumerator CoResolvePairWithFeedback(CellView aView, CellView bView)
    {
        pairResolving = true;

        List<CellView> clearTargets = GetCellsToClear(aView.Coord, bView.Coord);
        for (int i = 0; i < clearTargets.Count; i++)
            clearTargets[i].PlayClearFeedback();

        if (resolvePairDelay > 0f)
            yield return new WaitForSeconds(resolvePairDelay);

        board.TryProcessPair(aView.Coord, bView.Coord);
        selected = null;
        Refresh();

        pairResolving = false;
    }

    private List<CellView> GetCellsToClear(Vector2Int a, Vector2Int b)
    {
        var list = new List<CellView>(2);

        int va = board.GetCell(a.x, a.y);
        int vb = board.GetCell(b.x, b.y);
        if (va < 0 || vb < 0) return list;

        if (va == vb)
        {
            list.Add(views[a.x, a.y]);
            list.Add(views[b.x, b.y]);
            return list;
        }

        if (va > vb) list.Add(views[b.x, b.y]);
        else list.Add(views[a.x, a.y]);

        return list;
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
            Debug.Log("[Debug] tutorial_done を削除しました（次起動でチュートリアル表示）");
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("[Debug] チュートリアル強制表示は未実装です");
        }
    }
}
