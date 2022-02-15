using UnityEngine;
using System.Collections;
using UnityEngine.AI;

// ----------------------------------------------------------------
// CLASS	:	AIZombieState_Patrol1
// DESC		:	Generic Patrolling Behaviour for a Zombie
// ----------------------------------------------------------------
public class AIZombieState_Patrol1 : AIZombieState
{
    // Inpsector Assigned 
    [SerializeField] float _turnOnSpotThreshold = 80.0f;
    [SerializeField] float _slerpSpeed = 5.0f;
    [SerializeField] [Range(0.0f, 3.0f)] float _speed = 1.0f;
    [SerializeField] [Range(0.0f, 1.0f)] float _randomHeadLookChance = 0.0f;
    [SerializeField] Transform _randomLookTargeter = null;

    // Internals
    float _nextHeadLookTimer = 0;
    Vector3 _randomLookAtPosition = Vector3.zero;
    Vector3[] _cornersPreAlloc = new Vector3[1000];
    float _warmupTimer = 0;

    // ------------------------------------------------------------
    // Name	:	GetStateType
    // Desc	:	Called by parent State Machine to get this state's
    //			type.
    // ------------------------------------------------------------
    public override AIStateType GetStateType() {
        return AIStateType.Patrol;
    }

    // ------------------------------------------------------------------
    // Name	:	OnEnterState
    // Desc	:	Called by the State Machine when first transitioned into
    //			this state. It initializes the state machine
    // ------------------------------------------------------------------
    public override void OnEnterState() {
        // Debug.Log("Entering Patrol") ;

        base.OnEnterState();
        if (_zombieStateMachine == null)
            return;

        // Configure State Machine
        _zombieStateMachine.NavAgentControl(true, false);
        _zombieStateMachine.seeking = 0;
        _zombieStateMachine.feeding = false;
        _zombieStateMachine.attackType = 0;
        _warmupTimer = 0;

        // Set Destination
        _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));

        // Make sure NavAgent is switched on
        _zombieStateMachine.navAgent.isStopped = false;
    }


    // ------------------------------------------------------------
    // Name	:	OnUpdate
    // Desc	:	Called by the state machine each frame to give this
    //			state a time-slice to update itself. It processes 
    //			threats and handles transitions as well as keeping
    //			the zombie aligned with its proper direction in the
    //			case where root rotation isn't being used.
    // ------------------------------------------------------------
    public override AIStateType OnUpdate() {
        // No idea why this happens - perhaps a bug in Unity AI or in my code...but sometimes when stopping and restaring an agent
        // after clearing a target...the agent can be given a path with ZERO corners in it which causes all hell to break loose.
        // This is the code that traps this and flushes the agents path and recalcs it...seems to fix the problem but more
        // investigation is needed.
        if (_zombieStateMachine.navAgent.path.GetCornersNonAlloc(_cornersPreAlloc) < 1) {
            _zombieStateMachine.navAgent.isStopped = true;
            _zombieStateMachine.navAgent.ResetPath();
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(false));
            _zombieStateMachine.speed = 0;
            _zombieStateMachine.navAgent.isStopped = false;
            Debug.Log("ERROR DETECTION:\n================\nRassigning Zero Length Path");
            return AIStateType.Alerted;
        }

        // Do we have a visual threat that is the player
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Player) {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Pursuit;
        }

        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Light) {
            _zombieStateMachine.SetTarget(_zombieStateMachine.VisualThreat);
            return AIStateType.Alerted;
        }

        // Sound is the third highest priority
        if (_zombieStateMachine.AudioThreat.type == AITargetType.Audio) {
            _zombieStateMachine.SetTarget(_zombieStateMachine.AudioThreat);
            return AIStateType.Alerted;
        }

        // We have seen a dead body so lets pursue that if we are hungry enough
        if (_zombieStateMachine.VisualThreat.type == AITargetType.Visual_Food) {
            // If the distance to hunger ratio means we are hungry enough to stray off the path that far
            if ((1.0f - _zombieStateMachine.satisfaction) >
                (_zombieStateMachine.VisualThreat.distance / _zombieStateMachine.sensorRadius)) {
                _stateMachine.SetTarget(_stateMachine.VisualThreat);
                return AIStateType.Pursuit;
            }
        }

        _warmupTimer += Time.deltaTime;

        // If path is still be computed then wait
        if (_zombieStateMachine.navAgent.pathPending || _warmupTimer < 0) {
            _zombieStateMachine.speed = 0;
            return AIStateType.Patrol;
        }
        else {
            _zombieStateMachine.speed = _speed;
        }

        // Calculate angle we need to turn through to be facing our target
        float angle = Vector3.Angle(_zombieStateMachine.transform.forward,
            (_zombieStateMachine.navAgent.steeringTarget - _zombieStateMachine.transform.position));

        // If its too big then drop out of Patrol and into Altered
        if (angle > _turnOnSpotThreshold) {
            return AIStateType.Alerted;
        }

        // If root rotation is not being used then we are responsible for keeping zombie rotated
        // and facing in the right direction. 
        if (!_zombieStateMachine.useRootRotation) {
            // Generate a new Quaternion representing the rotation we should have
            Quaternion newRot = Quaternion.LookRotation(_zombieStateMachine.navAgent.desiredVelocity);

            // Smoothly rotate to that new rotation over time
            _zombieStateMachine.transform.rotation = Quaternion.Slerp(_zombieStateMachine.transform.rotation, newRot,
                Time.deltaTime * _slerpSpeed);
        }

        // If for any reason the nav agent has lost its path then call the NextWaypoint function
        // so a new waypoint is selected and a new path assigned to the nav agent.
        if (_zombieStateMachine.navAgent.isPathStale ||
            !_zombieStateMachine.navAgent.hasPath ||
            _zombieStateMachine.navAgent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete) {
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(true));
        }

        // Stay in Patrol State
        return AIStateType.Patrol;
    }


    // ----------------------------------------------------------------------
    // Name	:	OnDestinationReached
    // Desc	:	Called by the parent StateMachine when the zombie has reached
    //			its target (entered its target trigger
    // ----------------------------------------------------------------------
    public override void OnDestinationReached(bool isReached) {
        //Debug.Log("Detsination Reached " + isReached);

        // Only interesting in processing arrivals not departures
        if (_zombieStateMachine == null || !isReached)
            return;

        // Select the next waypoint in the waypoint network
        if (_zombieStateMachine.targetType == AITargetType.Waypoint)
            _zombieStateMachine.navAgent.SetDestination(_zombieStateMachine.GetWaypointPosition(true));
    }

    // -----------------------------------------------------------------------
    // Name	:	OnAnimatorIKUpdated
    // Desc	:	Override IK Goals
    // -----------------------------------------------------------------------
    public override void OnAnimatorIKUpdated() {
        if (_zombieStateMachine == null) return;

        _nextHeadLookTimer += Time.deltaTime;
        // Debug.Log("IK Updated " + _nextHeadLookTimer + "    " + Time.frameCount);

        if (_nextHeadLookTimer > 3.0f) {
            _randomLookAtPosition = Vector3.zero;
            _nextHeadLookTimer = 0;

            if (Random.value < _randomHeadLookChance) {
                // Debug.Log("New head Position");
                _randomLookAtPosition = _zombieStateMachine.transform.position + Random.onUnitSphere +
                                        _zombieStateMachine.transform.forward;
                _randomLookAtPosition.y = _zombieStateMachine.animator.GetBoneTransform(HumanBodyBones.Head).position.y;
                _randomLookTargeter.position = _randomLookAtPosition;
            }
        }

        if (_randomLookAtPosition.Equals(Vector3.zero)) {
            base.OnAnimatorIKUpdated();
        }
        else {
            // This is where we are trying to look
            Vector3 newLookAtTarget = Vector3.Lerp(_zombieStateMachine.currentLookAtPosition,
                _randomLookTargeter.position, Time.deltaTime);

            // We have a max rotate angle of 100 so calculate our current LookAt angle to target as a T value in that range
            float weight = Mathf.Clamp01(Mathf.InverseLerp(0.0f,
                120,
                Vector3.Angle(_zombieStateMachine.transform.forward,
                    newLookAtTarget - _zombieStateMachine.transform.position)
            ));


            // The new look angle is 1-weight so influence dilutes as angle grows and lerp towards that value
            _zombieStateMachine.currentLookAtWeight =
                Mathf.Lerp(_zombieStateMachine.currentLookAtWeight, 1 - weight, Time.deltaTime);
            _zombieStateMachine.currentLookAtPosition = newLookAtTarget;

            // Set look at position and weight
            _zombieStateMachine.animator.SetLookAtPosition(_zombieStateMachine.currentLookAtPosition);
            _zombieStateMachine.animator.SetLookAtWeight(_zombieStateMachine.currentLookAtWeight);
        }
    }
}