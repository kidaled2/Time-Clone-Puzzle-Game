using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemporalEcho.Tests
{
    internal sealed class GameInputScope : IDisposable
    {
        private readonly InputSettings original;
        private readonly InputSettings.BackgroundBehavior originalBackgroundBehavior;
        private readonly InputSettings.EditorInputBehaviorInPlayMode originalEditorBehavior;

        public GameInputScope()
        {
            original = InputSystem.settings;
            originalBackgroundBehavior = original.backgroundBehavior;
            originalEditorBehavior = original.editorInputBehaviorInPlayMode;
            // Batch mode has no focused Game View. Route synthetic devices to the game anyway.
            // Keep the same settings object: Unity's reload snapshot can retain a temporary
            // clone after it is destroyed, breaking InputSystem initialization on the next run.
            original.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            original.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }

        public void Dispose()
        {
            original.backgroundBehavior = originalBackgroundBehavior;
            original.editorInputBehaviorInPlayMode = originalEditorBehavior;
            InputSystem.settings = original;
        }
    }
}
