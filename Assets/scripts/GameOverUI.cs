using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject root;      // GameOverPanel自身でもOK
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text bestText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button shareButton;

    public event Action OnNext;
    public event Action OnShare;

    private void Awake()
    {
        if (root == null) root = gameObject;

        if (nextButton)  nextButton.onClick.AddListener(() => OnNext?.Invoke());
        if (shareButton) shareButton.onClick.AddListener(() => OnShare?.Invoke());
    }

    public void Show(int score, int best)
    {
        if (scoreText) scoreText.text = $"Score {score}";
        if (bestText)  bestText.text  = $"Best {best}";
        if (root) root.SetActive(true);
    }

    public void Hide()
    {
        if (root) root.SetActive(false);
    }
}
