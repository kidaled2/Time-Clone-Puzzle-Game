using System.Collections;
using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [Header("Door Panels")]
    [SerializeField] private Transform leftPanel;
    [SerializeField] private Transform rightPanel;

    [Header("Settings")]
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private float openOffset = 1.1f;

    private Vector3 leftClosed;
    private Vector3 rightClosed;
    private bool isOpen;
    private Coroutine slideCoroutine;
    private BoxCollider doorCollider;

    private void Awake()
    {
        if (leftPanel == null)
        {
            Transform foundLeft = transform.Find("Door_Left");
            if (foundLeft != null)
            {
                leftPanel = foundLeft;
            }
        }

        if (rightPanel == null)
        {
            Transform foundRight = transform.Find("Door_Right");
            if (foundRight != null)
            {
                rightPanel = foundRight;
            }
        }

        if (leftPanel != null)
        {
            leftClosed = leftPanel.localPosition;
        }

        if (rightPanel != null)
        {
            rightClosed = rightPanel.localPosition;
        }

        doorCollider = GetComponent<BoxCollider>();
    }

    /// <summary>
    /// Opens the door and disables blocking collision.
    /// </summary>
    public void Open()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }

        if (leftPanel == null || rightPanel == null)
        {
            return;
        }

        slideCoroutine = StartCoroutine(Slide(
            leftPanel, new Vector3(-openOffset, leftClosed.y, leftClosed.z),
            rightPanel, new Vector3(openOffset, rightClosed.y, rightClosed.z)));
    }

    /// <summary>
    /// Closes the door and enables blocking collision.
    /// </summary>
    public void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        if (doorCollider != null)
        {
            doorCollider.enabled = true;
        }

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }

        if (leftPanel == null || rightPanel == null)
        {
            return;
        }

        slideCoroutine = StartCoroutine(Slide(
            leftPanel, leftClosed,
            rightPanel, rightClosed));
    }

    private IEnumerator Slide(
        Transform lPanel, Vector3 lTarget,
        Transform rPanel, Vector3 rTarget)
    {
        float elapsed = 0f;
        Vector3 lStart = lPanel.localPosition;
        Vector3 rStart = rPanel.localPosition;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.0001f, slideDuration));
            lPanel.localPosition = Vector3.Lerp(lStart, lTarget, t);
            rPanel.localPosition = Vector3.Lerp(rStart, rTarget, t);
            yield return null;
        }

        lPanel.localPosition = lTarget;
        rPanel.localPosition = rTarget;
        slideCoroutine = null;
    }

    /// <summary>
    /// Restores the door to the closed state and re-enables collision.
    /// </summary>
    public void ResetDoor()
    {
        StopAllCoroutines();
        slideCoroutine = null;
        isOpen = false;

        if (leftPanel != null)
        {
            leftPanel.localPosition = leftClosed;
        }

        if (rightPanel != null)
        {
            rightPanel.localPosition = rightClosed;
        }

        if (doorCollider != null)
        {
            doorCollider.enabled = true;
        }
    }
}
