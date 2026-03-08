using UnityEngine;

/// <summary>
/// Draws an editor gizmo for the player start marker.
/// </summary>
public class PlayerStartPositionGizmo : MonoBehaviour
{
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
        Gizmos.DrawSphere(transform.position, 0.2f);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3.up * 0.5f));
    }
#endif
}
