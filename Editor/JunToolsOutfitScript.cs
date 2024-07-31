using System;
using System.Collections.Generic;
using System.Drawing.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.PlayerLoop;
using VRC.SDKBase;

//class for holding information about the state (from GUI)
public class StateObj {
    public string name = "";
    public string valueName = ""; //parameter name for self
    public int targetValue = 0;  //parameter value for self
    public string childValueName =""; //parameter name for children
    public int type = 0; //0 for regular animation, 1 for substate

    public List<StateObj> children = new List<StateObj>();

    public StateObj parent = null;

    public AnimationClip animation = null;

    public AnimatorState stateObject = null; //parameter used when animation / used as "Search" for state machine

    public AnimatorStateMachine stateMachineObject = null; //parameter used when state machine

    public StateObj(string _name, string _valueName, int _targetValue, int _type, StateObj _parent) { 
        name = _name;
        valueName = _valueName;
        targetValue = _targetValue;
        type = _type;

        if(_parent != null){
            parent = _parent;
        }
    }

}

public class JunToolsOutfitScript : EditorWindow
{
    AnimatorController targetAnimator;
    bool isRoot = true;
    string parameterName = "OutfitIndex";
    int parameterValue = 0; //this will be the first level parameter (i.e, the parameter for access CONTEXT state)
    string layerName = "Outfit";
    AnimationClip noneAnimation;
    List<StateObj> stateObjs= new List<StateObj>();

    int positionFactor = JunToolsHelper.positionFactor;

    private Vector2 scrollPos;


    //int elementNumber = 1;
    [MenuItem("Tools/JunTools/Selector Setup")]
    //still don't know why I do this
    public static void ShowWindow(){
        GetWindow(typeof(JunToolsOutfitScript));
    }

    //updates all in the "Values" field to be in order
    private void UpdateAllValues(List<StateObj> _objList){
        for(int i = 0; i < _objList.Count;i++){
            StateObj obj = _objList[i];
            obj.targetValue = i;

            if(obj.type == 1){
                UpdateAllValues(obj.children);
            }
        }
    }
    //draws -/+ button for chidren states, also adds and removes said states
    private void drawAddRemove(List<StateObj> _list,string _parameterName, StateObj _parent, int _depth){
        EditorGUI.indentLevel = _depth;

        int num = _list.Count;
        // [-] [+]
        GUILayout.BeginHorizontal();

        if(num > 1){
            if(GUILayout.Button("-")){
                _list.RemoveAt(_list.Count-1);
            }
        };

        if(GUILayout.Button("+")){
            
            _list.Add(new StateObj("",_parameterName,0,0,_parent));
        };

        GUILayout.EndHorizontal();
    }
    //figures out whether to draw GUI for an animation or for a substate (probable recursion)
    private void drawGui(List<StateObj> _list, string _parameterName, int _depth){
        EditorGUI.indentLevel = _depth;
        
        foreach(StateObj curObj in _list){
            if(curObj.type == 0){
                guiAnimationState(curObj,_depth);
            }else if(curObj.type == 1){
                guiSubState(curObj,_depth);
            }else{
                Debug.LogError("Invalid State Object Type");
            }
        }
    }
    //draws a line for animation states, and update it's values
    private void guiAnimationState(StateObj _curStateObj,int _depth){
        EditorGUI.indentLevel = _depth;
        //State Name: [Name] | Value: [value] | Animation: [Animation] | [Switch to Substate]
        GUILayout.BeginHorizontal();
        _curStateObj.name = EditorGUILayout.TextField("State Name", _curStateObj.name);
        _curStateObj.targetValue = EditorGUILayout.IntField("Value",_curStateObj.targetValue);
        _curStateObj.animation = EditorGUILayout.ObjectField("Animation",_curStateObj.animation,typeof(AnimationClip),false) as AnimationClip;
        if(GUILayout.Button("Switch to Substate")){
            _curStateObj.type = 1;
        };
        GUILayout.EndHorizontal();

        if(_curStateObj.parent != null){
            _curStateObj.valueName =  _curStateObj.parent.childValueName;
        }
        else{
            _curStateObj.valueName = parameterName;
        }
        

    }
    //draws a line for substates (probable recursion)
    private void guiSubState(StateObj _curStateObj,int _depth){
        EditorGUI.indentLevel = _depth;
        // State Name: [name] | Value: [value] | [Switch to Animation]
        GUILayout.BeginHorizontal();
        _curStateObj.name = EditorGUILayout.TextField("State Name", _curStateObj.name);
        _curStateObj.targetValue = EditorGUILayout.IntField("Value",_curStateObj.targetValue);

        if(_curStateObj.parent != null){
            _curStateObj.valueName =  _curStateObj.parent.childValueName;
        }
        else{
            _curStateObj.valueName = parameterName;
        }

        if(GUILayout.Button("Switch to Animation")){
            _curStateObj.type = 0;
            _curStateObj.children.Clear(); //removes children, just incase.
        };
        GUILayout.EndHorizontal();

        EditorGUI.indentLevel = _depth + 1;
        _curStateObj.childValueName = EditorGUILayout.TextField("Substate Value",_curStateObj.childValueName);

        drawGui(_curStateObj.children,_curStateObj.childValueName,(_depth + 1));
        drawAddRemove(_curStateObj.children, _curStateObj.childValueName,_curStateObj,(_depth + 1));
    }
    //debug display to make sure the hierarchy of the data is correct
    private string printTree(List<StateObj> _objList,string _parameterName,int _depth){
        string toPrint = "Layer: " + _depth.ToString() + " | Parameter: " + _parameterName + "\n";

        foreach(StateObj obj in _objList){
            string objPrint = "";

            for(int i = 0; i < _depth; i++){
                objPrint += "->";
            }

            if(obj.type == 0){
                objPrint += " [animation]";
            }else if(obj.type == 1){
                objPrint += " [substate]";
            }else{
                objPrint += " [UNK]";
            }
            
            string parentPrint = "";

            if(obj.parent == null){
                parentPrint = "NULL";
            }else{
                parentPrint = obj.parent.name;
            }

            objPrint += " | Parent: " + parentPrint +  " | Name: " + obj.name + " | Parameter: " + obj.valueName + " | Value: " + obj.targetValue + "\n";

            if(obj.type == 1){
                objPrint += printTree(obj.children,obj.childValueName,(_depth + 1));
            }

            toPrint += objPrint;
        }
        
        return toPrint;
    }
    
    //ANIMATOR BUILDER FUNCTIONS

    //by the point of making transitions, we assume that all eligible children have their states created.
    private void makeTransitions(AnimatorState _startingState, StateObj _rootObject){
        /*
            root obj
                -> Child A
                -> Child B
                -> Child C
        */
        StateObj curWalk = _rootObject; //for upwards "walking"
        AnimatorStateTransition tr; //reused refrence for transition editing

        //start -> exit (we "walk" up the family to make sure we have subsequent exit transitions)
       while(curWalk != null){
            tr = _startingState.AddExitTransition();
            JunToolsHelper.formatTransition(tr);
            tr.AddCondition(AnimatorConditionMode.NotEqual,curWalk.targetValue,curWalk.valueName);
            curWalk = curWalk.parent;
       }

        foreach(StateObj obj in _rootObject.children){
            if(obj.type == 0){
                Debug.Log("Walking through state: " + obj.name);
                if(obj.stateObject == null){
                    Debug.LogError(obj.name + " has no linked \"stateObject\"");
                }
                //state -> exit (we "walk" up the family tree, starting at self)
                curWalk = obj;

                while(curWalk != null){

                    if(curWalk != null){
                        Debug.Log("Linking: " + obj.name + " to EXIT using: " + curWalk.name + "\'s condition of: " + curWalk.valueName);
                    }else{
                        Debug.Log("Linking: " + obj.name + " to NULL");
                    }

                    if(curWalk == null){
                        break;
                    }
                    tr = obj.stateObject.AddExitTransition();
                    JunToolsHelper.formatTransition(tr);
                    tr.AddCondition(AnimatorConditionMode.NotEqual,curWalk.targetValue,curWalk.valueName);
                    curWalk = curWalk.parent;

                    if(curWalk == null){
                        break;
                    }
                }

                //from start -> state (we only make 1 enter state, since the exit states should pull it out if needed)
                tr = _startingState.AddTransition(obj.stateObject);
                JunToolsHelper.formatTransition(tr);
                tr.AddCondition(AnimatorConditionMode.Equals,obj.targetValue,obj.valueName);
            }
        }
    }
    //by this point, the "_parent" substate should have it's own substate and search state created
    private void BuildSubstate(StateObj _parent){
        /*
            Substate transition rules:
            All states need to come from "Search"
            All states need an exit for their respective transition
            All states need an exit for their parent's transition
        */

        //requirements: [substate] [has children] [has stateMachine object] [has stateObject (search state)]
        if(_parent.type != 1 || _parent.children.Count <= 0 || _parent.stateMachineObject == null || _parent.stateObject == null){return;} //exit

        //make new paramters if doesn't exist
        if(_parent.childValueName != ""){
            JunToolsHelper.addParameterToAnimator(targetAnimator, _parent.childValueName,AnimatorControllerParameterType.Int);
        }

        for (int i = 0; i < _parent.children.Count; i++) {
            //iterate through all the children and make the respective state objects
            StateObj obj = _parent.children[i];
            if(obj.type == 0){ //animation
                AnimatorState newState = _parent.stateMachineObject.AddState(obj.name,
                    new Vector3(
                        (2.5f*positionFactor),
                        ((i - (stateObjs.Count/2)) * (positionFactor/2))
                    )
                );
                newState.motion = obj.animation;
                newState.writeDefaultValues = false;

                obj.stateObject = newState;

            }else if(obj.type == 1){ //substate NOTE: I REALLY SHOULDN'T NEED TO HAVE MORE THAN 1 LEVEL OF CHILDREN
                obj.stateMachineObject = JunToolsHelper.CreateFormattedStateMachine(
                    _parent.stateMachineObject,
                    obj.name,
                    new Vector3(
                        (2.5f*positionFactor),
                        ((i - (stateObjs.Count/2)) * (positionFactor/2))
                    )
                );

                //create search state
                AnimatorState searchState = obj.stateMachineObject.AddState("Search",new Vector3((1*positionFactor),0));
                searchState.motion = noneAnimation;
                searchState.writeDefaultValues = false;

                obj.stateObject = searchState;

                BuildSubstate(obj);


            }else{
                Debug.LogError("INVALID OBJECT TYPE");
            }
        }

        //ok, start making transitions now that all the states have been made.
        makeTransitions(_parent.stateObject, _parent);

    }
    private void BuildAnimator(){
        //make the layer if it doesn't exist
        int layerIndex = JunToolsHelper.addLayerToAnimator(targetAnimator,layerName);
        //make main parameter if it doesn't exist
        int parameterIndex = JunToolsHelper.addParameterToAnimator(targetAnimator,parameterName,AnimatorControllerParameterType.Int);

        //we can't use the create formatted since it already exists
        //build root layer
        AnimatorStateMachine rootStateMachine = targetAnimator.layers[layerIndex].stateMachine;
        //format positions
        rootStateMachine.entryPosition = new Vector3(0,0);
        rootStateMachine.anyStatePosition = new Vector3(0,(-1*positionFactor));
        rootStateMachine.exitPosition = new Vector3((4*positionFactor),0);

        StateObj obj; //for walking
        AnimatorTransition tr; //for walking (entry)
        AnimatorStateTransition tsr; //for walking (exit)
        //build,format, and make transitions from entry
        for(int i = 0; i < stateObjs.Count ; i++){
            obj = stateObjs[i];
            if(obj.type == 0){ //animation
                AnimatorState newState = rootStateMachine.AddState(obj.name,
                    new Vector3(
                        (2.5f*positionFactor),
                        ((i - (stateObjs.Count/2)) * (positionFactor/2))
                    )
                );
                newState.motion = obj.animation;
                newState.writeDefaultValues = false;

                obj.stateObject = newState;

                //entry -> state (we don't format because there's nothing to format)
                tr = rootStateMachine.AddEntryTransition(obj.stateObject);
                tr.AddCondition(AnimatorConditionMode.Equals,obj.targetValue,obj.valueName);
                //state -> exit
                tsr = obj.stateObject.AddExitTransition();
                JunToolsHelper.formatTransition(tsr);
                tsr.AddCondition(AnimatorConditionMode.NotEqual,obj.targetValue,obj.valueName);

            }
            else if(obj.type == 1){ //substate
                obj.stateMachineObject = JunToolsHelper.CreateFormattedStateMachine(
                    rootStateMachine,
                    obj.name,
                    new Vector3(
                        (2.5f*positionFactor),
                        ((i - (stateObjs.Count/2)) * (positionFactor/2))
                    )
                );

                //create search state
                AnimatorState searchState = obj.stateMachineObject.AddState("Search",new Vector3((1*positionFactor),0));
                searchState.motion = noneAnimation;
                searchState.writeDefaultValues = false;

                obj.stateObject = searchState;

                //entry -> search (we don't format because there's nothing to format)
                tr = rootStateMachine.AddEntryTransition(obj.stateObject);
                tr.AddCondition(AnimatorConditionMode.Equals,obj.targetValue,obj.valueName);
                //(we don't have an exit because the substate has the exit)

                BuildSubstate(obj);


            }
            else{
                Debug.LogError("INVALID OBJECT TYPE");
            }
        }
    }
    //function that actually draws the UI in Unity
    private void OnGUI(){
        GUILayout.Label("Outfit Set setup",EditorStyles.boldLabel);

        if(GUILayout.Button("Reset Setup")){
            return;
        }
        
        
        isRoot = EditorGUILayout.Toggle("IS ROOT",isRoot);
        targetAnimator = EditorGUILayout.ObjectField("Animator",targetAnimator,typeof(AnimatorController),false) as AnimatorController;
        layerName = EditorGUILayout.TextField("Layer Name",layerName);
        noneAnimation = EditorGUILayout.ObjectField("None Animation",noneAnimation,typeof(AnimationClip),false) as AnimationClip;
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        scrollPos = GUILayout.BeginScrollView(scrollPos,
        GUILayout.Width(EditorGUIUtility.currentViewWidth -10),
        GUILayout.Height(EditorGUIUtility.singleLineHeight * 15)
        );

        // Parameter Name: [name] | Parameter Value: [value] (if not root)
        GUILayout.BeginHorizontal();
        parameterName = EditorGUILayout.TextField("Parameter Name",parameterName);
        if(!isRoot){
            parameterValue = EditorGUILayout.IntField("Value",parameterValue);
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        //ensure that stateObjs isn't empty
        if(stateObjs.Count <= 0){
            stateObjs.Add(new StateObj("",parameterName,0,0,null));
        }

        drawGui(stateObjs,parameterName,0);
        drawAddRemove(stateObjs,parameterName,null,0);
        UpdateAllValues(stateObjs);

        GUILayout.EndScrollView();

        if(GUILayout.Button("Print Tree")){
            Debug.Log(printTree(stateObjs,parameterName,0));
        }

        if(GUILayout.Button("Build Animator")){
            BuildAnimator();
        }
    }
}
