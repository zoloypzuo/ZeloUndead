using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKProceduralStepSystemSMB : StateMachineBehaviour
{
    // Inspector
    [SerializeField] IKProceduralStepSystemData _stepSystemData = null;
    [SerializeField] StringList _layerExclusions = null;

    // Internals
    protected IKProceduralStepSystemPlayer  _iKProcPlayer = null;
    protected AIStateMachine                _stateMachine = null;

    //Accessors
    public IKProceduralStepSystemPlayer ikProcPlayer
    {
        get { return _iKProcPlayer; }
        set { _iKProcPlayer = value; }
    }

    public AIStateMachine stateMachine
    {
        get { return _stateMachine; }
        set { _stateMachine = value; }
    }

    // --------------------------------------------------------
    // Name	:	OnStateEnter
    // Desc	:	Called prior to the first frame the
    //			animation assigned to this state.
    // --------------------------------------------------------
    override public void OnStateEnter(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        // Cast the state machine to a Zombie State Machine that supports Step Data System
        if (!_iKProcPlayer) return;
     
        // Give the state machine the step data for this animation 
        if (ShouldProcessStepData( animator, layerIndex ))
        {
            _iKProcPlayer.data = _stepSystemData;
        }

    }

    // --------------------------------------------------------
    // Name	:	OnStateEnter
    // Desc	:	Called prior to the first frame the
    //			animation assigned to this state.
    // --------------------------------------------------------
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        // Cast the state machine to a Zombie State Machine that supports Step Data System
        if (!_iKProcPlayer) return;
        
        // Give the state machine the step data for this animation 
        if (ShouldProcessStepData(animator, layerIndex))
        {
            _iKProcPlayer.data = _stepSystemData;
        }

    }

    // --------------------------------------------------------
    // Name	:	OnStateExit
    // Desc	:	Called on the last frame of the animation prior
    //			to leaving the state.
    // --------------------------------------------------------
    override public void OnStateExit(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        // Cast the state machine to a Zombie State Machine that supports Step Data System
        if (!_iKProcPlayer) return;
       
        // Clear the Step Data for this animation state as we are leaving it now
        if (ShouldProcessStepData( animator, layerIndex ))
        {
            _iKProcPlayer.data = null;
        }

     
    }


    // --------------------------------------------------------------------------------------------
    // Name :   ShouldProcessStepData
    // Desc :   Determines whether this layer has focus and is not being overriden by a higher layer.
    // ---------------------------------------------------------------------------------------------
    protected bool ShouldProcessStepData( Animator animator, int layerIndex )
    {
        // If layer is disabled then return false we don't want to process step sstem
        if (layerIndex != 0 && animator.GetLayerWeight(layerIndex).Equals(0.0f)) return false;
     
        // If any of the specified layers are active then also return false
        if (_layerExclusions != null)
        {
            for (int i = 0; i < _layerExclusions.count; i++)
            {
                if (_stateMachine.IsLayerActive(_layerExclusions[i])) return false;
            }
        }

        // Okay...we should process the steps system as this animation layer has focus
        return true;
    }
}
