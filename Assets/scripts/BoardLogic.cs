using System;
using UnityEngine;

public class BoardLogic : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int width = 5;
    [SerializeField] private int height = 9;          // 8 + outzone(1)
    [SerializeField] private int outZoneRows = 1;     // ←追加
    [SerializeField] private int initialFilledRows = 4;

    [Header("Numbers")]
    [SerializeField] private int minValue = 1;
    [SerializeField] private int maxValue = 20;

    [Header("Rising")]
    [SerializeField] private int riseInterval = 5; // 3 or 5
    [SerializeField] private bool useFixedSeed = false;
    [SerializeField] private int seed = 12345;

    public int Width => width;
    public int Height => height;                 // 総高さ（アウトゾーン込み）
    public int PlayHeight => Mathf.Max(0, height - outZoneRows); // ←追加
    public int Score { get; private set; }
    public int Moves { get; private set; }
    public int MovesUntilRise => Mathf.Max(0, riseInterval - (Moves % riseInterval));

    // -1 = empty
    private int[,] grid;

    public event Action OnBoardChanged;
    public event Action OnGameOver;
    public event Action<int> OnScoreChanged;

    private System.Random rng;

    private void Awake()
    {
        rng = useFixedSeed ? new System.Random(seed) : new System.Random();
        grid = new int[width, height];
    }

    private void Start() => NewGame();

    public void NewGame()
    {
        Score = 0;
        Moves = 0;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = -1;

        // 下側だけ初期配置（アウトゾーンには置かない）
        int fillRows = Mathf.Clamp(initialFilledRows, 0, PlayHeight);
        for (int y = 0; y < fillRows; y++)
            for (int x = 0; x < width; x++)
                grid[x, y] = RandValue();

        ApplyGravity();
        OnScoreChanged?.Invoke(Score);
        OnBoardChanged?.Invoke();
    }

    public int GetCell(int x, int y) => InBounds(x, y) ? grid[x, y] : -1;

    public bool TryProcessPair(Vector2Int a, Vector2Int b)
    {
        if (!InBounds(a.x, a.y) || !InBounds(b.x, b.y)) return false;
        if (!IsAdjacent(a, b)) return false;

        // アウトゾーンは操作不可にしたいならここで弾く
        if (a.y >= PlayHeight || b.y >= PlayHeight) return false;

        int va = grid[a.x, a.y];
        int vb = grid[b.x, b.y];
        if (va < 0 || vb < 0) return false;

        if (va == vb)
        {
            grid[a.x, a.y] = -1;
            grid[b.x, b.y] = -1;
            AddScore(va * 2);
        }
        else
        {
            int diff = Math.Abs(va - vb);
            if (va > vb) { grid[a.x, a.y] = diff; grid[b.x, b.y] = -1; }
            else { grid[b.x, b.y] = diff; grid[a.x, a.y] = -1; }
            AddScore(diff);
        }

        ApplyGravity();
        Moves++;

        // ここでアウトゾーン到達＝即ゲームオーバーにする
        if (IsOutZoneOccupied())
        {
            OnBoardChanged?.Invoke();
            OnGameOver?.Invoke();
            return true;
        }

        if (riseInterval > 0 && Moves % riseInterval == 0)
        {
            RiseOneRow();

            if (IsOutZoneOccupied())
            {
                OnBoardChanged?.Invoke();
                OnGameOver?.Invoke();
                return true;
            }
        }

        OnBoardChanged?.Invoke();
        return true;
    }

    private bool IsOutZoneOccupied()
    {
        for (int y = PlayHeight; y < height; y++)
            for (int x = 0; x < width; x++)
                if (grid[x, y] >= 0) return true;
        return false;
    }

    private void RiseOneRow()
    {
        for (int y = height - 1; y >= 1; y--)
            for (int x = 0; x < width; x++)
                grid[x, y] = grid[x, y - 1];

        for (int x = 0; x < width; x++)
            grid[x, 0] = RandValue();
    }

    private void ApplyGravity()
    {
        for (int x = 0; x < width; x++)
        {
            int writeY = 0;
            for (int y = 0; y < PlayHeight; y++)   // ← height じゃなく PlayHeight
            {
                if (grid[x, y] >= 0)
                {
                    int v = grid[x, y];
                    if (writeY != y) { grid[x, writeY] = v; grid[x, y] = -1; }
                    writeY++;
                }
            }
        }
    }


    private void AddScore(int add)
    {
        Score += Mathf.Max(0, add);
        OnScoreChanged?.Invoke(Score);
    }

    private int RandValue()
    {
        int lo = Mathf.Min(minValue, maxValue);
        int hi = Mathf.Max(minValue, maxValue);
        return rng.Next(lo, hi + 1);
    }

    private bool InBounds(int x, int y) => (x >= 0 && x < width && y >= 0 && y < height);
    private bool IsAdjacent(Vector2Int a, Vector2Int b) => (Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y)) == 1;
}
