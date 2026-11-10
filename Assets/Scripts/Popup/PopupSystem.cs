using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace BarracksAndBolts.Popup
{
    public class PopupSystem : MonoBehaviour
    {
        [Tooltip("Parent transform inside the Canvas where popup prefabs are spawned.")]
        [SerializeField] private Transform popupContainer;

        [Tooltip("Colour of the fullscreen blocker placed behind modal popups.")]
        [SerializeField] private Color blockerColor = new Color(0f, 0f, 0f, 0.4f);

        private IObjectResolver _resolver;

        private readonly Dictionary<PopupBase, PopupData> _openPopups = new();
        // Each modal popup gets its own blocker so stacked modals each clean up correctly.
        private readonly Dictionary<PopupBase, GameObject> _blockers = new();

        // Opening order. The dictionaries above answer "does this popup have a
        // blocker"; only a list can answer "which one is the player looking at".
        private readonly List<PopupBase> _stack = new();

        private readonly Vector3[] _corners = new Vector3[4];

        // True while at least one modal popup is open — use this to block non-UI input (e.g. camera drag).
        public bool HasModalOpen => _blockers.Count > 0;

        // How many popups are open right now, stacked ones included.
        public int OpenCount => _openPopups.Count;

        // Raised after a popup is spawned and when it starts closing. Hook sounds or analytics here,
        // so the popup module stays unaware of them.
        public event Action<PopupBase> Opened;
        public event Action<PopupBase> Closed;

        [Inject]
        private void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public PopupBase Open(PopupData data) => Open<PopupBase>(data);

        public T Open<T>(PopupData data) where T : PopupBase
        {
            // Blocker is added first so it sits behind the popup in sibling order.
            GameObject blocker = null;
            if (data.IsModal)
                blocker = CreateBlocker();

            // Whatever was on screen steps aside before the new one arrives.
            Cover(Top(), true);

            // Instantiate through the container so the popup and its children get their [Inject] methods.
            var popup = _resolver.Instantiate(data.Prefab, popupContainer).GetComponent<T>();
            _openPopups[popup] = data;
            _stack.Add(popup);

            if (blocker != null)
            {
                _blockers[popup] = blocker;

                if (data.CloseOnBlockerTap) CloseOnTap(blocker, popup);
            }

            popup.PlayOpenAnimation(data.OpenAnimation);
            popup.OnOpened();
            Opened?.Invoke(popup);

            return popup;
        }

        public void Close(PopupBase popup)
        {
            if (popup == null || !_openPopups.TryGetValue(popup, out var data)) return;

            _openPopups.Remove(popup);

            var wasOnTop = Top() == popup;

            _stack.Remove(popup);

            // Only the popup the player was actually looking at uncovers the
            // one beneath it. Closing a buried popup changes nothing on screen.
            if (wasOnTop) Cover(Top(), false);

            Closed?.Invoke(popup);

            // Remove the blocker immediately so the background is interactive again.
            if (_blockers.TryGetValue(popup, out var blocker))
            {
                _blockers.Remove(popup);
                if (blocker != null) Destroy(blocker);
            }

            popup.PlayCloseAnimation(data.CloseAnimation, () =>
            {
                popup.OnClosed();
                if (popup != null) Destroy(popup.gameObject);
            });
        }

        // Closes the popup the player is looking at, as its own close button would. For the Esc key.
        // Returns false when no popup is open.
        public bool CloseTop()
        {
            var top = Top();
            if (top == null) return false;

            top.RequestClose();
            return true;
        }

        private PopupBase Top() => _stack.Count == 0 ? null : _stack[_stack.Count - 1];

        // Hides or restores a popup together with its own dark overlay.
        //
        // THE OVERLAY GOES WITH IT, so only one dimming layer is ever on screen.
        // Leaving it behind stacked a second wash of black under every new
        // popup and the screen kept getting darker the deeper the player went.
        //
        // THE POPUP IS NOT DESTROYED, only made invisible and untouchable, so
        // it comes back exactly as it was left.
        private void Cover(PopupBase popup, bool covered)
        {
            if (popup == null) return;

            popup.SetHiddenBehind(covered);

            if (_blockers.TryGetValue(popup, out var blocker) && blocker != null)
                blocker.SetActive(!covered);
        }

        // Makes the dark overlay close the popup when tapped.
        //
        // The listener is added after the popup exists, because the blocker is
        // created first so it lands behind the popup in sibling order.
        //
        // Stacked modals need no special care: each popup brings its own
        // blocker and the topmost one is the one that receives the tap.
        private static void CloseOnTap(GameObject blocker, PopupBase popup)
        {
            var button = blocker.AddComponent<Button>();

            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                if (popup != null) popup.RequestClose();
            });
        }

        // Creates a fullscreen transparent image that blocks all raycasts behind it.
        // Blocker covers the whole screen, not just the safe area. The popup
        // container sits under the safe-area fitter, so stretching to the
        // parent left the notch strip and the home-indicator strip bright
        // while everything between them went dark.
        private GameObject CreateBlocker()
        {
            var go = new GameObject("ModalBlocker", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(popupContainer, false);

            var rt = go.GetComponent<RectTransform>();
            var parent = popupContainer as RectTransform;
            var screen = ScreenRectIn(parent);

            // Stretched to the parent and pushed out to the screen edges by offsets, not a fixed size:
            // when the canvas is rescaled while the popup is open (the UI Size setting), the blocker
            // keeps covering the whole screen instead of staying at its old size.
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);

            if (screen.HasValue)
            {
                rt.offsetMin = screen.Value.min - parent.rect.min;
                rt.offsetMax = screen.Value.max - parent.rect.max;
            }
            else
            {
                // No canvas above us: fall back to filling the parent. Better a
                // blocker that stops at the safe area than none at all.
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            var img = go.GetComponent<Image>();
            img.color = blockerColor;
            img.raycastTarget = true;

            return go;
        }

        // The whole screen, in the given transform's own coordinates. Measured
        // from the root canvas rather than written as an inset, so it holds on
        // any notch and any aspect ratio.
        private Rect? ScreenRectIn(RectTransform space)
        {
            if (space == null) return null;

            var canvas = space.GetComponentInParent<Canvas>();
            var root = canvas == null ? null : canvas.rootCanvas.transform as RectTransform;

            if (root == null) return null;

            root.GetWorldCorners(_corners);

            var min = space.InverseTransformPoint(_corners[0]);
            var max = space.InverseTransformPoint(_corners[2]);

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
