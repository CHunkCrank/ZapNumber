using System;
using System.Collections;
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
    [SerializeField] private Color emptyColor = new Color32(0x71, 0x66, 0x66, 0xFF);
    [SerializeField] private Color outZoneColor = new Color32(0xAA, 0xAA, 0xAA, 0xFF);
    [SerializeField] private Color minColor = new Color32(0xA0, 0xA8, 0xFF, 0xFF);
    [SerializeField] private Color maxColor = new Color32(0xFF, 0xA1, 0xA0, 0xFF);
    [SerializeField, Range(0f, 1f)] private float neighborBrighten = 0.25f;

    [Header("Feedback")]
    [SerializeField] private Color selectedColor = new Color32(0xFF, 0xD5, 0x66, 0xFF);
    [SerializeField, Range(0f, 1f)] private float selectedTint = 0.65f;
    [SerializeField, Range(1f, 1.2f)] private float tapPulseScale = 1.08f;
    [SerializeField, Range(0.04f, 0.25f)] private float tapPulseSeconds = 0.10f;
    [SerializeField] private Color clearFlashColor = new Color32(0xFF, 0xF2, 0xAA, 0xFF);
    [SerializeField, Range(0.06f, 0.35f)] private float clearFlashSeconds = 0.18f;

    public Vector2Int Coord { get; private set; }
    public bool IsOutZone { get; private set; }

    private Action<CellView> onClick;
    private Color baseColor;
    private bool neighborHint;
    private bool selected;
    private Coroutine pulseRoutine;
    private Coroutine clearRoutine;
    private Vector3 baseScale = Vector3.one;

    public void Init(Vector2Int coord, Action<CellView> onClick)
    {
        Coord = coord;
        this.onClick = onClick;

        if (!button) button = GetComponent<Button>();
        if (!background) background = GetComponent<Image>();
        if (!label) label = GetComponentInChildren<TMP_Text>(true);
        baseScale = transform.localScale;

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => this.onClick?.Invoke(this));
        }
    }

    public void SetOutZone(bool on)
    {
        IsOutZone = on;
        if (IsOutZone)
        {
            SetBaseColor(outZoneColor);
            if (label) label.text = "";
        }
    }

    public void SetSelected(bool on)
    {
        selected = on;
        ApplyVisualColor();
    }

    public void SetInteractable(bool canClick)
    {
        if (button) button.interactable = canClick;
    }

    public void SetTutorialHighlight(bool on) => SetNeighborHint(on);

    public void SetNeighborHint(bool on)
    {
        neighborHint = on;
        ApplyVisualColor();
    }

    public void SetValue(int value)
    {
        if (!background || !label)
        {
            if (!background) background = GetComponent<Image>();
            if (!label) label = GetComponentInChildren<TMP_Text>(true);
        }

        if (IsOutZone)
        {
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

        if (neighborHint) SetNeighborHint(true);
    }

    public void PlayTapFeedback()
    {
        if (!isActiveAndEnabled) return;
        if (pulseRoutine != null) StopCoroutine(pulseRoutine);
        pulseRoutine = StartCoroutine(CoPulse());
    }

    public void PlayClearFeedback()
    {
        if (!isActiveAndEnabled || !background) return;
        if (clearRoutine != null) StopCoroutine(clearRoutine);
        clearRoutine = StartCoroutine(CoClearFlash());
    }

    private void SetBaseColor(Color c)
    {
        baseColor = c;
        ApplyVisualColor();
    }

    private void ApplyVisualColor()
    {
        if (!background) return;

        Color c = baseColor;
        if (neighborHint) c = Color.Lerp(c, Color.white, neighborBrighten);
        if (selected) c = Color.Lerp(c, selectedColor, selectedTint);
        background.color = c;
    }

    private IEnumerator CoPulse()
    {
        float half = tapPulseSeconds * 0.5f;
        if (half <= 0f) yield break;

        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float k = t / half;
            transform.localScale = Vector3.Lerp(baseScale, baseScale * tapPulseScale, k);
            yield return null;
        }

        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float k = t / half;
            transform.localScale = Vector3.Lerp(baseScale * tapPulseScale, baseScale, k);
            yield return null;
        }

        transform.localScale = baseScale;
        pulseRoutine = null;
    }

    private IEnumerator CoClearFlash()
    {
        Color from = background.color;
        Color labelFrom = label ? label.color : Color.white;

        float half = clearFlashSeconds * 0.5f;
        if (half <= 0f) yield break;

        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float k = t / half;
            background.color = Color.Lerp(from, clearFlashColor, k);
            if (label) label.color = new Color(labelFrom.r, labelFrom.g, labelFrom.b, Mathf.Lerp(labelFrom.a, 0.15f, k));
            yield return null;
        }

        Color to = baseColor;
        if (neighborHint) to = Color.Lerp(to, Color.white, neighborBrighten);
        if (selected) to = Color.Lerp(to, selectedColor, selectedTint);

        for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
        {
            float k = t / half;
            background.color = Color.Lerp(clearFlashColor, to, k);
            if (label) label.color = new Color(labelFrom.r, labelFrom.g, labelFrom.b, Mathf.Lerp(0.15f, labelFrom.a, k));
            yield return null;
        }

        ApplyVisualColor();
        if (label) label.color = labelFrom;
        clearRoutine = null;
    }

    private Color EvaluateNumberColor(int value)
    {
        int v = Mathf.Clamp(value, 1, 20);
        float t = (v - 1f) / 19f;
        return Color.Lerp(minColor, maxColor, t);
    }
}
