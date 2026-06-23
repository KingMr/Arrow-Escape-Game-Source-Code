using UnityEngine;

namespace Core
{
    public class AutoDestroyParticle : MonoBehaviour
    {
        private ParticleSystem ps;

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
        }

        private void Start()
        {
            float dur = 0f;

            if (ps != null)
            {
                var main = ps.main;
                dur = main.duration + main.startLifetimeMultiplier;
            }

            Destroy(gameObject, Mathf.Max(dur, 0.5f));
        }
    }
}
