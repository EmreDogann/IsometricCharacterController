using UnityEngine;

namespace LlamAcademy.Spring.Runtime
{
    public class BaseSpringBehaviour : MonoBehaviour
    {
        [SerializeField]
        [Range(0.0f, 1.0f)]
        protected float Damping = 0.5f;
        [SerializeField]
        [Range(0, 100)]
        protected float Stiffness = 10.0f;
    }
}
