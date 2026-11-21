using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TemporalEcho.Tests
{
    internal sealed class GameInputScope : IDisposable
    {
        private readonly InputSettings original;
        private readonly InputSettings testSettings;

        public GameInputScope()
        {
            original = InputSystem.settings;
            testSettings = UnityEngine.Object.Instantiate(original);
            // Batch mode has no focused Game View. Route synthetic devices to the game anyway.
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testSettings;
        }

        public void Dispose()
        {
            InputSystem.settings = original;
            UnityEngine.Object.Destroy(testSettings);
        }
    }
}
