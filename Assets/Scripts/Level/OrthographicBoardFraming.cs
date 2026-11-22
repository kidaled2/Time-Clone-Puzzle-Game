using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrthographicBoardFraming : MonoBehaviour
{
    [SerializeField, Min(1f)] private float minimumWidth = 7.8f;
    private Camera boardCamera;
    private float minimumSize;

    private void Awake()
    {
        boardCamera = GetComponent<Camera>();
        minimumSize = boardCamera.orthographicSize;
    }

    private void LateUpdate()
    {
        boardCamera.orthographicSize = Mathf.Max(minimumSize, minimumWidth / (2f * boardCamera.aspect));
    }
}
