using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedStateTransitionSMB : StateMachineBehaviour
{
    // Inspector Assigned
    [Tooltip("How long before the forced transition occurs.")]
    public float IdleTimeout = 10.0f;

    [Tooltip("Transition Time into forced state")]
    public float TransitionTime = 0.2f;

    [Tooltip("The name of the state to force a transition to")]
    public string StateName = "Empty State";

    // Internals
    private float   _timer     = 0.0f;
    private int     _stateHash = -1; 
   
    override public void OnStateEnter(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        _timer = 0.0f;
        _stateHash = Animator.StringToHash( StateName );
    }



    override public void OnStateUpdate(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        _timer += Time.deltaTime;
        if (_timer > IdleTimeout)
        {
            _timer = float.MinValue;
            animator.CrossFade(_stateHash, TransitionTime);
        }
    }
}
