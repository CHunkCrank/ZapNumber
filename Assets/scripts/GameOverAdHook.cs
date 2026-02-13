using UnityEngine;

public class GameOverAdHook : MonoBehaviour
{
    [Tooltip("空なら FindFirstObjectByType で自動取得します")]
    [SerializeField] private InterstitialAdController interstitial;

    private bool shown;

    private void Awake()
    {
        if (interstitial == null)
            interstitial = FindFirstObjectByType<InterstitialAdController>();
    }

    private void OnEnable()
    {
        // GameOver表示のたびに1回だけ
        if (shown) return;
        shown = true;

        if (interstitial != null)
            interstitial.ShowIfReady();
    }

    private void OnDisable()
    {
        // 次のGameOverに備えてリセット
        shown = false;
    }
}
