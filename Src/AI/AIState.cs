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
	public virtual void 		OnAnimatorUpdated() 	{}
	public virtual void 		OnAnimatorIKUpdated()	{}
	public virtual void 		OnTriggerEvent( AITriggerEventType eventType, Collider other ){}
	public virtual void 		OnDestinationReached ( bool isReached ) {}

	// Abtract Methods
	public abstract AIStateType GetStateType();
	public abstract AIStateType OnUpdate();

	// Protected Fields
	protected AIStateMachine	_stateMachine;
}
