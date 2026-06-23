using UnityEngine;

namespace Core
{
    public class ClickParticleHandler : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (VFXManager.Instance != null)
                {
                    VFXManager.Instance.PlayClickEffect(Input.mousePosition);
                }
            }
        }
    }
}
