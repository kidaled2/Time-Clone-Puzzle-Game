using System.Collections;
using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [Header("Visual Variant")]
    [SerializeField] private MechanicColorVariant colorVariant = MechanicColorVariant.Cyan;
    [SerializeField] private MeshRenderer[] panelRenderers;
    [SerializeField] private MeshRenderer[] accentRenderers;
    [SerializeField] private MeshRenderer[] frameRenderers;

    [Header("Door Panels")]
    [SerializeField] private Transform leftPanel;
    [SerializeField] private Transform rightPanel;

    [Header("Settings")]
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private float openOffset = 0.86f;
    [SerializeField] private float openPanelWidthMultiplier = 0.16f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private float audioVolume = 1f;

    private Vector3 leftClosed;
    private Vector3 rightClosed;
    private Vector3 leftClosedScale;
    private Vector3 rightClosedScale;
    private bool isOpen;
    private Coroutine slideCoroutine;
    private BoxCollider doorCollider;
    private MaterialPropertyBlock propertyBlock;

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
            leftClosedScale = leftPanel.localScale;
        }

        if (rightPanel != null)
        {
            rightClosed = rightPanel.localPosition;
            rightClosedScale = rightPanel.localScale;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        doorCollider = GetComponent<BoxCollider>();

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer >= 0)
        {
            gameObject.layer = wallLayer;
        }

        CacheRenderers();
        ApplyVisuals();
    }

    public void SetVariant(MechanicColorVariant variant)
    {
        colorVariant = variant;
        ApplyVisuals();
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

        if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayDoorOpen(transform.position);
        }

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }

        PlayClip(openClip);

        if (leftPanel == null || rightPanel == null)
        {
            return;
        }

        slideCoroutine = StartCoroutine(Slide(
            leftPanel,
            new Vector3(-openOffset, leftClosed.y, leftClosed.z),
            GetOpenScale(leftClosedScale),
            rightPanel,
            new Vector3(openOffset, rightClosed.y, rightClosed.z),
            GetOpenScale(rightClosedScale)));
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

        PlayClip(closeClip);

        if (leftPanel == null || rightPanel == null)
        {
            return;
        }

        slideCoroutine = StartCoroutine(Slide(
            leftPanel, leftClosed, leftClosedScale,
            rightPanel, rightClosed, rightClosedScale));
    }

    private IEnumerator Slide(
        Transform lPanel, Vector3 lTarget, Vector3 lTargetScale,
        Transform rPanel, Vector3 rTarget, Vector3 rTargetScale)
    {
        float elapsed = 0f;
        Vector3 lStart = lPanel.localPosition;
        Vector3 rStart = rPanel.localPosition;
        Vector3 lStartScale = lPanel.localScale;
        Vector3 rStartScale = rPanel.localScale;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.0001f, slideDuration));
            lPanel.localPosition = Vector3.Lerp(lStart, lTarget, t);
            rPanel.localPosition = Vector3.Lerp(rStart, rTarget, t);
            lPanel.localScale = Vector3.Lerp(lStartScale, lTargetScale, t);
            rPanel.localScale = Vector3.Lerp(rStartScale, rTargetScale, t);
            yield return null;
        }

        lPanel.localPosition = lTarget;
        rPanel.localPosition = rTarget;
        lPanel.localScale = lTargetScale;
        rPanel.localScale = rTargetScale;
        slideCoroutine = null;
    }

    private Vector3 GetOpenScale(Vector3 closedScale)
    {
        return new Vector3(
            closedScale.x * Mathf.Clamp01(openPanelWidthMultiplier),
            closedScale.y,
            closedScale.z);
    }

    private void CacheRenderers()
    {
        if (panelRenderers == null || panelRenderers.Length == 0)
        {
            panelRenderers = GetNamedRenderers("Door_");
        }

        if (accentRenderers == null || accentRenderers.Length == 0)
        {
            accentRenderers = GetNamedRenderers("Accent");
        }

        if (frameRenderers == null || frameRenderers.Length == 0)
        {
            frameRenderers = GetNamedRenderers("Frame", "Pocket");
        }
    }

    private MeshRenderer[] GetNamedRenderers(params string[] nameParts)
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        var matches = new System.Collections.Generic.List<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            foreach (string namePart in nameParts)
            {
                if (renderer.gameObject.name.Contains(namePart))
                {
                    matches.Add(renderer);
                    break;
                }
            }
        }

        return matches.ToArray();
    }

    private void ApplyVisuals()
    {
        propertyBlock ??= new MaterialPropertyBlock();

        ApplyColor(panelRenderers, MechanicColorPalette.GetDoorPanelColor(colorVariant), false);
        ApplyColor(accentRenderers, MechanicColorPalette.GetAccent(colorVariant), true);
        ApplyColor(frameRenderers, MechanicColorPalette.GetFrameColor(), false);
    }

    private void ApplyColor(MeshRenderer[] renderers, Color color, bool emissive)
    {
        if (renderers == null)
        {
            return;
        }

        Color emissionColor = emissive ? color * 0.65f : Color.black;
        emissionColor.a = 1f;

        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            propertyBlock.SetColor(EmissionColorId, emissionColor);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, audioVolume);
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
            leftPanel.localScale = leftClosedScale;
        }

        if (rightPanel != null)
        {
            rightPanel.localPosition = rightClosed;
            rightPanel.localScale = rightClosedScale;
        }

        if (doorCollider != null)
        {
            doorCollider.enabled = true;
        }
    }
}
