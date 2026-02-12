using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CellView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text label;

    [Header("Colors")]
    [SerializeField] private Color emptyColor = new Color32(0x71, 0x66, 0x66, 0xFF); // 716666
    [SerializeField] private Color outZoneColor = new Color32(0xAA, 0xAA, 0xAA, 0xFF);
    [SerializeField] private Color minColor  = new Color32(0xA0, 0xA8, 0xFF, 0xFF); // 1  : A0A8FF
    [SerializeField] private Color maxColor  = new Color32(0xFF, 0xA1, 0xA0, 0xFF); // 20 : FFA1A0
    [SerializeField, Range(0f, 1f)] private float neighborBrighten = 0.25f;        // 隣接をどれくらい白寄りにするか

    public Vector2Int Coord { get; private set; }
    public bool IsOutZone { get; private set; }

    private Action<CellView> onClick;

    // ✅ ここが元コードのバグ点：baseColor が無いのに参照してる
    // → baseColor を正式にフィールドとして持つ
    private Color baseColor;
    private bool _neighborHint;

    public void Init(Vector2Int coord, Action<CellView> onClick)
    {
        Coord = coord;
        this.onClick = onClick;

        if (!button) button = GetComponent<Button>();
        if (!background) background = GetComponent<Image>();
        if (!label) label = GetComponentInChildren<TMP_Text>(true);

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => this.onClick?.Invoke(this));
        }
    }

    public void SetOutZone(bool on)
    {
        IsOutZone = on;
        // 色だけ先に反映しておく（SetValueがすぐ呼ばれる想定だが、呼ばれなくても見た目が崩れないように）
        if (IsOutZone)
        {
            SetBaseColor(outZoneColor);
            if (label) label.text = "";
        }
    }

    // 既存：今は選択しても色変えない方針のまま
    public void SetSelected(bool on)
    {
        // 今は色は変えずにOK（必要ならここで枠/スケール等を足す）
    }

    public void SetInteractable(bool canClick)
    {
        if (button) button.interactable = canClick;
    }

    // 既存コード互換（以前使ってた名前が残ってても落ちないように）
    public void SetTutorialHighlight(bool on) => SetNeighborHint(on);

    public void SetNeighborHint(bool on)
    {
        _neighborHint = on;
        if (!background) return;

        background.color = on
            ? Color.Lerp(baseColor, Color.white, neighborBrighten)
            : baseColor;
    }

    // ✅ 既存の BoardPresenter が呼んでる SetValue(int) を残す
    //   outzoneは BoardPresenter側で -1 を渡してるので、その場合は数字非表示にする
    public void SetValue(int value)
    {
        if (!background || !label)
        {
            // 参照が無い場合でも落ちないように
            if (!background) background = GetComponent<Image>();
            if (!label) label = GetComponentInChildren<TMP_Text>(true);
        }

        if (IsOutZone)
        {
            // ✅ OutZoneは常に指定色＆数字なし（あなたの要望：色変えたい）
            SetBaseColor(outZoneColor);
            if (label) label.text = "";
            return;
        }

        if (value < 0)
        {
            SetBaseColor(emptyColor);
            if (label) label.text = "";
        }
        else
        {
            SetBaseColor(EvaluateNumberColor(value));
            if (label) label.text = value.ToString();
        }

        // 既に隣接ヒントがONなら、色を再適用
        if (_neighborHint) SetNeighborHint(true);
    }

    private void SetBaseColor(Color c)
    {
        baseColor = c;
        if (background) background.color = baseColor;
    }

    private Color EvaluateNumberColor(int value)
    {
        int v = Mathf.Clamp(value, 1, 20);
        float t = (v - 1f) / 19f; // 1→0, 20→1
        return Color.Lerp(minColor, maxColor, t);
    }
}
