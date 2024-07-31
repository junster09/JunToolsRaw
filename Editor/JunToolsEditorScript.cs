using System;
using System.Collections.Generic;
using System.Drawing.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using VRC.SDKBase;
public class JunToolsEditorScript : EditorWindow{
    AnimatorController targetAnimator;
    string emotionVariableName = "EmotionIndex"; //name for emotion variable
    int emotionSetInteger = 0;
    string emotionSetName = "Default";
    bool hasFaceLock = true;
    string leftHandLayerName = "Left Hand Gestures";
    string rightHandLayerName = "Right Hand Gestures";
    AnimationClip noneAnimation;
    AnimationClip[] faceAnimations = new AnimationClip[8];

    int positionFactor = 200;

    [MenuItem("Tools/JunTools/Emotion Set Setup")]
    //I don't know why I have this here
    public static void ShowWindow(){
        GetWindow(typeof(JunToolsEditorScript)); 
    }   

    private void OnGUI(){
        GUILayout.Label("Emotion Set Setup",EditorStyles.boldLabel); //Test Label

        if(GUILayout.Button("Reset Setup")){
            resetToolValues();
        }

        targetAnimator = EditorGUILayout.ObjectField("Animator",targetAnimator,typeof(AnimatorController),false) as AnimatorController;
        emotionVariableName = EditorGUILayout.TextField("Emotion Variable Name",emotionVariableName);

        EditorGUILayout.Space();

        emotionSetName = EditorGUILayout.TextField("Emotion Set Name",emotionSetName);
        emotionSetInteger = EditorGUILayout.IntField("Emotion Set Integer",emotionSetInteger);

        EditorGUILayout.Space();

        hasFaceLock = EditorGUILayout.Toggle("Use face lock",hasFaceLock);

        EditorGUILayout.Space();

        leftHandLayerName = EditorGUILayout.TextField("Left Hand Layer",leftHandLayerName);
        rightHandLayerName = EditorGUILayout.TextField("Right Hand Layer",rightHandLayerName);

        EditorGUILayout.Space();

        GUILayout.Label("Emotion Animations",EditorStyles.boldLabel);

        noneAnimation = EditorGUILayout.ObjectField("Empty Animation ",noneAnimation,typeof(AnimationClip),false) as AnimationClip;

        EditorGUILayout.Space();
        //All this will do is draw the face animation stuff b/c I'm lazy
        string[] animationNames = {"Neutral","Closed Fist","Open Hand","Point","Peace","Rock n Roll","Finger Gun","Thumbs Up"};
        for(int i = 0;i < animationNames.Length;i++){
            faceAnimations[i] = EditorGUILayout.ObjectField(animationNames[i],faceAnimations[i],typeof(AnimationClip),false) as AnimationClip;
        }

        if(GUILayout.Button("Build Animator"))
        {
            BuildAnimator();
            AssetDatabase.SaveAssets();
        };
    }
    private void resetToolValues(){
        targetAnimator = null;
        emotionVariableName = "EmotionIndex";
        emotionSetInteger = 0;
        emotionSetName = "Default";
        hasFaceLock = true;
        leftHandLayerName = "Left Hand Gestures";
        rightHandLayerName = "Right Hand Gestures";
        noneAnimation = null;
        faceAnimations = new AnimationClip[8];
    }
//actually just a layer nuke
    private void removeAllFromLayer(AnimatorStateMachine _stateMachine){
        while(_stateMachine.states.Length > 0){
            _stateMachine.RemoveState(_stateMachine.states[1].state);
        }

        while(_stateMachine.stateMachines.Length > 0){
            _stateMachine.RemoveStateMachine(_stateMachine.stateMachines[1].stateMachine);
        }
    }

    private void BuildAnimator(){
        //Get all refrences required to start building the animator
        //fill in missing animations with "none"
        //fill in missing data

        //fills in the missing animations for the face animations
        for(int i=0;i<faceAnimations.Length;i++){
            if(faceAnimations[i] == null){ faceAnimations[i] = noneAnimation;}
        }

        /*
         required:
            GestureLeft (int)
            GestureRight (int)
            EmotionIndex <or whatever fed> (int)
            face lock (if it's neccessary)
        */
        
        //makes layers if they don't exist
        string[] requiredLayerNames = {leftHandLayerName,rightHandLayerName};
        Dictionary<string,int> layers = new Dictionary<string, int>();

        for(int i =0; i<requiredLayerNames.Length;i++){
            int layerIndex = JunToolsHelper.getAnimatorLayerIndex(targetAnimator,requiredLayerNames[i]);

            if (layerIndex == -1){
                var layer = new AnimatorControllerLayer{
                    name = requiredLayerNames[i],
                    defaultWeight = 1f,
                    stateMachine = new AnimatorStateMachine()
                };
                layer.stateMachine.name = requiredLayerNames[i];
                targetAnimator.AddLayer(layer);
                //LAYERS NEED TO BE SAVED OTHERWISE UNITY WILL NOT LIKE ME ANYMORE
                JunToolsHelper.saveToParent(layer.stateMachine,targetAnimator);

                layerIndex = JunToolsHelper.getAnimatorLayerIndex(targetAnimator,requiredLayerNames[i]);
            }

            layers.Add(requiredLayerNames[i],layerIndex);
        }

        //makes parameters if they don't exist
        string[] requiredParameterNames = {"GestureLeft","GestureRight",emotionVariableName,"FaceLockToggle"};
        AnimatorControllerParameterType[] requiredParameterTypes = {AnimatorControllerParameterType.Int,AnimatorControllerParameterType.Int,AnimatorControllerParameterType.Int,AnimatorControllerParameterType.Bool};

        for(int i = 0; i<requiredParameterNames.Length;i++){
            int parameterIndex = JunToolsHelper.getAnimatorParameterIndex(targetAnimator,requiredParameterNames[i]); //returns -1 for null

            if (parameterIndex != -1){ 
                //check for correct type, if not delete and make a new one
                if(targetAnimator.parameters[parameterIndex].type != requiredParameterTypes[i]){
                    targetAnimator.RemoveParameter(parameterIndex);
                    targetAnimator.AddParameter(requiredParameterNames[i],requiredParameterTypes[i]);

                }
            }else{
                //make a new one
                targetAnimator.AddParameter(requiredParameterNames[i],requiredParameterTypes[i]);
            }
        }


//build left hand layer
        int activeLayerIndex = layers[leftHandLayerName];

        //we are at root state
        AnimatorStateMachine rootStateMachine = targetAnimator.layers[activeLayerIndex].stateMachine;

        rootStateMachine.entryPosition = new Vector3(0,0);
        rootStateMachine.anyStatePosition = new Vector3(0,(-1*positionFactor));
        rootStateMachine.exitPosition = new Vector3((4*positionFactor),0);
        
        //check for Emotion Search state
        int curStateIndex = JunToolsHelper.getStateIndex(rootStateMachine,"Emotion Search");
        AnimatorState EmotionSearchState;
        if(curStateIndex == -1){ //make one
            EmotionSearchState = rootStateMachine.AddState("Emotion Search",new Vector3((1*positionFactor),0));
        }else{//somehow get a refrence to it
            ChildAnimatorState e = rootStateMachine.states[curStateIndex];
            Debug.Log(e.position);
            e.position = new Vector3((1*positionFactor),0);
            EmotionSearchState = e.state;
        }

        //ensure this is setup correctly 
        EmotionSearchState.motion = noneAnimation;
        EmotionSearchState.writeDefaultValues = false;

        //create a new substate for the face set
        AnimatorStateMachine emotionStateMachine = rootStateMachine.AddStateMachine(emotionSetName,new Vector3((2*positionFactor),((emotionSetInteger-1)*positionFactor)));


        emotionStateMachine.parentStateMachinePosition = new Vector3(0,0);
        emotionStateMachine.entryPosition = new Vector3(0,(1*positionFactor));
        emotionStateMachine.anyStatePosition = new Vector3(0,(-1*positionFactor));
        emotionStateMachine.exitPosition = new Vector3((4*positionFactor),0);

        //populate substate

        //create gesture search
        AnimatorState searchState = emotionStateMachine.AddState("Search",new Vector3((1*positionFactor),0));
        searchState.motion = noneAnimation;
        searchState.writeDefaultValues = false;



        //from search -> exit
        AnimatorStateTransition tr = searchState.AddExitTransition();
        JunToolsHelper.formatTransition(tr);
        tr.AddCondition(AnimatorConditionMode.NotEqual,emotionSetInteger,emotionVariableName);

        //from context -> search
        tr = EmotionSearchState.AddTransition(searchState);
        JunToolsHelper.formatTransition(tr);
        tr.AddCondition(AnimatorConditionMode.Equals,emotionSetInteger,emotionVariableName);


        //setup each individual state
        string[] animationNames = {"Neutral","Closed Fist","Open Hand","Point","Peace","Rock n Roll","Finger Gun","Thumbs Up"};
        for(int i = 0; i < animationNames.Length;i++){
        //make the state
            AnimatorState curState = emotionStateMachine.AddState(animationNames[i],
                new Vector3(
                    (2.5f*positionFactor),
                    ((i - (animationNames.Length/2)) * (positionFactor/2))
                )
            );
            
            curState.motion = faceAnimations[i];
            curState.writeDefaultValues = false;


        //make the transitions

            //from search to state
            tr = searchState.AddTransition(curState);
            JunToolsHelper.formatTransition(tr);
            tr.AddCondition(AnimatorConditionMode.Equals,i,"GestureLeft");

            if(hasFaceLock){
                tr.AddCondition(AnimatorConditionMode.IfNot,1,"FaceLockToggle");
            }


            //from state to exit
            tr = curState.AddExitTransition();
            JunToolsHelper.formatTransition(tr);
            tr.AddCondition(AnimatorConditionMode.NotEqual,i,"GestureLeft");

            if(hasFaceLock){
                tr.AddCondition(AnimatorConditionMode.IfNot,1,"FaceLockToggle");
            }


            //from state to exit
            tr = curState.AddExitTransition();
            JunToolsHelper.formatTransition(tr);
            tr.AddCondition(AnimatorConditionMode.NotEqual,emotionSetInteger,emotionVariableName); 


        }

//build right hand layer
        activeLayerIndex = layers[rightHandLayerName];

        //we are at root state
        rootStateMachine = targetAnimator.layers[activeLayerIndex].stateMachine;

        rootStateMachine.entryPosition = new Vector3(0,0);
        rootStateMachine.anyStatePosition = new Vector3(0,(-1*positionFactor));
        rootStateMachine.exitPosition = new Vector3((4*positionFactor),0);
        
        //check for Emotion Search state
        curStateIndex = JunToolsHelper.getStateIndex(rootStateMachine,"Emotion Search");

        if(curStateIndex == -1){ //make one
            EmotionSearchState = rootStateMachine.AddState("Emotion Search",new Vector3((1*positionFactor),0));


        }else{//somehow get a refrence to it
            ChildAnimatorState e = rootStateMachine.states[curStateIndex];
            Debug.Log(e.position);
            e.position = new Vector3((1*positionFactor),0);
            EmotionSearchState = e.state;
        }

        //ensure this is setup correctly 
        EmotionSearchState.motion = noneAnimation;
        EmotionSearchState.writeDefaultValues = false;

        //create a new substate for the face set
        emotionStateMachine = rootStateMachine.AddStateMachine(emotionSetName,new Vector3((2*positionFactor),((emotionSetInteger-1)*positionFactor)));

        emotionStateMachine.parentStateMachinePosition = new Vector3(0,0);
        emotionStateMachine.entryPosition = new Vector3(0,(1*positionFactor));
        emotionStateMachine.anyStatePosition = new Vector3(0,(-1*positionFactor));
        emotionStateMachine.exitPosition = new Vector3((4*positionFactor),0);



    //populate substate

        //create gesture search
        searchState = emotionStateMachine.AddState("Search",new Vector3((1f*positionFactor),0));
        searchState.motion = noneAnimation;
        searchState.writeDefaultValues = false;


        //from search -> exit
        tr = searchState.AddExitTransition();
        JunToolsHelper.formatTransition(tr);
        tr.AddCondition(AnimatorConditionMode.NotEqual,emotionSetInteger,emotionVariableName);


        //from context -> search
        tr = EmotionSearchState.AddTransition(searchState);
        JunToolsHelper.formatTransition(tr);
        tr.AddCondition(AnimatorConditionMode.Equals,emotionSetInteger,emotionVariableName);


        //setup each individual state
        for(int i = 0; i < animationNames.Length;i++){
        //make the state
            AnimatorState curState;

            if(i == 0){
                curState = emotionStateMachine.AddState(animationNames[i],
                    new Vector3(
                        (2.5f*positionFactor),
                        (((i-1) - (animationNames.Length/2)) * (positionFactor/2))
                    )
                );
            }else{
                curState = emotionStateMachine.AddState(animationNames[i],
                    new Vector3(
                        (2.5f*positionFactor),
                        ((i - (animationNames.Length/2)) * (positionFactor/2))
                    )
                );
            }

            
            curState.motion = faceAnimations[i];
            curState.writeDefaultValues = false;


        //make the transitions
            //from search to state
            tr = searchState.AddTransition(curState);
            JunToolsHelper.formatTransition(tr);
            tr.AddCondition(AnimatorConditionMode.Equals,i,"GestureRight");

            if(hasFaceLock){
                tr.AddCondition(AnimatorConditionMode.IfNot,1,"FaceLockToggle");
            }

            if(i == 0){
                tr.AddCondition(AnimatorConditionMode.Equals,0,"GestureLeft");
            }


            //from state to exit 1
            tr = curState.AddExitTransition();
            JunToolsHelper.formatTransition(tr);
            tr.AddCondition(AnimatorConditionMode.NotEqual,i,"GestureRight");

            if(hasFaceLock){
                tr.AddCondition(AnimatorConditionMode.IfNot,1,"FaceLockToggle");
            }


            //from state to exit 3
            tr = curState.AddExitTransition();
            JunToolsHelper.formatTransition(tr);
            tr.AddCondition(AnimatorConditionMode.NotEqual,emotionSetInteger,emotionVariableName);      

            if(i == 0){
                tr = curState.AddExitTransition();
                JunToolsHelper.formatTransition(tr);
                tr.AddCondition(AnimatorConditionMode.NotEqual,0,"GestureLeft");

                if(hasFaceLock){
                    tr.AddCondition(AnimatorConditionMode.IfNot,1,"FaceLockToggle");
                }

            }


            //if it's 0 on the right hand, we're making another one.
            if(i == 0){
                curState = emotionStateMachine.AddState("Left Hand Control",
                    new Vector3(
                        (2.5f*positionFactor),
                        ((i - (animationNames.Length/2)) * (positionFactor/2))
                    )
                );
                curState.motion = noneAnimation;
                curState.writeDefaultValues = false;


            //make the transitions
                //from search to state
                tr = searchState.AddTransition(curState);
                JunToolsHelper.formatTransition(tr);
                tr.AddCondition(AnimatorConditionMode.Equals,i,"GestureRight");

                if(hasFaceLock){
                    tr.AddCondition(AnimatorConditionMode.IfNot,1,"FaceLockToggle");
                }

                tr.AddCondition(AnimatorConditionMode.NotEqual,0,"GestureLeft");


                //from state to exit 1
                tr = curState.AddExitTransition();
                JunToolsHelper.formatTransition(tr);
                tr.AddCondition(AnimatorConditionMode.NotEqual,i,"GestureRight");

                if(hasFaceLock){
                    tr.AddCondition(AnimatorConditionMode.IfNot,1,"FaceLockToggle");
                }

                //from state to exit 2
                tr = curState.AddExitTransition();
                JunToolsHelper.formatTransition(tr);
                tr.AddCondition(AnimatorConditionMode.NotEqual,emotionSetInteger,emotionVariableName);      

                //from state to exit 3
                tr = curState.AddExitTransition();
                JunToolsHelper.formatTransition(tr);
                tr.AddCondition(AnimatorConditionMode.Equals,0,"GestureLeft");
                if(hasFaceLock){
                    tr.AddCondition(AnimatorConditionMode.IfNot,1,"FaceLockToggle");
                }
            }
        }
    }
}
