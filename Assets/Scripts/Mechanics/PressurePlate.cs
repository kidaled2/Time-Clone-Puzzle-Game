using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [SerializeField] private string plateId = "Plate_1";
    [SerializeField] private MeshRenderer plateRenderer;
    [SerializeField] private SlidingDoor pairedDoor;

    private static readonly Color ColorNeutral = new Color(0.54f, 0.54f, 0.60f);
    private static readonly Color ColorActive = new Color(1.00f, 0.84f, 0.00f);

    public bool IsActivated { get; private set; }
    public string PlateId => plateId;

    private readonly HashSet<string> actorsOnPlate = new HashSet<string>();
    private Coroutine colorCoroutine;

    private void Awake()
    {
        if (plateRenderer == null)
        {
            plateRenderer = GetComponent<MeshRenderer>();
        }

        if (plateRenderer != null)
        {
            plateRenderer.material.color = ColorNeutral;
        }
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

        colorCoroutine = StartCoroutine(LerpColor(IsActivated ? ColorActive : ColorNeutral));
    }

    private IEnumerator LerpColor(Color target)
    {
        if (plateRenderer == null)
        {
            yield break;
        }

        Color start = plateRenderer.material.color;
        float elapsed = 0f;
        const float duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            plateRenderer.material.color = Color.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        plateRenderer.material.color = target;
        colorCoroutine = null;
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

        if (plateRenderer != null)
        {
            plateRenderer.material.color = ColorNeutral;
        }
    }
}
