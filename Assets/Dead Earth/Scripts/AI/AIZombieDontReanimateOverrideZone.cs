using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ------------------------------------------------------------------------------------------------
// Class    :   AIZombieDontReanimateOverrideZone
// Desc     :   A trigger script that overrides the current reanimation settings of the zombie
//              state machine
// ------------------------------------------------------------------------------------------------
[RequireComponent(typeof(BoxCollider))]
public class AIZombieDontReanimateOverrideZone : MonoBehaviour
{
    // Internal
    protected Collider _collider = null;

    // Start is called before the first frame update
    void Start() {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
    }

    protected void OnTriggerEnter(Collider other) {
        if (GameSceneManager.instance) {
            AIZombieStateMachine _stateMachine =
                GameSceneManager.instance.GetAIStateMachine(other.GetInstanceID()) as AIZombieStateMachine;
            if (_stateMachine) {
                _stateMachine.DontReanimate(true);
            }
        }
    }

    protected void OnTriggerExit(Collider other) {
        if (GameSceneManager.instance) {
            AIZombieStateMachine _stateMachine =
                GameSceneManager.instance.GetAIStateMachine(other.GetInstanceID()) as AIZombieStateMachine;
            if (_stateMachine) {
                _stateMachine.DontReanimate(false);
            }
        }
    }
}