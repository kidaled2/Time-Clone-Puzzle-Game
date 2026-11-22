using System;
using TMPro;
using UnityEngine;

public class ResonanceLock : MonoBehaviour
{
    [Serializable]
    private struct Chord
    {
        public string label;
        public PressurePlate[] plates;
    }

    [SerializeField] private Chord[] sequence;
    [SerializeField] private SlidingDoor gate;
    [SerializeField] private TMP_Text score;

    public int CompletedChords { get; private set; }
    private int displayedCount = -1;

    private void Update()
    {
        if (CompletedChords >= sequence.Length) return;

        Chord target = sequence[CompletedChords];
        int occupied = 0;
        foreach (PressurePlate plate in target.plates)
        {
            if (plate.IsActivated) occupied++;
        }

        if (occupied == target.plates.Length)
        {
            CompletedChords++;
            displayedCount = -1;
            if (CompletedChords == sequence.Length)
            {
                gate.Open();
                score.text = "Exit open";
            }
            return;
        }

        if (occupied == displayedCount) return;
        displayedCount = occupied;
        score.text = $"Hold three {target.label} together   {occupied}/{target.plates.Length}\n" +
                     "Triangles   /   Diamonds   /   Rings";
    }
}
