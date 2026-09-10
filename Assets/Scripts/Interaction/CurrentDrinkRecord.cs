using System;
using UnityEngine;

namespace CupPrototype.Interaction
{
    [DisallowMultipleComponent]
    public sealed class CurrentDrinkRecord : MonoBehaviour
    {
        public bool ProcessIce { get; private set; }
        public event Action Changed;

        public bool TryRecordProcessIce()
        {
            if (!isActiveAndEnabled || ProcessIce) return false;
            ProcessIce = true;
            Changed?.Invoke();
            return true;
        }

        public void ResetAttempt()
        {
            ProcessIce = false;
            Changed?.Invoke();
        }
    }
}
