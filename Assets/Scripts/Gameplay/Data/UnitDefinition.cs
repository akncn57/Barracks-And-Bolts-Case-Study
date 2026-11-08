using UnityEngine;

namespace BarracksAndBolts.Gameplay.Data
{
    [CreateAssetMenu(fileName = "Data_Unit_New", menuName = "Barracks & Bolts/Data/Unit")]
    public sealed class UnitDefinition : ScriptableObject
    {
        // The spearman reaches 1.5 cells and still fights up close.
        private const float MeleeRangeLimit = 2f;

        [Tooltip("Stable key for save files. Don't change it once saves exist.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite portrait;

        [Header("Stats")]
        [SerializeField, Min(1)] private int maxHp = 10;
        [SerializeField, Min(0)] private int damage = 1;
        [Tooltip("In cells. 1 means melee.")]
        [SerializeField, Min(1f)] private float attackRange = 1f;
        [Tooltip("Seconds between two attacks.")]
        [SerializeField, Min(0.1f)] private float attackCooldown = 1f;
        [Tooltip("Cells per second.")]
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.5f;
        [SerializeField, Min(0)] private int populationCost = 1;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Portrait => portrait;
        public int MaxHp => maxHp;
        public int Damage => damage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;

        /// <summary>Fires projectiles instead of striking up close.</summary>
        public bool IsRanged => attackRange > MeleeRangeLimit;

        public float MoveSpeed => moveSpeed;
        public int PopulationCost => populationCost;

        private void OnValidate()
        {
            // [Min] only guards the inspector; this also catches values edited in the asset file.
            maxHp = Mathf.Max(maxHp, 1);
            damage = Mathf.Max(damage, 0);
            attackRange = Mathf.Max(attackRange, 1f);
            attackCooldown = Mathf.Max(attackCooldown, 0.1f);
            moveSpeed = Mathf.Max(moveSpeed, 0.1f);
            populationCost = Mathf.Max(populationCost, 0);

            if (string.IsNullOrWhiteSpace(id))
                Debug.LogWarning($"{name}: id is empty.", this);
        }
    }
}
