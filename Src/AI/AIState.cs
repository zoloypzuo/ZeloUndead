using UnityEngine;
using System.Collections;

// ----------------------------------------------------------------------
// Class	:	AIState
// Desc		:	The base class of all AI States used by our AI System.
// ----------------------------------------------------------------------
public abstract class AIState : MonoBehaviour 
{
	// Public Method
	// Called by the parent state machine to assign its reference
	public void SetStateMachine( AIStateMachine stateMachine ) { _stateMachine = stateMachine; }

	// Default Handlers
	public virtual void			OnEnterState()			{}
	public virtual void 		OnExitState()			{}
	public virtual void 		OnAnimatorIKUpdated()	{}
	public virtual void 		OnTriggerEvent( AITriggerEventType eventType, Collider other ){}
	public virtual void 		OnDestinationReached ( bool isReached ) {}

	// Abtract Methods
	public abstract AIStateType GetStateType();
	public abstract AIStateType OnUpdate();

	// Protected Fields
	protected AIStateMachine	_stateMachine;

	// ------------------------------------------------------------------
	// Name	:	OnAnimatorUpdated
	// Desc	:	Called by the parent state machine to allow root motion
	//			processing
	// ------------------------------------------------------------------
	public virtual void 		OnAnimatorUpdated() 	
	{
		// Get the number of meters the root motion has updated for this update and
		// divide by deltaTime to get meters per second. We then assign this to
		// the nav agent's velocity.
		if (_stateMachine.useRootPosition)
			_stateMachine.navAgent.velocity = _stateMachine.animator.deltaPosition / Time.deltaTime;

		// Grab the root rotation from the animator and assign as our transform's rotation.
		if (_stateMachine.useRootRotation)
			_stateMachine.transform.rotation = _stateMachine.animator.rootRotation;

	}

}
