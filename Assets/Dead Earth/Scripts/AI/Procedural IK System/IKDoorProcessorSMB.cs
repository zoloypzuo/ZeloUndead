using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKDoorProcessorSMB : StateMachineBehaviour
{
    int _openDoorHash = Animator.StringToHash("Open Door");

    // The AI State Machine reference
    protected IKDoorProcessor _behaviour;
    public IKDoorProcessor behaviour { set { _behaviour = value; } }

    // --------------------------------------------------------
    // Name	:	OnStateExit
    // Desc	:	Called on the last frame of the animation prior
    //			to leaving the state. This makes sure that the
    //          paired IKDoorProcessor has a chance to clean up
    //          and reset the door system
    // --------------------------------------------------------
    override public void OnStateExit(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        // Does this animator have a link to a paired IKDoorProcessor behaviour
        if (_behaviour)
        {
            // We don't need to send in a state machine reference as the processor already has it
            _behaviour.OnResetSystem(null);

            // Reset the parameter in the animator if we are exiting under normal means (exit time)
            animator.SetBool(_openDoorHash, false);
        }
    }
}
