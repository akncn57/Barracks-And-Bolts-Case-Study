using System;
using DG.Tweening;
using UnityEngine;

namespace BarracksAndBolts.Popup
{
    public enum PopupAnimationType
    {
        None,
        Fade,
        Scale,
        Punch,
        SlideFromBottom,
        SlideFromTop,
        SlideFromLeft,
        SlideFromRight,
    }

    [Serializable]
    public struct PopupAnimationData
    {
        public PopupAnimationType Type;
        public float Duration;
        public Ease Ease;
        // Slide: offset in pixels (default 300), Punch: strength (default 0.2)
        [Min(0f)] public float Magnitude;
    }

    [CreateAssetMenu(fileName = "NewPopupData", menuName = "Barracks & Bolts/UI/Popup Data")]
    public class PopupData : ScriptableObject
    {
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField] public PopupAnimationData OpenAnimation { get; private set; }
        [field: SerializeField] public PopupAnimationData CloseAnimation { get; private set; }
        // When true, a darkened overlay is placed behind this popup to block interaction with everything beneath it.
        [field: SerializeField] public bool IsModal { get; private set; }

        // Tapping the dark overlay closes the popup, the same as pressing its
        // close button. On by default: nearly every popup already has a close
        // button, so this only makes the way out easier to find.
        //
        // Turn it off where a stray tap would cost the player something they
        // cannot get back — a half-written message, for instance.
        [field: SerializeField] public bool CloseOnBlockerTap { get; private set; } = true;
    }
}
