using FracturedChorus.UI;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.Combat.Bootstrap
{
    public static class CombatPoolUnitVisuals
    {
        private const string CatalogResourcePath = "CombatPoolPrefabs/CombatPoolPrefabCatalog";
        private const string AnimatorResourcePrefix = "CombatPoolAnimators/Unit_";
        private const string PrefabFolder = "Assets/FracturedChorus/Prefabs/Enemy Pool";

        private static CombatPoolPrefabCatalogSO _catalog;

        public static bool IsPoolCombatKey(string unitKey) =>
            unitKey == CombatEnemyKeys.Enemy1
            || unitKey == CombatEnemyKeys.Enemy2
            || unitKey == CombatEnemyKeys.Enemy3
            || unitKey == CombatEnemyKeys.Elite1
            || unitKey == CombatEnemyKeys.Elite2
            || unitKey == CombatEnemyKeys.Elite3;

        public static UnitView InstantiatePoolUnit(
            string unitKey,
            Transform parent,
            Vector3 worldPosition,
            int sortingOrder)
        {
            var prefab = LoadPrefab(unitKey);
            if (prefab == null)
            {
                Debug.LogError($"[CombatPool] Prefab missing for '{unitKey}'.");
                return null;
            }

            var unitGo = Object.Instantiate(prefab, worldPosition, Quaternion.identity, parent);
            var spriteRenderer = unitGo.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = sortingOrder;
            }

            var animator = unitGo.GetComponent<Animator>();
            if (animator == null)
            {
                animator = unitGo.AddComponent<Animator>();
            }

            var controller = Resources.Load<RuntimeAnimatorController>(AnimatorResourcePrefix + unitKey);
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                animator.Rebind();
                animator.Update(0f);
            }

            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;

            var view = unitGo.GetComponent<UnitView>();
            if (view == null)
            {
                view = unitGo.AddComponent<UnitView>();
            }

            UnitSpriteSimulator.EnsureOn(view);
            return view;
        }

        public static void SnapSpawnedUnitToCell(UnitView view, Vector3 cellWorld)
        {
            if (view == null)
            {
                return;
            }

            view.EnsureInteractionColliders();
            view.SnapFeetTo(cellWorld, cellWorld.z);
            view.CaptureAnchor();
        }

        public static void PlayIdle(UnitView view, string unitKey)
        {
            if (view == null)
            {
                return;
            }

            view.EnsureDefaultCombatAnimStates();
            var animator = view.GetComponent<Animator>();
            var idle = ResolveIdleStateName(unitKey);
            if (animator == null
                || animator.runtimeAnimatorController == null
                || string.IsNullOrEmpty(idle))
            {
                return;
            }

            animator.Play(idle, 0, 0f);
            animator.Update(0f);
        }

        public static GameObject LoadPrefab(string unitKey)
        {
            if (!IsPoolCombatKey(unitKey))
            {
                return null;
            }

#if UNITY_EDITOR
            var fromEditor = LoadPrefabFromAssetDatabase(unitKey);
            if (fromEditor != null)
            {
                return fromEditor;
            }
#endif
            if (_catalog == null)
            {
                _catalog = Resources.Load<CombatPoolPrefabCatalogSO>(CatalogResourcePath);
            }

            return _catalog != null ? _catalog.GetPrefab(unitKey) : null;
        }

        private static string ResolveIdleStateName(string unitKey)
        {
            switch (unitKey)
            {
                case CombatEnemyKeys.Enemy1:
                    return "Enemy 1 - Idle";
                case CombatEnemyKeys.Enemy2:
                    return "Enemy 2 - Idle";
                case CombatEnemyKeys.Enemy3:
                    return "Enemy 3 - Idle";
                case CombatEnemyKeys.Elite1:
                    return "Elite 1 -Idle Sprite";
                case CombatEnemyKeys.Elite2:
                    return "Elite 2 - Idle Sprite";
                case CombatEnemyKeys.Elite3:
                    return "Elite 3 - Idle";
                default:
                    return null;
            }
        }

#if UNITY_EDITOR
        private static GameObject LoadPrefabFromAssetDatabase(string unitKey)
        {
            var path = PrefabAssetPath(unitKey);
            return string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static string PrefabAssetPath(string unitKey)
        {
            switch (unitKey)
            {
                case CombatEnemyKeys.Enemy1:
                    return $"{PrefabFolder}/Enemy 1 - Pink_Shoes.prefab";
                case CombatEnemyKeys.Enemy2:
                    return $"{PrefabFolder}/Enemy 2 - Everything_There_of_an_Inquisitor.prefab";
                case CombatEnemyKeys.Enemy3:
                    return $"{PrefabFolder}/Enemy 3 - Whale_of_the_Porous_Hand_Mermaid.prefab";
                case CombatEnemyKeys.Elite1:
                    return $"{PrefabFolder}/Elite 1 - Shock_Centipede.prefab";
                case CombatEnemyKeys.Elite2:
                    return $"{PrefabFolder}/Elite 2 - Cassetti.prefab";
                case CombatEnemyKeys.Elite3:
                    return $"{PrefabFolder}/Elite 3 - Don.prefab";
                default:
                    return null;
            }
        }
#endif
    }
}
