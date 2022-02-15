using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// ------------------------------------------------------------------------------------------------
// CLASS    :   IKDoorProcessor
// DESC     :   Can be added to any AIZombieStateMachine object to handle Door animations and
//              hand IK
// -------------------------------------------------------------------------------------------------
[RequireComponent(typeof(AIZombieStateMachine))]
public class IKDoorProcessor : MonoBehaviour
{
    // Inspector Assigned
    [Tooltip(
        "The Layer Mask describing which layers should be cast against to find doors. Door colliders should be on their own layer and each have an InteractiveDoor component on it.")]
    [SerializeField]
    protected LayerMask _layerMask = -1;

    [Tooltip("The distance at which a Jogging Zombi should drop down to the approach Speed.")] [SerializeField]
    protected float _jogSlowDownDistance = 2.0f;

    [Tooltip("The distance at which a sprinting zombie down down to the approach speed.")] [SerializeField]
    protected float _sprintSlowDownDistance = 3.25f;

    [Tooltip("The speed to use for final apporach to the door (usually 1.0 the walk speed in the animator.")]
    [SerializeField]
    protected float _approachSpeed = 1;

    [Tooltip("The distance at which the door activation animation will be played.")] [SerializeField]
    protected float _activateDistance = 0.51f;

    [Tooltip("The weight to use for tilting the head towards the IK Goal")] [Range(0.0f, 1.0f)] [SerializeField]
    float _headIKWeight = 1.0f;

    [Tooltip("Show NavMesh Line Casting Lines in the Scene View.")] [SerializeField]
    protected bool _drawDebug = false;

    // Animator Parameter Hashes
    protected int _openDoorHash = Animator.StringToHash("Open Door");
    protected int _comChannelHash = Animator.StringToHash("ComChannelDoor");
    protected int _openDoorMirrorHash = Animator.StringToHash("Open Door Mirror");
    protected int _handsIKTargetWeightHash = Animator.StringToHash("HandsIKTargetWeight");
    protected int _handsIKMirrorLeftHash = Animator.StringToHash("HandsIKMirrorLeft");


    // Internals
    protected NavMeshAgent _agent;
    protected AIZombieStateMachine _npc;
    protected bool _leftSide = false;

    Transform _IKTarget = null; // Transform of the door's hand IK target we will fetch from the door
    InteractiveDoor _door = null; // The door we have encountered on our path
    InteractiveDoor _previousDoor = null;

    bool
        _processingDoor =
            false; // Are we currently processing a door which includes the pre-animation and the door activation

    bool _activatingDoor = false; // Is the door currently in the middle of activating (opening or closing)
    bool _attemptSuccessful = false;
    float _autoTimer = 0;

    Vector3 _standingPosition = Vector3.zero;
    Vector3 _lookDirection = Vector3.zero;

    // --------------------------------------------------------------------------------------------
    // Name :   Start
    // Desc :   Cache StateMachine and NavAgent components and register listeners with the
    //          state machine so we can reset the door system when the zombie is hit or damaged.
    // --------------------------------------------------------------------------------------------
    void Start() {
        _npc = GetComponent<AIZombieStateMachine>();
        _agent = _npc.navAgent;

        // Add listeners to reset the system if interrupted half way through
        _npc.OnDeath.AddListener(OnResetSystem);
        _npc.OnTakenDamage.AddListener(OnResetSystem);
        _npc.OnRagdoll.AddListener(OnResetSystem);

        // Tell any IKDoorProcessorSMB script in the animator that THIS behaviour is their target.
        IKDoorProcessorSMB[] smbs = _npc.animator.GetBehaviours<IKDoorProcessorSMB>();
        foreach (IKDoorProcessorSMB smb in smbs) {
            smb.behaviour = this;
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Name  :   Update
    // Desc  :   Searches along the current Nav Path to see if a door is about to be encountered and
    //           either reduces the speed of the zombie or activates the Door Opening animation
    //           based on distance from the door.
    // ---------------------------------------------------------------------------------------------
    void Update() {
        // We need a nav agent with a path
        if (!_agent || !_agent.hasPath || _agent.pathPending || _agent.isPathStale ||
            _agent.pathStatus != NavMeshPathStatus.PathComplete) {
            // Wait for a valid path to become available
            return;
        }

        _autoTimer -= Time.deltaTime;

        // If we are not already in the process of opening a door
        // or waiting for and auto-door to open
        if (_processingDoor || _autoTimer > 0) return;

        // The length we need to search along the path for a door
        float distanceRemaining = _npc.speed > 2.0f ? _sprintSlowDownDistance : _jogSlowDownDistance;
        float distanceTracked = 0;

        // Get the array of corners on the nav path
        Vector3[] corners = _agent.path.corners;
        Vector3 nextStartPoint, nextEndPoint;

        // Render entire path
        if (_drawDebug) {
            for (int i = 1; i < corners.Length; i++) {
                Debug.DrawLine(corners[i - 1], corners[i], Color.red);
            }
        }

        // Iterate through all the corners from the agents current position
        for (int i = 0; i < corners.Length; i++) {
            // If distance has been spent then break from the loop we found nothing
            if (distanceRemaining <= 0) break;

            // Find the next two world space positions along the path to 
            // perform a linecast with
            if (i > 0) {
                // Usually its the previous and current points
                nextStartPoint = corners[i - 1];
                nextEndPoint = corners[i];
            }
            else {
                // Except in first iteration when the first line segment is
                // from the NPCs curent position to the first waypoint
                nextStartPoint = transform.position;
                nextEndPoint = corners[i];
            }

            // Calculate distance of this line segment
            float dist = Vector3.Distance(nextStartPoint, nextEndPoint);

            // If the distance between the next two points exceeds the distance we have left
            // to search then modify the end point to be the correct distance along the line
            // Otherwise simply use the two points and subtract the distance from the remaining
            // distance we have left to search
            if (dist >= distanceRemaining) {
                dist = distanceRemaining;
                distanceRemaining = 0;
                nextEndPoint = nextStartPoint + ((nextEndPoint - nextStartPoint).normalized * dist);
            }
            else {
                distanceRemaining -= dist;
            }


            // Draw the search line in the editor's scene view (if enabled)
            if (_drawDebug) {
                Debug.DrawLine(nextStartPoint, nextEndPoint);
            }

            // Perform a linecast on the next two point to see if we hit a door between them
            RaycastHit hit;
            if (Physics.Linecast(nextStartPoint, nextEndPoint, out hit, _layerMask.value,
                QueryTriggerInteraction.Ignore)) {
                // Get the Interactive Door component on the object
                _door = hit.collider.GetComponentInParent<InteractiveDoor>();

                // If this doesn't have a door on it then something has been put on the
                // door layer without a door script
                if (!_door) continue;

                // If another NPC is currently using this door drop into idle and wait for it to finish
                if (_door.AIID != -1 && _door.AIID != _npc.GetInstanceID()) {
                    _npc.SetStateOverride(AIStateType.Idle);
                    break;
                }

                _door.AIID = _npc.GetInstanceID();

                // If we actually need to open the door
                if (!_npc.isCrawling &&
                    distanceTracked + hit.distance <= _activateDistance * 1.2f &&
                    Vector3.Angle(transform.forward, hit.point - transform.position) < 45.0f) {
                    // Only process if door is closed and its not an auto open door
                    if (!_door.isOpen && !_door.isAutoOpen) {
                        // Calculate the standing position in front / behind the door
                        _standingPosition = hit.point - (transform.forward * _activateDistance);
                        _lookDirection = hit.point - transform.position;

                        // Let the door know which function to call should the door operation be
                        // aborted mid way through
                        _door.OnPlayerInteractionEvent.AddListener(OnAbortProcessing);

                        // Fetch the IK target for the handle of the door from the Interactive Door Component
                        _IKTarget = _door.GetIKTarget(transform.position, hit.collider.GetInstanceID());

                        // Convert IK target into NPC local space to find whether it is on the left or right of us
                        _leftSide = _npc.transform.InverseTransformPoint(_IKTarget.position).x < 0;

                        // Stop the zombie (drop into idle animation)
                        _npc.speedOverride = 0;

                        // Add an listener so we will have a chance to update IK of the Animator from this component
                        _npc.AnimatorIKEvent.AddListener(UpdateNPCIK);

                        // Set the OpenDoor trigger in the NPCs animator
                        _npc.animator.SetBool(_openDoorHash, true);

                        // We are beginning to process the door interaction
                        _processingDoor = true;

                        // But are not yet opening it
                        _activatingDoor = false;

                        _attemptSuccessful = Random.value < _npc.intelligence;
                        return;
                    }
                }
                else {
                    // If we have encoutered an auto-opening door then open it
                    if (_door.isAutoOpen && !_door.isOpen) {
                        if (_door.aiDoorType == AIInteractiveDoorType.AICanOpen) {
                            _door.Activate(transform.position, false);
                            _door = null;
                            _processingDoor = false;
                        }
                        else {
                            // It's an auto-opening door which AI can't open (WEIRD) but let's just
                            // abandon the target in this case. DOOR SHOULD NOT BE SETUP THIS WAY :)
                            // Clear the current target
                            _npc.ClearTarget();
                        }

                        // This will makes us wait to 1 second before executing this search again which
                        // will give the door object enough time to switch to an Open status. Without
                        // this the door would be rapid fire activated every frame and never get a chance
                        // to actually open 
                        _autoTimer = 1;
                    }
                    else if (_npc.isCrawling) {
                        if (!_door.isOpen) {
                            ClearNPCTarget();
                            _npc.speedOverride = 0;
                            _autoTimer = 0;
                            return;
                        }
                    }

                    _npc.speedOverride = _approachSpeed;
                    return;
                }
            }

            // Add on the amount we have traced so far along the path
            distanceTracked += dist;
        }

        // If we were trying to get into a door and failed and we have now pathed away from it...we want to push
        // the NavAgent clear of the door so that going into an Alerted State or some other animation doesn't make
        // the zombie poke through the door. The nav obstacle will adjust the position of the Nav Agent to move it clear of the door
        if (_previousDoor) {
            _previousDoor.RepulseAIWhenClosed(3);
            _previousDoor.AIID = -1;
            _previousDoor = null;
        }

        // Disable speed override if no door intercepted along path
        _npc.speedOverride = -1;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   UpdateNPCIK
    // Desc :   Used to update the Hand of the NPC over the duration of the Open animation
    //          to move it towards the IK target (door handle).
    // --------------------------------------------------------------------------------------------
    protected void UpdateNPCIK(AIStateMachine stateMachine, int layer, Animator animator) {
        // Used to represent either left or right hand goal
        AvatarIKGoal ikGoal;

        // Does this animation need to be mirrored for the left hand (fed by curve)
        bool mirrorNeededForLeft = animator.GetFloat(_handsIKMirrorLeftHash) > 0.99f;

        // Door handle is on left hand side so use the left hand of the NPC
        if (_leftSide) {
            // Set left hand as the IK Goal
            ikGoal = AvatarIKGoal.LeftHand;

            // If we need to mirror the animation for the left then set parameter in animator
            if (mirrorNeededForLeft)
                animator.SetBool(_openDoorMirrorHash, true);
            else
                animator.SetBool(_openDoorMirrorHash, false);
        }
        else {
            // Set the right hand as the IK goial
            ikGoal = AvatarIKGoal.RightHand;

            // If we don't need to mirror for left then we need to mirror for right
            if (!mirrorNeededForLeft)
                animator.SetBool(_openDoorMirrorHash, true);
            else
                animator.SetBool(_openDoorMirrorHash, false);
        }

        // Get the IK Weight curve from the animation so we can
        // ramp up and down the IK Weight at the beginning and end
        // of the animation.
        float IKWeight = animator.GetFloat(_handsIKTargetWeightHash);

        // Set the IK target and weight
        animator.SetIKPosition(ikGoal, _IKTarget.position);
        animator.SetIKPositionWeight(ikGoal, IKWeight);

        // Lerp between previous head IK position and weight to the DoorIK position and Weight
        float headDoorIKWeight = IKWeight * _headIKWeight;
        _npc.currentLookAtWeight = Mathf.Lerp(_npc.currentLookAtWeight, headDoorIKWeight, Time.deltaTime);
        Vector3 headLookAtVector = Vector3.Lerp(_npc.currentLookAtPosition, _IKTarget.position, headDoorIKWeight);
        animator.SetLookAtPosition(_IKTarget.position);
        animator.SetLookAtWeight(_npc.currentLookAtWeight);

        // If its a door we are going to repeatedly fail to open
        // lets make sure we don't let it nudge forward which each attempt by
        // lerp it into its ideal position
        if (_door.aiDoorType != AIInteractiveDoorType.AICanOpen || !_attemptSuccessful) {
            transform.position = Vector3.Lerp(transform.position, _standingPosition, Time.deltaTime);

            // Generate a new Quaternion representing the rotation we should have
            Quaternion newRot = Quaternion.LookRotation(_lookDirection);

            // Smoothly rotate to that new rotation over time
            _npc.transform.rotation = Quaternion.Slerp(_npc.transform.rotation, newRot, Time.deltaTime);
        }

        // If we are not processing a door or if we are in the middle of activating it
        // ignore the rest of the function that starts the activation
        if (_activatingDoor || !_processingDoor) return;

        // Are we at the point in the animation where we
        // activate the door opening sequence?
        if (animator.GetFloat(_comChannelHash) > 0.99f) {
            // If this is a door we can open and we are intelligent enough to open it - then open it
            if (_door.aiDoorType == AIInteractiveDoorType.AICanOpen && _attemptSuccessful) {
                _door.Activate(_npc.transform.position, true);
                _previousDoor = _door;
            }
            // Otherwise we have let the Zombie rattle the handle
            // but its time to select somewhere else to go now.
            else {
                //_speedTimer = 20;
                ClearNPCTarget();
                _previousDoor = _door;
            }

            // We are (or have tried) activating the door once this animation so don't try again
            _activatingDoor = true;
        }
    }


    // --------------------------------------------------------------------------------------------
    // Name :   OnResetSystem
    // Desc :   It is possible that a zombie may get ragdolled, hit or killed whilst in the middle
    //          of activating a door. This function is registered as a listener to these zombie
    //          events so we can interrupt the procedure and reset the system.
    // --------------------------------------------------------------------------------------------
    public void OnResetSystem(AIZombieStateMachine npc) {
        if (_door)
            _door.OnPlayerInteractionEvent.RemoveListener(OnAbortProcessing);

        _npc.AnimatorIKEvent.RemoveListener(UpdateNPCIK);
        //_npc.speedOverride = -1;

        _door = null;
        _processingDoor = false;
        _activatingDoor = false;

        _npc.animator.SetBool(_openDoorHash, false);


        // Turn off door processing if dead
        if (_npc.health <= 0)
            enabled = false;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   OnAbortProcessing
    // Desc :   Registered as a listener with the InteractibeDoor component so the player opening
    //          the door while the zombie is opening it aborts the zombie still trying to open
    //          / close the door.
    // --------------------------------------------------------------------------------------------
    public void OnAbortProcessing(CharacterManager charMan, bool success, bool isOpen) {
        OnResetSystem(null);
    }

    // --------------------------------------------------------------------------------------------
    // Name :   ClearNPCTarget
    // Desc :   Used to clear the current target after a failed door open attempt and move onto
    //          the next waypoint in the list.
    // --------------------------------------------------------------------------------------------
    protected void ClearNPCTarget() {
        // Clear the current target
        _npc.ClearTarget();

        // If player tracking is on disable it so the zombie
        // doesn't keep SEEING the player through the door
        _npc.StopTrackingPlayer();

        // _npc.navAgent.isStopped = true;
        // _npc.navAgent.ResetPath();

        // Force the internal Waypoint system of the Zombie
        // to move on to the next waypoint in the list and set that as the new target
        _npc.GetWaypointPosition(true);

        // Force a re-entry into the Patrol state which will set the Nav Path 
        // to the new waypoint
        _npc.SetStateOverride(AIStateType.Patrol, true);
    }
}