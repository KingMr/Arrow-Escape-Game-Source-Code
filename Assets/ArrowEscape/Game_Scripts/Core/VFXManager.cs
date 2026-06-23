using UnityEngine;

namespace Core
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("VFX Settings")]
        public Color blockedFlashColor = Color.red;
        public float flashDuration = 0.3f;

        [Header("Particle Effects")]
        public Camera uiCamera;
        public GameObject clickCanvas;
        public GameObject clickParticlePrefab;
        public GameObject winParticlePrefab;
        public GameObject loseParticlePrefab;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private System.Collections.Generic.HashSet<ArrowUnit> flashingArrows = new System.Collections.Generic.HashSet<ArrowUnit>();

        public void PlayBlockedAnimation(ArrowUnit arrow)
        {
            if (arrow == null || flashingArrows.Contains(arrow)) return;
            StartCoroutine(FlashRoutine(arrow));
        }

        public void PlayWinEffect()
        {
            if (winParticlePrefab != null)
            {
                Vector3 centerPos = GetCameraCenter();
                GameObject go = Instantiate(winParticlePrefab, centerPos, Quaternion.identity);
                if (go.GetComponent<AutoDestroyParticle>() == null)
                    go.AddComponent<AutoDestroyParticle>();
            }
        }

        public void PlayLoseEffect()
        {
            if (loseParticlePrefab != null)
            {
                Vector3 centerPos = GetCameraCenter();
                GameObject go = Instantiate(loseParticlePrefab, centerPos, Quaternion.identity);
                if (go.GetComponent<AutoDestroyParticle>() == null)
                    go.AddComponent<AutoDestroyParticle>();
            }
        }

        public void PlayClickEffect(Vector3 position)
        {
            if (clickParticlePrefab == null) return;

            GameObject particle = Instantiate(clickParticlePrefab);
            
            if (clickCanvas != null)
            {
                particle.transform.SetParent(clickCanvas.transform, false);
                
                if (uiCamera != null)
                {
                    // Use UI camera to convert screen point to world point
                    float distance = Mathf.Abs(uiCamera.transform.position.z);
                    Vector3 worldPos = uiCamera.ScreenToWorldPoint(new Vector3(position.x, position.y, distance));
                    particle.transform.position = worldPos;
                }
                else
                {
                    particle.transform.position = position;
                }
            }
            else
            {
                particle.transform.position = position;
            }

            if (particle.GetComponent<AutoDestroyParticle>() == null)
            {
                particle.AddComponent<AutoDestroyParticle>();
            }
        }

        private Vector3 GetCameraCenter()
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector3.zero;
            
            // Get the center of the camera's viewport in world space
            // Keep Z at 0 for 2D particles
            Vector3 centerPos = cam.transform.position;
            centerPos.z = 0;
            return centerPos;
        }

        private System.Collections.IEnumerator FlashRoutine(ArrowUnit arrow)
        {
            if (arrow == null) yield break;
            
            flashingArrows.Add(arrow);

            // Get all renderers in the arrow (lines and head/body sprites)
            var lineRenderers = arrow.GetComponentsInChildren<LineRenderer>();
            var spriteRenderers = arrow.GetComponentsInChildren<SpriteRenderer>();

            // Store original colors and states
            var originalLineColors = new System.Collections.Generic.Dictionary<LineRenderer, (Color, Color)>();
            foreach (var lr in lineRenderers)
            {
                originalLineColors[lr] = (lr.startColor, lr.endColor);
            }

            var originalSpriteColors = new System.Collections.Generic.Dictionary<SpriteRenderer, Color>();
            foreach (var sr in spriteRenderers)
            {
                originalSpriteColors[sr] = sr.color;
            }

            // Apply Flash Color
            foreach (var lr in lineRenderers)
            {
                lr.startColor = blockedFlashColor;
                lr.endColor = blockedFlashColor;
            }
            foreach (var sr in spriteRenderers)
            {
                sr.color = blockedFlashColor;
            }

            // Shake Effect
            Vector3 originalLocalPos = arrow.transform.localPosition;
            float elapsed = 0;
            float shakeMagnitude = 0.15f;

            while (elapsed < flashDuration)
            {
                float x = Random.Range(-1f, 1f) * shakeMagnitude;
                float y = Random.Range(-1f, 1f) * shakeMagnitude;
                
                if (arrow != null)
                    arrow.transform.localPosition = originalLocalPos + new Vector3(x, y, 0);
                else
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Restore
            if (arrow != null)
            {
                arrow.transform.localPosition = originalLocalPos;
                
                foreach (var lr in lineRenderers)
                {
                    if (lr != null && originalLineColors.ContainsKey(lr))
                    {
                        lr.startColor = originalLineColors[lr].Item1;
                        lr.endColor = originalLineColors[lr].Item2;
                    }
                }
                foreach (var sr in spriteRenderers)
                {
                    if (sr != null && originalSpriteColors.ContainsKey(sr))
                    {
                        sr.color = originalSpriteColors[sr];
                    }
                }
            }
            
            // Allow flashing again
            flashingArrows.Remove(arrow);
        }
    }
}

