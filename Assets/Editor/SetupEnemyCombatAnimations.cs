using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupEnemyCombatAnimations
{
    [MenuItem("Tools/Berserk/Setup Enemy Combat Animations")]
    public static void Setup()
    {
        string controllerPath = "Assets/Fantasy knight/enemy.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError("Could not find enemy.controller at " + controllerPath);
            return;
        }

        // 1. Ensure parameters
        EnsureParameter(controller, "block", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "attackIndex", AnimatorControllerParameterType.Int);

        // 2. Load Clips
        AnimationClip blockClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Shield_Block_Hit_1_InPlace.anim");
        AnimationClip attack1Clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_2_InPlace.anim");
        AnimationClip attack2Clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim");

        AnimatorStateMachine rootSm = controller.layers[0].stateMachine;

        // Find existing states
        AnimatorState runState = FindState(rootSm, "run");
        AnimatorState knockDownState = FindState(rootSm, "knock_down");
        AnimatorState attack0State = FindState(rootSm, "attack0");

        // 3. Setup block state
        AnimatorState blockState = FindState(rootSm, "block");
        if (blockState == null)
        {
            blockState = rootSm.AddState("block", new Vector3(300, -80, 0));
        }
        if (blockClip != null) blockState.motion = blockClip;

        // Transition AnyState -> block
        bool hasAnyToBlock = false;
        foreach (var trans in rootSm.anyStateTransitions)
        {
            if (trans.destinationState == blockState) { hasAnyToBlock = true; break; }
        }
        if (!hasAnyToBlock)
        {
            var anyToBlock = rootSm.AddAnyStateTransition(blockState);
            anyToBlock.AddCondition(AnimatorConditionMode.If, 0, "block");
            anyToBlock.duration = 0.1f;
            anyToBlock.canTransitionToSelf = false;
        }

        // Transition block -> run (exit time)
        bool hasBlockToRun = false;
        foreach (var trans in blockState.transitions)
        {
            if (trans.destinationState == runState) { hasBlockToRun = true; break; }
        }
        if (!hasBlockToRun && runState != null)
        {
            var blockToRun = blockState.AddTransition(runState);
            blockToRun.hasExitTime = true;
            blockToRun.exitTime = 0.85f;
            blockToRun.duration = 0.2f;
        }

        // 4. Update attack0 transition to check attackIndex == 0
        if (runState != null && attack0State != null)
        {
            foreach (var trans in runState.transitions)
            {
                if (trans.destinationState == attack0State)
                {
                    bool hasIdx = false;
                    foreach (var c in trans.conditions) if (c.parameter == "attackIndex") hasIdx = true;
                    if (!hasIdx) trans.AddCondition(AnimatorConditionMode.Equals, 0, "attackIndex");
                }
            }
        }

        // 5. Setup attack1 (Heavy Overhead Cleave)
        AnimatorState attack1State = FindState(rootSm, "attack1");
        if (attack1State == null)
        {
            attack1State = rootSm.AddState("attack1", new Vector3(560, 200, 0));
        }
        if (attack1Clip != null) attack1State.motion = attack1Clip;

        if (runState != null)
        {
            bool hasRunToAtk1 = false;
            foreach (var trans in runState.transitions)
            {
                if (trans.destinationState == attack1State) { hasRunToAtk1 = true; break; }
            }
            if (!hasRunToAtk1)
            {
                var runToAtk1 = runState.AddTransition(attack1State);
                runToAtk1.AddCondition(AnimatorConditionMode.If, 0, "attack");
                runToAtk1.AddCondition(AnimatorConditionMode.Equals, 1, "attackIndex");
                runToAtk1.duration = 0.2f;
                runToAtk1.hasExitTime = false;
            }
        }

        bool hasAtk1ToRun = false;
        foreach (var trans in attack1State.transitions)
        {
            if (trans.destinationState == runState) { hasAtk1ToRun = true; break; }
        }
        if (!hasAtk1ToRun && runState != null)
        {
            var atk1ToRun = attack1State.AddTransition(runState);
            atk1ToRun.hasExitTime = true;
            atk1ToRun.exitTime = 0.85f;
            atk1ToRun.duration = 0.25f;
        }

        // 6. Setup attack2 (Lunging Gap-Closer)
        AnimatorState attack2State = FindState(rootSm, "attack2");
        if (attack2State == null)
        {
            attack2State = rootSm.AddState("attack2", new Vector3(560, 270, 0));
        }
        if (attack2Clip != null) attack2State.motion = attack2Clip;

        if (runState != null)
        {
            bool hasRunToAtk2 = false;
            foreach (var trans in runState.transitions)
            {
                if (trans.destinationState == attack2State) { hasRunToAtk2 = true; break; }
            }
            if (!hasRunToAtk2)
            {
                var runToAtk2 = runState.AddTransition(attack2State);
                runToAtk2.AddCondition(AnimatorConditionMode.If, 0, "attack");
                runToAtk2.AddCondition(AnimatorConditionMode.Equals, 2, "attackIndex");
                runToAtk2.duration = 0.2f;
                runToAtk2.hasExitTime = false;
            }
        }

        bool hasAtk2ToRun = false;
        foreach (var trans in attack2State.transitions)
        {
            if (trans.destinationState == runState) { hasAtk2ToRun = true; break; }
        }
        if (!hasAtk2ToRun && runState != null)
        {
            var atk2ToRun = attack2State.AddTransition(runState);
            atk2ToRun.hasExitTime = true;
            atk2ToRun.exitTime = 0.85f;
            atk2ToRun.duration = 0.25f;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("<color=green><b>[Berserk] Successfully configured Enemy Block & Multi-Attack Controller!</b></color>");
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == name) return;
        }
        controller.AddParameter(name, type);
    }

    private static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        foreach (var childState in sm.states)
        {
            if (childState.state.name == name) return childState.state;
        }
        return null;
    }
}
