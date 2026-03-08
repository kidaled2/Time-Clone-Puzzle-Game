using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CloneCounterUI : MonoBehaviour
{
    [Header("Icon Settings")]
    [SerializeField] private Transform iconContainer;
    [SerializeField] private GameObject cloneIconPrefab;
    [SerializeField] private int maxDisplayIcons = 2;

    [Header("Colors")]
    [SerializeField] private Color colorAvailable = new Color(0.0f, 1.0f, 1.0f, 0.9f);
    [SerializeField] private Color colorUsed = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private readonly List<Image> icons = new List<Image>();
    private int builtSlots = -1;
    private bool loggedMissingIconContainer;
    private bool loggedMissingIconPrefab;
    private bool loggedMissingImageComponent;

    private void Awake()
    {
        BuildIcons(0);
    }

    public void UpdateCloneDisplay(int currentTurn, int maxTurns)
    {
        int normalizedMaxDisplayIcons = Mathf.Max(0, maxDisplayIcons);
        int effectiveMaxTurns = maxTurns > 0 ? maxTurns : normalizedMaxDisplayIcons + 1;
        int totalSlots = Mathf.Clamp(effectiveMaxTurns - 1, 0, normalizedMaxDisplayIcons);
        if (totalSlots != builtSlots)
        {
            BuildIcons(totalSlots);
        }

        int clonesRemaining = Mathf.Clamp(effectiveMaxTurns - currentTurn, 0, totalSlots);
        for (int i = 0; i < icons.Count; i++)
        {
            int iconFromRight = icons.Count - 1 - i;
            bool isAvailable = iconFromRight < clonesRemaining;
            icons[i].color = isAvailable ? colorAvailable : colorUsed;
        }
    }

    private void BuildIcons(int slotCount)
    {
        if (iconContainer == null)
        {
            if (!loggedMissingIconContainer)
            {
                Debug.LogWarning("[CloneCounterUI] iconContainer is not assigned.");
                loggedMissingIconContainer = true;
            }

            return;
        }

        if (cloneIconPrefab == null)
        {
            if (!loggedMissingIconPrefab)
            {
                Debug.LogWarning("[CloneCounterUI] cloneIconPrefab is not assigned.");
                loggedMissingIconPrefab = true;
            }

            return;
        }

        for (int i = iconContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(iconContainer.GetChild(i).gameObject);
        }

        icons.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            GameObject icon = Instantiate(cloneIconPrefab, iconContainer);
            Image image = icon.GetComponent<Image>();
            if (image != null)
            {
                icons.Add(image);
            }
            else if (!loggedMissingImageComponent)
            {
                Debug.LogWarning("[CloneCounterUI] cloneIconPrefab is missing an Image component.");
                loggedMissingImageComponent = true;
            }
        }

        builtSlots = slotCount;
    }
}
