using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCutoutSync : MonoBehaviour
{
    public static int PosID = Shader.PropertyToID("_Position");
    public static int SizeID = Shader.PropertyToID("_Size");

    public Camera camera;
    public LayerMask mask;
    [Range(0, 5)]
    public float scaleEaseTime;
    [Range(0, 10)]
    public float targetScale;

    private Dictionary<int, (Material mat, bool isHit, float scaleVelocity)> materialsMap;

    private void Start()
    {
        materialsMap = new Dictionary<int, (Material mat, bool isHit, float scaleVelocity)>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 dir = camera.transform.position - transform.position;
        Vector3 view = camera.WorldToViewportPoint(transform.position);

        RaycastHit[] hits = Physics.RaycastAll(new Ray(transform.position, dir.normalized), dir.magnitude, mask);
        if (hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                Material mat = hits[i].transform.GetComponent<MeshRenderer>().material;
                float scale;
                float scaleVelocity;
                if (materialsMap.ContainsKey(mat.GetInstanceID()))
                {
                    scaleVelocity = materialsMap[mat.GetInstanceID()].scaleVelocity;
                    float finalTargetScale = targetScale * (hits[i].distance / (dir.magnitude / 2.0f));
                    finalTargetScale = Mathf.Clamp(finalTargetScale, 0.5f, targetScale);

                    scale = Mathf.SmoothDamp(mat.GetFloat(SizeID), targetScale * finalTargetScale, ref scaleVelocity, scaleEaseTime);
                }
                else
                {
                    scaleVelocity = 0.0f;
                    scale = Mathf.SmoothDamp(mat.GetFloat(SizeID), targetScale, ref scaleVelocity, scaleEaseTime);
                }
                materialsMap[mat.GetInstanceID()] = (mat, true, scaleVelocity);
                mat.SetFloat(SizeID, scale);
            }

        }

        foreach (KeyValuePair<int, (Material mat, bool isHit, float scaleVelocity)> entry in materialsMap.ToList())
        {
            if (!entry.Value.isHit)
            {
                Material mat = entry.Value.mat;
                float scaleVelocity = entry.Value.scaleVelocity;
                float scale = Mathf.SmoothDamp(mat.GetFloat(SizeID), 0.0f, ref scaleVelocity, scaleEaseTime);
                mat.SetFloat(SizeID, scale);

                materialsMap[entry.Key] = (materialsMap[entry.Key].mat, false, scaleVelocity);
            }
            else
            {
                materialsMap[entry.Key] = (materialsMap[entry.Key].mat, false, entry.Value.scaleVelocity);
            }

            entry.Value.mat.SetVector(PosID, view);
        }
    }
}
