using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private string plateId = "Plate_1";

    [Header("Visual Variant")]
    [SerializeField] private MechanicColorVariant colorVariant = MechanicColorVariant.Cyan;
    [SerializeField] private bool deriveVariantFromId = true;

    [Header("Visual Renderers")]
    [SerializeField] private MeshRenderer plateRenderer;
    [SerializeField] private MeshRenderer baseRenderer;
    [SerializeField] private MeshRenderer[] accentRenderers;

    [Header("Door Pairing")]
    [SerializeField] private SlidingDoor pairedDoor;

    public bool IsActivated { get; private set; }
    public string PlateId => plateId;

    private readonly HashSet<string> actorsOnPlate = new HashSet<string>();
    private Coroutine colorCoroutine;
    private Color currentSurfaceColor;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        if (deriveVariantFromId)
        {
            colorVariant = MechanicColorPalette.InferFromId(plateId);
        }

        if (plateRenderer == null)
        {
            Transform surface = transform.Find("Plate_Surface");
            plateRenderer = surface != null ? surface.GetComponent<MeshRenderer>() : GetComponent<MeshRenderer>();
        }

        if (baseRenderer == null)
        {
            baseRenderer = GetComponent<MeshRenderer>();
        }

        if (accentRenderers == null || accentRenderers.Length == 0)
        {
            accentRenderers = GetNamedRenderers("Accent");
        }

        if (pairedDoor != null)
        {
            pairedDoor.SetVariant(colorVariant);
        }

        propertyBlock = new MaterialPropertyBlock();
        ApplyStaticVisuals();
        SetSurfaceColor(MechanicColorPalette.GetPlateInactiveColor(colorVariant));
    }

    private void OnTriggerEnter(Collider other)
    {
        IActorTag actor = other.GetComponent<IActorTag>();
        if (actor == null)
        {
            return;
        }

        actorsOnPlate.Add(actor.ActorId);
        EvaluateState();
    }

    private void OnTriggerExit(Collider other)
    {
        IActorTag actor = other.GetComponent<IActorTag>();
        if (actor == null)
        {
            return;
        }

        actorsOnPlate.Remove(actor.ActorId);
        EvaluateState();
    }

    private void EvaluateState()
    {
        bool shouldBeActive = actorsOnPlate.Count > 0;
        if (shouldBeActive == IsActivated)
        {
            return;
        }

        IsActivated = shouldBeActive;

        if (IsActivated && VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayPressurePlateActivate(transform.position);
        }

        if (pairedDoor != null)
        {
            if (IsActivated)
            {
                pairedDoor.Open();
            }
            else
            {
                pairedDoor.Close();
            }
        }

        if (plateRenderer == null)
        {
            return;
        }

        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
        }

        Color target = IsActivated
            ? MechanicColorPalette.GetPlateActiveColor(colorVariant)
            : MechanicColorPalette.GetPlateInactiveColor(colorVariant);

        colorCoroutine = StartCoroutine(LerpColor(target));
    }

    private IEnumerator LerpColor(Color target)
    {
        if (plateRenderer == null)
        {
            yield break;
        }

        Color start = currentSurfaceColor;
        float elapsed = 0f;
        const float duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetSurfaceColor(Color.Lerp(start, target, elapsed / duration));
            yield return null;
        }

        SetSurfaceColor(target);
        colorCoroutine = null;
    }

    private MeshRenderer[] GetNamedRenderers(string namePart)
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        var matches = new System.Collections.Generic.List<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.gameObject.name.Contains(namePart))
            {
                matches.Add(renderer);
            }
        }

        return matches.ToArray();
    }

    private void ApplyStaticVisuals()
    {
        SetRendererColor(baseRenderer, MechanicColorPalette.GetFrameColor(), false);

        Color accent = MechanicColorPalette.GetAccent(colorVariant);
        if (accentRenderers == null)
        {
            return;
        }

        foreach (MeshRenderer renderer in accentRenderers)
        {
            SetRendererColor(renderer, accent, true);
        }
    }

    private void SetSurfaceColor(Color color)
    {
        currentSurfaceColor = color;
        SetRendererColor(plateRenderer, color, true);
    }

    private void SetRendererColor(MeshRenderer renderer, Color color, bool emissive)
    {
        if (renderer == null)
        {
            return;
        }

        propertyBlock ??= new MaterialPropertyBlock();
        Color emissionColor = emissive ? color * 0.5f : Color.black;
        emissionColor.a = 1f;

        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, color);
        propertyBlock.SetColor(ColorId, color);
        propertyBlock.SetColor(EmissionColorId, emissionColor);
        renderer.SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// Restores the plate to its default inactive state.
    /// </summary>
    public void ResetPlate()
    {
        actorsOnPlate.Clear();
        IsActivated = false;

        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
            colorCoroutine = null;
        }

        ApplyStaticVisuals();
        SetSurfaceColor(MechanicColorPalette.GetPlateInactiveColor(colorVariant));
    }
}
