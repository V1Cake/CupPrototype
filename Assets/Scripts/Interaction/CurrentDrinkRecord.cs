using System;
using CupPrototype.Game;
using UnityEngine;

namespace CupPrototype.Interaction
{
    [DisallowMultipleComponent]
    public sealed class CurrentDrinkRecord : MonoBehaviour
    {
        public OrderContext ActiveOrder { get; private set; }

        public void BeginOrder(OrderContext order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            ActiveOrder = order;
            ResetAttempt();
        }

        public bool ProcessIce { get; private set; }
        public bool ShakePerformed { get; private set; }
        public bool HasShaken { get; private set; }
        public bool Tasted { get; private set; }
        public CupPrototype.DrinkSystem.DrinkContainer SelectedGlass { get; private set; }
        public void SelectGlass(CupPrototype.DrinkSystem.DrinkContainer cup)
        {
            SelectedGlass = cup;
            Changed?.Invoke();
        }
        public int PreparationVersion { get; private set; }
        public int LastTastedVersion { get; private set; } = -1;
        public bool CanTasteCurrentVersion => HasShaken && LastTastedVersion != PreparationVersion;
        public string TasteFeedback { get; private set; } = string.Empty;
        public event Action Changed;

        public void RecordTaste(string feedback = null)
        {
            Tasted = true;
            LastTastedVersion = PreparationVersion;
            if (feedback != null) TasteFeedback = feedback;
            Changed?.Invoke();
        }
        public void RecordContentChange()
        {
            PreparationVersion++;
            ShakePerformed = false;
            Changed?.Invoke();
        }

        public void RecordShake()
        {
            ShakePerformed = true;
            HasShaken = true;
            Changed?.Invoke();
        }

        public bool TryRecordProcessIce()
        {
            if (!isActiveAndEnabled || ProcessIce) return false;
            ProcessIce = true;
            RecordContentChange();
            return true;
        }

        public void ResetAttempt()
        {
            ProcessIce = false;
            HasShaken = false;
            ShakePerformed = false;
            Tasted = false;
            SelectedGlass = null;
            PreparationVersion = 0;
            LastTastedVersion = -1;
            TasteFeedback = string.Empty;
            Changed?.Invoke();
        }
    }
}
