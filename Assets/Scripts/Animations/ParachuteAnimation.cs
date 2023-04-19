//using static Utils.SpringMotion;
using LlamAcademy.Spring.Runtime;
using UnityEngine;

public class ParachuteAnimation : MonoBehaviour
{
    [SerializeField]
    private SpringToScale ScaleSpring;
    [SerializeField]
    private Transform playerMeshTransform;

    [SerializeField]
    private Vector3 goalScale;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float damping;

    private Vector3 spingMotionVelocity;

    private Vector3 rotVector;
    private float rotVelocity;

    private bool isActive = false;

    private void Start()
    {
        transform.localScale = Vector3.zero;
    }

    public void Activate()
    {
        isActive = true;
        ScaleSpring.SpringTo(goalScale);
    }

    public void Deactivate()
    {
        isActive = false;
        ScaleSpring.SpringTo(Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        // Smoothly interpolate rotation.
        Quaternion target = isActive ? playerMeshTransform.localRotation : Quaternion.identity;
        float delta = Quaternion.Angle(transform.localRotation, target);
        if (delta > 0f)
        {
            float t = Mathf.SmoothDampAngle(delta, 0.0f, ref rotVelocity, damping);
            t = 1.0f - (t / delta);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, target, t);
        }
    }
}
