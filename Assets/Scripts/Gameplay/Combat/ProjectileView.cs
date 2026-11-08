using System;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Combat
{
    /// <summary>
    /// One arrow in flight. Only for the eye: damage is applied when the archer fires.
    /// The sprite is expected to point right.
    /// </summary>
    public sealed class ProjectileView : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 12f;

        private Vector2 _end;
        private Action<ProjectileView> _onArrived;

        public void Launch(Vector2 start, Vector2 end, Action<ProjectileView> onArrived)
        {
            _end = end;
            _onArrived = onArrived;

            var direction = end - start;
            transform.SetPositionAndRotation(start, Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
        }

        private void Update()
        {
            var position = Vector2.MoveTowards(transform.position, _end, speed * Time.deltaTime);
            transform.position = position;
            if (position == _end)
                _onArrived(this);
        }
    }
}
