using System;

namespace Zinnia.Rule
{
    using UnityEngine;
    using Zinnia.Data.Collection.List;

    /// <summary>
    /// Determines whether a <see cref="GameObject"/>'s <see cref="GameObject.tag"/> is part of a list.
    /// </summary>
    public class StarBaseUpgradeLogicRule : GameObjectRule
    {
        [Tooltip("The parent starbase to check against.")]
        [SerializeField]
        private StarBase starBase;
        
        public StarBase StarBase
        {
            get
            {
                return starBase;
            }
            set
            {
                starBase = value;
            }
        }

        private void Start()
        {
            if (starBase == null)
            {
                Debug.LogWarning("StarBaseUpgradeLogicRule has no starbase assigned. Trying to find one.");
                starBase = GetComponentInParent<StarBase>();
                if (starBase == null)
                {
                    Debug.LogError("StarBaseUpgradeLogicRule has no starbase assigned and none could be found.");
                }
            }
        }

        /// <inheritdoc />
        protected override bool Accepts(GameObject targetGameObject)
        {
            return StarBaseManager.Instance.CanUpgradeBase(StarBase.BaseId);
        }
    }
}