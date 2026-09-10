using System;
using UnityEngine;

namespace CupPrototype.Interaction
{
    [DisallowMultipleComponent]
    public sealed class CurrentDrinkRecord : MonoBehaviour
    {
        public bool ProcessIce { get; private set; }
        public bool ShakePerformed { get; private set; }
        public bool Tasted { get; private set; }
        public event Action Changed;

        public void RecordTaste() { Tasted = true; Changed?.Invoke(); }
        public void ClearShake() { ShakePerformed = false; Changed?.Invoke(); }

        public void RecordShake()
        {
            ShakePerformed = true;
            Changed?.Invoke();
        }

        public bool TryRecordProcessIce()
        {
            if (!isActiveAndEnabled || ProcessIce) return false;
            ProcessIce = true;
            ShakePerformed = false;
            Changed?.Invoke();
            return true;
        }

        public void ResetAttempt()
        {
            ProcessIce = false;
            ShakePerformed = false;
            Tasted = false;
            Changed?.Invoke();
        }
    }
}
