using UnityEngine;

public class RampTile : MonoBehaviour
{
    [Header("Ramp Settings")]
    [Tooltip("Player's standing Y on the upper floor after using this ramp")]
    [SerializeField] private float exitY = 2.5f;

    [Tooltip("Player's standing Y on the ground floor (base of ramp)")]
    [SerializeField] private float entryY = 0.5f;

    [Tooltip("Required input direction to ascend. Opposite direction descends.")]
    [SerializeField] private Vector2 ascendDirection = Vector2.up;

    public float ExitY => exitY;
    public float EntryY => entryY;

    /// <summary>
    /// Returns true if this input direction uses the ramp (ascending or descending).
    /// </summary>
    public bool CanUse(Vector2 inputDirection) => inputDirection == ascendDirection;

    /// <summary>
    /// Returns the target Y the actor should arrive at after using this ramp.
    /// </summary>
    public float GetTargetY(Vector2 inputDirection)
    {
        if (inputDirection == ascendDirection)
        {
            return exitY;
        }

        return entryY;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.4f);
        Gizmos.DrawCube(transform.position + (Vector3.up * 0.3f), new Vector3(0.9f, 0.6f, 0.9f));

        Gizmos.color = new Color(1f, 0.85f, 0f, 0.9f);
        Vector3 direction = new Vector3(ascendDirection.x, 0f, ascendDirection.y);
        Gizmos.DrawRay(transform.position + (Vector3.up * 0.6f), direction * 0.5f);
    }
#endif
}
