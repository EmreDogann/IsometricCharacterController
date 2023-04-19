using LlamAcademy.Spring.Runtime;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [SerializeField]
    private SpringToScale ScaleSpring;

    [Header("Jumping")]
    [SerializeField]
    private Vector3 scaleJump;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float dampingJump;
    [SerializeField]
    [Range(0, 100)]
    private float frequencyJump;

    [Header("Landing")]
    [SerializeField]
    private Vector3 scaleLand;
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float dampingLand;
    [SerializeField]
    [Range(0, 100)]
    private float frequencyLand;

    public void Activate()
    {
        ScaleSpring.Damping = dampingJump;
        ScaleSpring.Stiffness = frequencyJump;

        ScaleSpring.Nudge(scaleJump);
    }

    public void Deactivate()
    {
        ScaleSpring.Damping = dampingLand;
        ScaleSpring.Stiffness = frequencyLand;

        ScaleSpring.Nudge(scaleLand);
    }
}
