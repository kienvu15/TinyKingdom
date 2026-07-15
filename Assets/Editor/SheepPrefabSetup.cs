using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TinyKingdom.Editor
{
    /// <summary>
    /// Explicit, repeatable setup for the existing sheep animation controller
    /// and prefab. It never runs automatically when Unity opens.
    /// </summary>
    public static class SheepPrefabSetup
    {
        private const string ControllerPath = "Assets/Animations/Meat/Sheep.controller";
        private const string PrefabPath = "Assets/Prefabs/Meat/Sheep_Grass.prefab";

        [MenuItem("TinyKingdom/Animals/Configure Sheep Prefab")]
        public static void Configure()
        {
            ConfigureAnimator();
            ConfigurePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TinyKingdom: Sheep prefab and animator configured.");
        }

        private static void ConfigureAnimator()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (controller == null)
            {
                Debug.LogError($"TinyKingdom: Missing sheep controller at {ControllerPath}.");
                return;
            }

            AddBoolParameterIfMissing(controller, "IsMoving");
            AddBoolParameterIfMissing(controller, "IsGrazing");

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = FindState(stateMachine, "Sheep_Idle");
            AnimatorState move = FindState(stateMachine, "Sheep_Move");
            AnimatorState grass = FindState(stateMachine, "Sheep_Grass");

            if (idle == null || move == null || grass == null)
            {
                Debug.LogError("TinyKingdom: Sheep controller is missing one or more required states.");
                return;
            }

            stateMachine.defaultState = idle;

            ReplaceTransition(idle, move, transition =>
            {
                transition.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            });
            ReplaceTransition(idle, grass, transition =>
            {
                transition.AddCondition(AnimatorConditionMode.If, 0f, "IsGrazing");
            });
            ReplaceTransition(move, idle, transition =>
            {
                transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
                transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrazing");
            });
            ReplaceTransition(move, grass, transition =>
            {
                transition.AddCondition(AnimatorConditionMode.If, 0f, "IsGrazing");
            });
            ReplaceTransition(grass, idle, transition =>
            {
                transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrazing");
                transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            });
            ReplaceTransition(grass, move, transition =>
            {
                transition.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            });

            EditorUtility.SetDirty(controller);
        }

        private static void ConfigurePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

            try
            {
                Rigidbody2D body = root.GetComponent<Rigidbody2D>();
                if (body == null)
                {
                    body = root.AddComponent<Rigidbody2D>();
                }

                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = 0f;
                body.freezeRotation = true;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                BoxCollider2D bodyCollider = root.GetComponent<BoxCollider2D>();
                if (bodyCollider == null)
                {
                    bodyCollider = root.AddComponent<BoxCollider2D>();
                }

                // Covers the sheep's feet/body rather than transparent sprite padding.
                bodyCollider.isTrigger = false;
                bodyCollider.offset = new Vector2(0f, -0.65f);
                bodyCollider.size = new Vector2(1.35f, 0.7f);

                if (root.GetComponent<SheepWanderer>() == null)
                {
                    root.AddComponent<SheepWanderer>();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddBoolParameterIfMissing(AnimatorController controller, string name)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == name)
                {
                    return;
                }
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Bool);
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string name)
        {
            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                if (childState.state.name == name)
                {
                    return childState.state;
                }
            }

            return null;
        }

        private static void ReplaceTransition(
            AnimatorState source,
            AnimatorState destination,
            System.Action<AnimatorStateTransition> configureConditions)
        {
            foreach (AnimatorStateTransition transition in source.transitions)
            {
                if (transition.destinationState == destination)
                {
                    source.RemoveTransition(transition);
                }
            }

            AnimatorStateTransition newTransition = source.AddTransition(destination);
            newTransition.hasExitTime = false;
            newTransition.hasFixedDuration = true;
            newTransition.duration = 0.05f;
            newTransition.offset = 0f;
            configureConditions(newTransition);
        }
    }
}
