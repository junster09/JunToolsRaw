using System;
using System.Collections.Generic;
using System.Drawing.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using VRC.SDKBase;
public class JunToolsSanityCheck : EditorWindow
{
    AnimatorController targetAnimator;

    [MenuItem("Tools/JunTools/Animator Sanity Checker")]

    public static void showWindow(){
        GetWindow(typeof(JunToolsSanityCheck));
    }

    /*
    _stateMachine (AnimatorStateMachine)
        -> stateMachines (ChildAnimatorStateMachine)
            -> stateMachines[i].stateMachine
    */

    private void checkEmptyAnimationStateMachine(AnimatorStateMachine _stateMachine){
        //check state machines
        for(int i=0;i<_stateMachine.stateMachines.Length;i++){
            checkEmptyAnimationStateMachine(_stateMachine.stateMachines[i].stateMachine);
        }
        
        //check states
        for(int i=0;i<_stateMachine.states.Length;i++){
            if(_stateMachine.states[i].state.motion == null){
                Debug.Log(_stateMachine.states[i].state.name + " has a NULL motion");
            }
            else{
                Debug.Log(_stateMachine.states[i].state.name + " has motion " + _stateMachine.states[i].state.motion.name);
            };
        };
    }
    private void checkEmptyAnimationStates(){
        //iterate through all layers
        for(int i=0;i<targetAnimator.layers.Length;i++){
            AnimatorStateMachine sm =  targetAnimator.layers[i].stateMachine;
            checkEmptyAnimationStateMachine(sm);
        };
    }

    void OnGUI()
    {
        GUILayout.Label("Sanity Checker",EditorStyles.boldLabel);
        targetAnimator = EditorGUILayout.ObjectField("Animator",targetAnimator,typeof(AnimatorController),false) as AnimatorController;

        if(GUILayout.Button("Check empty animation states")){
            checkEmptyAnimationStates();
        }
    }
}
