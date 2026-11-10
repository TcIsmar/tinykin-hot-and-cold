using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace TinykinHotCold.Radar
{
    /// <summary>The radar's own input actions, built from the control paths in the config file.</summary>
    internal sealed class RadarInput : IDisposable
    {
        private readonly InputAction toggle;
        private readonly InputAction switchTarget;

        internal RadarInput(string toggleBindings, string switchBindings)
        {
            toggle = Create("Hot & Cold: toggle radar", toggleBindings);
            switchTarget = Create("Hot & Cold: switch target", switchBindings);
        }

        internal bool TogglePressed => Triggered(toggle);

        internal bool SwitchPressed => Triggered(switchTarget);

        public void Dispose()
        {
            toggle.Dispose();
            switchTarget.Dispose();
        }

        private static InputAction Create(string name, string bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            var paths = new List<string>();
            foreach (string path in (bindings ?? string.Empty).Split(';'))
            {
                if (!string.IsNullOrWhiteSpace(path))
                {
                    paths.Add(path.Trim());
                    action.AddBinding(path.Trim());
                }
            }

            action.Enable();
            Plugin.Log.LogInfo($"{name}: {(paths.Count > 0 ? string.Join(", ", paths) : "(no bindings)")}");
            return action;
        }

        private static bool Triggered(InputAction action)
        {
            if (!action.enabled)
            {
                action.Enable();
            }

            return action.triggered;
        }
    }
}
