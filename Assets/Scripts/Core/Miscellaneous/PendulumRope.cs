using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class PendulumRope : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    [SerializeField] private Transform weight;

    [SerializeField] private Vector3 pivotOffset;
    [SerializeField] private Vector3 weightOffset;

    private LineRenderer line;

    private void OnEnable()
    {
        line = GetComponent<LineRenderer>();
        UpdateRope();
    }

    private void LateUpdate()
    {
        UpdateRope();
    }

    private void OnValidate()
    {
        UpdateRope();
    }

    private void UpdateRope()
    {
        if (line == null)
            line = GetComponent<LineRenderer>();

        if (pivot == null || weight == null)
            return;

        line.positionCount = 2;

        Vector3 pivotPosition = pivot.TransformPoint(pivotOffset);

        Vector3 weightPosition = weight.TransformPoint(weightOffset);

        line.SetPosition(0, pivotPosition);
        line.SetPosition(1, weightPosition);
    }
}