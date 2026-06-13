using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ImageBlink : MonoBehaviour
{
    [Header("UI References")]
    public Image targetImage;

    [Header("Blink Settings")]
    public float blinkDuration = 0.5f;
    [Range(0f, 1f)] public float minAlpha = 0f;

    [Tooltip("How many full blinks before it stops?")]
    public int numberOfBlinks = 3;

    private void Start()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        // if (targetImage != null)
        // {
        //     StartBlinking();
        // }
    }

    public void StartBlinking()
    {
        gameObject.SetActive(true);
        targetImage.DOKill();

        // Reset to fully visible before starting
        Color color = targetImage.color;
        color.a = 1f;
        targetImage.color = color;

        // A Yoyo loop counts each direction as 1 loop. 
        // Fade Out = 1 loop, Fade In = 2 loops. Multiply by 2 for full blinks.
        int totalLoops = numberOfBlinks * 2;

        targetImage.DOFade(minAlpha, blinkDuration)
                   .SetLoops(totalLoops, LoopType.Yoyo)
                   .SetEase(Ease.InOutSine)
                   .OnComplete(ResetAlpha); // Ensures it resets when finished
    }

    public void StopBlinking()
    {
        targetImage.DOKill();
        ResetAlpha();
    }

    private void ResetAlpha()
    {
        gameObject.SetActive(false);
        // Guarantee the image is fully visible when the animation ends or is stopped
        if (targetImage != null)
        {
            Color color = targetImage.color;
            color.a = 1f;
            targetImage.color = color;
        }
    }

    private void OnDestroy()
    {
        if (targetImage != null)
        {
            targetImage.DOKill();
        }
    }
}