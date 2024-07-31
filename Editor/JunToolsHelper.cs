using System;
using System.Collections.Generic;
using System.Drawing.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using VRC.SDKBase;


/*
Helper class with basic tools that I'll use in other scripts
*/

public static class JunToolsHelper{
    //multiplied number for placement on the animator graph
    public static int positionFactor = 200;
    //returns index to layer, -1 if null
    public static int getAnimatorLayerIndex(AnimatorController _animator,string _layerName){
        for(int i=0; i< _animator.layers.Length;i++){
            if(_animator.layers[i].name == _layerName){
                return i;
            }
        }

        return -1;
    }
    //returns index to parameter, -1 if null
    public static int getAnimatorParameterIndex(AnimatorController _animator, string _parameterName){
        for(int i=0; i< _animator.parameters.Length;i++){
            if(_animator.parameters[i].name == _parameterName){
                return i;
            }
        }

        return -1;
    }
    //returns index to state, -1 if null
    public static int getStateIndex(AnimatorStateMachine _stateMachine, string _name){
        for(int i=0;i<_stateMachine.states.Length;i++){
            if(_stateMachine.states[i].state.name == _name){
                return i;
            }
        }

        return -1;
    }
    
    public static void formatTransition(AnimatorStateTransition _trans){
        _trans.duration = 0;
        _trans.exitTime = 0;
        _trans.hasExitTime = false;
        _trans.hasFixedDuration = true;
    }
    //use basically only when creating new layers
    public static void saveToParent(UnityEngine.Object _child, UnityEngine.Object _parent){
        try
        {
            AssetDatabase.AddObjectToAsset(_child,AssetDatabase.GetAssetPath(_parent));
            EditorUtility.SetDirty(_child);
            _child.hideFlags = HideFlags.HideInHierarchy;
        }
        catch (System.Exception)
        {
            Debug.Log("Failed to save to Parent");
        }

    }

    //adds parameter to animator, returns index to parameter
    public static int addParameterToAnimator(AnimatorController _animator,string _name,AnimatorControllerParameterType _type){
        int parameterIndex = getAnimatorParameterIndex(_animator, _name);

        //double check that it's not there already
        if(parameterIndex != -1){
            //double check that it's the correct type
            if(_animator.parameters[parameterIndex].type == _type){
                return parameterIndex; //return if exists
            } else{
                //if wrong type, delete this one
                _animator.RemoveParameter(parameterIndex);
            }
        }

        //add parameter
        _animator.AddParameter(_name,_type);
        parameterIndex = getAnimatorParameterIndex(_animator, _name);

        return parameterIndex;
    }
    
    //Adds layer to animator, returns index to layer
    public static int addLayerToAnimator(AnimatorController _animator,string _name){
        int layerIndex = getAnimatorLayerIndex(_animator, _name);
        if(layerIndex != -1){
            return layerIndex;
        }

        var layer = new AnimatorControllerLayer{
            name = _name,
            defaultWeight = 1f,
            stateMachine = new AnimatorStateMachine()
        };

        layer.stateMachine.name = _name;
        _animator.AddLayer(layer);

        //LAYERS NEED TO BE SAVED OTHERWISE UNITY WILL NOT LIKE ME ANYMORE
        saveToParent(layer.stateMachine,_animator);

        layerIndex = getAnimatorLayerIndex(_animator,_name);
        return layerIndex;
    }

    public static AnimatorStateMachine CreateFormattedStateMachine(AnimatorStateMachine _parentStateMachine, string _name, Vector3 _position){
        AnimatorStateMachine newStateMachine = _parentStateMachine.AddStateMachine(_name,_position);

        newStateMachine.parentStateMachinePosition = new Vector3(0,0);
        newStateMachine.entryPosition = new Vector3(0,(1*positionFactor));
        newStateMachine.anyStatePosition = new Vector3(0,(-1*positionFactor));
        newStateMachine.exitPosition = new Vector3((4*positionFactor),0);

        return newStateMachine;
    }
}