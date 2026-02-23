using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public static class UIEffect
{
    private static Dictionary<RectTransform, Tween> breatheTweens = new Dictionary<RectTransform, Tween>();

    // 彈出動畫（勝利面板/重要提示）
    public static void Popup(GameObject panel)
    {
        panel.transform.localScale = Vector3.zero;
        panel.SetActive(true);
        panel.transform
             .DOScale(1f, 0.4f)
             .SetEase(Ease.OutBack)
             .SetUpdate(true); // 不受 Time.timeScale 影響
    }

    // 彈出按鈕動畫
    public static void ButtonPopup(GameObject button, float duration = 0.5f)
    {
        button.transform.localScale = Vector3.zero;
        button.SetActive(true);
        button.transform
              .DOScale(1f, duration)
              .SetEase(Ease.OutBack)
              .SetUpdate(true); // 不受 Time.timeScale 影響
    }

    // 淡入
    public static void FadeIn(CanvasGroup cg, float duration = 0.4f)
    {
        cg.alpha = 0;
        cg.DOFade(1f, duration)
          .SetUpdate(true); // 不受 Time.timeScale 影響
    }

    // 淡出
    public static void FadeOut(CanvasGroup cg, float duration = 0.3f)
    {
        cg.DOFade(0f, duration)
          .SetUpdate(true); // 不受 Time.timeScale 影響
    }

    // 震動（提示用）
    public static void Shake(RectTransform rect)
    {
        rect.DOShakeAnchorPos(0.5f, 10f, 20, 90f)
            .SetUpdate(true); // 不受 Time.timeScale 影響
    }

    // 呼吸縮放
    public static void Breathe(RectTransform rect, float scaleFactor = 1.1f, float duration = 1f)
    {
        if (breatheTweens.ContainsKey(rect))
        {
            breatheTweens[rect].Kill();
            breatheTweens.Remove(rect);
        }

        Vector3 originalScale = rect.localScale;

        Sequence seq = DOTween.Sequence();
        seq.Append(rect.DOScale(originalScale * scaleFactor, duration)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true)) // 不受 Time.timeScale 影響
           .Append(rect.DOScale(originalScale, duration)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true));
        seq.SetLoops(-1, LoopType.Restart);

        breatheTweens[rect] = seq;
    }

    public static void StopBreathe(RectTransform rect)
    {
        if (breatheTweens.ContainsKey(rect))
        {
            breatheTweens[rect].Kill();
            breatheTweens.Remove(rect);
        }
    }
}