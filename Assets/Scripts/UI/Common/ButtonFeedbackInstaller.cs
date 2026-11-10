using UnityEngine;

namespace BarracksAndBolts.UI.Common
{
    /// <summary>Sits on the main canvas and gives every button already in the scene a hover and press feel.</summary>
    public sealed class ButtonFeedbackInstaller : MonoBehaviour
    {
        // Start, not Awake: by then the scene's prefabs and the views' own buttons all exist.
        private void Start()
        {
            ButtonFeedback.AddToChildren(transform);
        }
    }
}
