﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

// Describes an axis
public enum InteractiveDoorAxisAlignment
{
    XAxis,
    YAxis,
    ZAxis
}

public enum AIInteractiveDoorType
{
    AICanOpen,
    AICantOpen,
    CarvedWhenClosed
}

[System.Serializable]
public class InteractiveDoorStateChanged : UnityEvent<bool>
{
}

[System.Serializable]
public class InteractiveDoorPlayerInteraction : UnityEvent<CharacterManager, bool, bool>
{
}

// ------------------------------------------------------------------------------------------------
// CLASS    :   InteractiveDoorInfo
// DESC     :   Describes the animation properties of a single door in an InteractiveDoor 
// ------------------------------------------------------------------------------------------------
[System.Serializable]
public class InteractiveDoorInfo
{
    // Transform to animate
    public Transform Transform = null;

    // Local rotation axis and amount
    public Vector3 Rotation = Vector3.zero;

    // Local axis of movement and distance along that axis
    public Vector3 Movement = Vector3.zero;

    public Transform FrontSideHandle = null;
    public Transform BackSideHandle = null;

    // The following are used to cache the open and closed position and rotations
    // of the door at startup for easy Lerping/Slerping
    [HideInInspector] public Quaternion ClosedRotation = Quaternion.identity;
    [HideInInspector] public Quaternion OpenRotation = Quaternion.identity;
    [HideInInspector] public Vector3 OpenPosition = Vector3.zero;
    [HideInInspector] public Vector3 ClosedPosition = Vector3.zero;
    [HideInInspector] public NavMeshObstacle NavObstacle = null;
    [HideInInspector] public Collider Collider = null;
}

// ------------------------------------------------------------------------------------------------
// CLASS    :   InteractiveDoor
// DESC     :   Control mechanism for all rotating door constructs
// ------------------------------------------------------------------------------------------------
[RequireComponent(typeof(BoxCollider))]
public class InteractiveDoor : InteractiveItem
{
    // Inpsector Assigned Variables
    [Header("Activation Properties")] [Tooltip("Does the door start open or closed")] [SerializeField]
    protected bool _isClosed = true;

    [Tooltip("Does the door open in both directions")] [SerializeField]
    protected bool _isTwoWay = true;

    [Tooltip("Does the door open automatically when the player walks into its trigger")] [SerializeField]
    protected bool _autoOpen = false;

    [Tooltip("Does the door close automatically after a certain period of time")] [SerializeField]
    protected bool _autoClose = false;

    [Tooltip("The Random time range for the autoclose delay")] [SerializeField]
    protected Vector2 _autoCloseDelay = new Vector2(5.0f, 5.0f);

    [Tooltip("Disable Manual Activation")] [SerializeField]
    protected bool _disableManualActivation = false;

    [Tooltip("How should the size of the box collider grow when the door is open")] [SerializeField]
    protected float _colliderLengthOpenScale = 3.0f;

    [Tooltip("Should we offset the center of the collider when open")] [SerializeField]
    protected bool _offsetCollider = true;

    [Tooltip("A container object used as the parent for any objects the open door should reveal")] [SerializeField]
    protected Transform _contentsMount = null;

    // The axis setup
    [SerializeField] protected InteractiveDoorAxisAlignment _localForwardAxis = InteractiveDoorAxisAlignment.ZAxis;

    [Header("AI and Navigation")]
    [Tooltip(
        "Can AI open this door.\n\nYou should not use AICantOpen with Auto-Opening doors in the general use-case. This can lead to unpredictable AI behaviour.")]
    [SerializeField]
    protected AIInteractiveDoorType _aiDoorType = AIInteractiveDoorType.AICanOpen;

    // Key Values that must be set in app database for door to open
    [Header("Game State Management")] [SerializeField]
    protected List<GameState> _requiredStates = new List<GameState>();

    [SerializeField] protected List<string> _requiredItems = new List<string>();

    // The three text messages to return to HUD based on status of door
    [Header("Message")] [TextArea(3, 10)] [SerializeField]
    protected string _openedHintText = "Door: Press 'Use' to close";

    [TextArea(3, 10)] [SerializeField] protected string _closedHintText = "Door: Press 'Use' to open";
    [TextArea(3, 10)] [SerializeField] protected string _cantActivateHintText = "Door: It's Locked";

    [Header("Door Transforms")]
    // The list of door to objects to animate
    [Tooltip("A list of child transforms to animate")]
    [SerializeField]
    protected List<InteractiveDoorInfo> _doors = new List<InteractiveDoorInfo>();

    [Header("Sounds")] [Tooltip("The AudioCollection to use for the door opening and closing sounds")] [SerializeField]
    protected AudioCollection _doorSounds = null;

    [Tooltip("Optional assignment of a AudioPunchInPunchOut Database")] [SerializeField]
    protected AudioPunchInPunchOutDatabase _audioPunchInPunchOutDatabase = null;

    [Header("Events")] [Tooltip("Raised when the door state changes from Open to Closed or vica versa.")]
    public InteractiveDoorStateChanged OnStateChangedEvent = new InteractiveDoorStateChanged();

    [Tooltip("Raised when the player interacts with the door in some way.")]
    public InteractiveDoorPlayerInteraction OnPlayerInteractionEvent = new InteractiveDoorPlayerInteraction();

    // Private
    protected IEnumerator _coroutine = null;
    protected Vector3 _closedColliderSize = Vector3.zero;
    protected Vector3 _closedColliderCenter = Vector3.zero;
    protected Vector3 _openColliderSize = Vector3.zero;
    protected Vector3 _openColliderCenter = Vector3.zero;
    protected BoxCollider _boxCollider = null;
    protected Plane _plane;
    protected bool _openedFrontside = true;

    // Used for storing info about the door progress during a coroutine
    protected float _normalizedTime = 0.0f;
    protected ulong _oneShotSoundID = 0;

    // Properties
    public bool isOpen {
        get { return !_isClosed; }
    }

    public bool isAutoOpen {
        get { return _autoOpen; }
    }

    public AIInteractiveDoorType aiDoorType {
        get { return _aiDoorType; }
    }

    public int AIID = -1;

    // ---------------------------------------------------------------------------------------------
    // Name :   CarveDoors
    // Desc :   Enables/Disables the NavMesh Carving of Door NavMeshObstacles
    // ---------------------------------------------------------------------------------------------
    protected void CarveDoors(bool carve) {
        foreach (InteractiveDoorInfo door in _doors) {
            if (door.NavObstacle) {
                door.NavObstacle.carving = carve;
                door.NavObstacle.enabled = carve;
            }
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   RepulseAI
    // Desc :   Enables the NavObstacle for a specified period of time to repulse AI
    // ---------------------------------------------------------------------------------------------
    public void RepulseAIWhenClosed(float time) {
        if (!_isClosed) return;
        if (_aiDoorType == AIInteractiveDoorType.CarvedWhenClosed) return;

        foreach (InteractiveDoorInfo door in _doors) {
            if (door.NavObstacle) {
                door.NavObstacle.enabled = true;

                StartCoroutine(DisableRepulseAIWhenClosed(time));
            }
        }
    }

    protected IEnumerator DisableRepulseAIWhenClosed(float time) {
        // Chew up any delay time that has been specified
        yield return new WaitForSeconds(time);

        if (!_isClosed) yield break;
        if (_aiDoorType == AIInteractiveDoorType.CarvedWhenClosed) yield break;

        foreach (InteractiveDoorInfo door in _doors) {
            if (door.NavObstacle) {
                door.NavObstacle.enabled = false;
            }
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   GetText
    // Desc :   Return a string of text to display on the HUD when the player is inspecting this
    //          interactive item
    // --------------------------------------------------------------------------------------------
    public override string GetText() {
        if (_disableManualActivation) return null;

        // We need to test all the states that need to be set to see if this item can be activated
        // as that determines the text we send back
        bool haveInventoryItems = HaveRequiredInvItems();
        bool haveRequiredStates = true;

        // Check the required states are set in the application state database
        if (_requiredStates.Count > 0) {
            if (ApplicationManager.instance == null) haveRequiredStates = false;
            else
                haveRequiredStates = ApplicationManager.instance.AreStatesSet(_requiredStates);
        }

        // What text should we return
        if (_isClosed) {
            if (!haveRequiredStates || !haveInventoryItems) {
                return _cantActivateHintText;
            }
            else {
                return _closedHintText;
            }
        }
        else {
            return _openedHintText;
        }
    }

    // PLACEHOLDER
    protected bool HaveRequiredInvItems() {
        return true;
    }

    // Start is called before the first frame update
    protected override void Start() {
        base.Start();

        // Cache components
        _boxCollider = _collider as BoxCollider;

        // Calculate the open and closed collider sizes and center points
        if (_boxCollider != null) {
            _closedColliderSize = _openColliderSize = _boxCollider.size;
            _closedColliderCenter = _openColliderCenter = _boxCollider.center;
            float offset = 0.0f;

            // Make sure we offset the collider and grow it in the dimension specified as what *we*
            // perceive to be its forward axis
            switch (_localForwardAxis) {
                case InteractiveDoorAxisAlignment.XAxis:
                    _plane = new Plane(transform.right, transform.position);
                    _openColliderSize.x *= _colliderLengthOpenScale;
                    offset = _closedColliderCenter.x - (_openColliderSize.x / 2);
                    _openColliderCenter = new Vector3(offset, _closedColliderCenter.y, _closedColliderCenter.z);
                    break;

                case InteractiveDoorAxisAlignment.YAxis:
                    _plane = new Plane(transform.up, transform.position);
                    _openColliderSize.y *= _colliderLengthOpenScale;
                    offset = _closedColliderCenter.y - (_openColliderSize.y / 2);
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, offset, _closedColliderCenter.z);
                    break;

                case InteractiveDoorAxisAlignment.ZAxis:
                    _plane = new Plane(transform.forward, transform.position);
                    _openColliderSize.z *= _colliderLengthOpenScale;
                    offset = _closedColliderCenter.z - (_openColliderSize.z / 2);
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, _closedColliderCenter.y, offset);

                    break;
            }


            // If we are starting the game OPEN then apply open scale and offsets to collider now
            if (!_isClosed) {
                _boxCollider.size = _openColliderSize;
                if (_offsetCollider) _boxCollider.center = _openColliderCenter;
                _openedFrontside = true;
            }
        }

        // Set all of the doors this object manages to their starting orientations
        foreach (InteractiveDoorInfo door in _doors) {
            if (door != null && door.Transform != null) {
                // It is assumed that all doors are set in the closed position at startup
                // so grab the current rotation quat and store it as the closed rotation
                door.ClosedRotation = door.Transform.localRotation;
                door.ClosedPosition = door.Transform.position;
                door.OpenPosition = door.Transform.position - door.Transform.TransformDirection(door.Movement);
                door.Collider = door.Transform.GetComponent<Collider>();

                // If the door has a nav obstacle then if its closed we wish it to start disabled as we don't
                // want the obstacle to alter the path of the AI. We really only want it to act like a collider
                // for the AI while the door is opening / closing.
                door.NavObstacle = door.Transform.GetComponent<NavMeshObstacle>();
                if (door.NavObstacle)
                    door.NavObstacle.enabled = false;

                // Calculate a rotation to take it into the open position
                Quaternion rotationToOpen = Quaternion.Euler(door.Rotation);

                // If the door is supposed to start open then apply rotation/Movement to open it
                if (!_isClosed) {
                    door.Transform.localRotation = door.ClosedRotation * rotationToOpen;
                    door.Transform.position = door.OpenPosition;
                    if (door.NavObstacle) {
                        door.NavObstacle.enabled = true;
                        door.NavObstacle.carving = true;
                    }
                }
                // Else the door is closed and therefore we should enable carving if that mdoe has been selected
                else {
                    if (door.NavObstacle && _aiDoorType == AIInteractiveDoorType.CarvedWhenClosed) {
                        door.NavObstacle.enabled = true;
                        door.NavObstacle.carving = true;
                    }
                }
            }
        }


        // Finally disable colliders of any contents if in the closed position
        if (_contentsMount != null) {
            Collider[] colliders = _contentsMount.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders) {
                if (_isClosed)
                    col.enabled = false;
                else
                    col.enabled = true;
            }
        }

        // Animation is not currently in progress
        _coroutine = null;

        // Let any dependants know the state of the door at startup (Pass true if open)
        OnStateChangedEvent.Invoke(!_isClosed);
    }

    // --------------------------------------------------------------------------------------------
    // Name :   Activate
    // Desc :   Called by the character manager to activate the door (open / close) it.
    // --------------------------------------------------------------------------------------------
    public override void Activate(CharacterManager characterManager) {
        // The door can not be activate manually
        if (_disableManualActivation) return;

        // Check the required states are set in the application state database
        // to allow us to be able to interact with this item
        bool haveRequiredStates = true;
        if (_requiredStates.Count > 0) {
            if (ApplicationManager.instance == null) haveRequiredStates = false;

            haveRequiredStates = ApplicationManager.instance.AreStatesSet(_requiredStates);
        }

        // Only activate the door if we meet all reuirements
        if (haveRequiredStates && HaveRequiredInvItems()) {
            // Stop any animation current running and activate the new animation
            if (_coroutine != null) StopCoroutine(_coroutine);

            // Inform listeners that the character manager has successfully activated the door.
            // Also pass in the current Open Status of the door before the state change takes place.
            OnPlayerInteractionEvent.Invoke(characterManager, true, !_isClosed);

            _coroutine = Activate(_plane.GetSide(characterManager.transform.position));
            StartCoroutine(_coroutine);
        }
        else {
            // Inform listeners that the character manager has unsuccessfully tried to activate the door.
            // Also pass in the current Open Status of the door.
            OnPlayerInteractionEvent.Invoke(characterManager, true, !_isClosed);

            // We are not allowed to activate this door so play its Can't Activate sound
            if (_doorSounds && AudioManager.instance) {
                // Fetch a sound from the open bank
                AudioClip clip = _doorSounds[2];
                if (clip) {
                    _oneShotSoundID = AudioManager.instance.PlayOneShotSound(_doorSounds.audioGroup,
                        clip,
                        transform.position,
                        _doorSounds.volume,
                        _doorSounds.spatialBlend,
                        _doorSounds.priority);
                }
            }
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   IsOnFrontSide
    // Desc :   Returns true if the passed world space position is on the front side of the door
    // --------------------------------------------------------------------------------------------
    public bool IsOnFrontSide(Vector3 position) {
        return _plane.GetSide(position);
    }

    // --------------------------------------------------------------------------------------------
    // Name :   GetIKTarget
    // Desc :   Returns the transform of the IKTarget of the handle to use for the door
    //          with the passed collider instance ID
    // --------------------------------------------------------------------------------------------
    public Transform GetIKTarget(Vector3 position, int doorInstanceID) {
        for (int i = 0; i < _doors.Count; i++) {
            if (_doors[i].Collider.GetInstanceID().Equals(doorInstanceID))
                return _plane.GetSide(position) ? _doors[i].FrontSideHandle : _doors[i].BackSideHandle;
        }

        return null;
    }


    // --------------------------------------------------------------------------------------------
    // Name :   Activate
    // Desc :   Called by AI Door Trigger to open a door
    // --------------------------------------------------------------------------------------------
    public virtual void Activate(Vector3 position, bool skipStartTime) {
        // Stop any animation current running and activate the new animation
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = Activate(_plane.GetSide(position), false, 0.0f, skipStartTime);
        StartCoroutine(_coroutine);
    }

    // --------------------------------------------------------------------------------------------
    // Name :   Activate (Coroutine)
    // Desc :   This is the function that perform the actual animation of the door 
    // --------------------------------------------------------------------------------------------
    private IEnumerator Activate(bool frontSide, bool autoClosing = false, float delay = 0.0f,
        bool skipStartTime = false) {
        AudioClip clip = null;

        // Chew up any delay time that has been specified
        yield return new WaitForSeconds(delay);

        // Used to sync animation with sound
        float duration = 1.5f;
        float time = 0.0f;
        float startAnimTime = 0.0f;

        if (!_isTwoWay) frontSide = true;

        // Ping Pong Normalized Time
        if (_normalizedTime > 0.0f)
            _normalizedTime = 1 - _normalizedTime;

        // If the door is closed then we need to open it
        if (_isClosed) {
            // Consider it open from this point on
            _isClosed = false;

            if (_normalizedTime > 0)
                frontSide = _openedFrontside;

            // Record side we opened from
            _openedFrontside = frontSide;

            // Find a sound to play
            if (_doorSounds && AudioManager.instance) {
                // Stop any previous animaton sound that might be playing
                AudioManager.instance.StopSound(_oneShotSoundID);

                // Fetch a sound from the open bank
                clip = _doorSounds[0];
                if (clip) {
                    // BY default we set the length of the animation to the length of the audo clip
                    duration = clip.length;

                    // Let's see if this clip has markers in the PunchInPunchOut database
                    if (_audioPunchInPunchOutDatabase) {
                        AudioPunchInPunchOutInfo info = _audioPunchInPunchOutDatabase.GetClipInfo(clip);

                        // If it has then adjust start time and duration to match markers
                        if (info != null) {
                            // Get the StartTime registered with this clip 
                            startAnimTime = Mathf.Min(info.StartTime, clip.length);

                            // Assuming the end time is larger than the start time the duration of the animation
                            // is simply the time between the two markers
                            if (info.EndTime >= startAnimTime) {
                                duration = info.EndTime - startAnimTime;
                            }
                            else {
                                // Other wise it is assumed we with the play the clip to the end so we just
                                // subtract the start time. This allows us to put zero in the end time slot
                                // instead of having to look up the exact length of each clip we use
                                duration = clip.length - startAnimTime;
                            }
                        }
                    }

                    // If we are already part-way into the animation then we need to start the sound
                    // some way from the beginning
                    float playbackOffset = 0;
                    if (_normalizedTime > 0.0f || skipStartTime) {
                        playbackOffset = startAnimTime + (duration * _normalizedTime);
                        startAnimTime = 0.0f;
                    }

                    _oneShotSoundID = AudioManager.instance.PlayOneShotSound(_doorSounds.audioGroup,
                        clip,
                        transform.position,
                        _doorSounds.volume,
                        _doorSounds.spatialBlend,
                        _doorSounds.priority,
                        playbackOffset);
                }
            }

            // Determine perceived forward axis and offset and scale the collider in that dimension
            float offset = 0.0f;
            switch (_localForwardAxis) {
                case InteractiveDoorAxisAlignment.XAxis:
                    offset = _openColliderSize.x / 2.0f;
                    if (!frontSide) offset = -offset;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x - offset, _closedColliderCenter.y,
                        _closedColliderCenter.z);
                    break;

                case InteractiveDoorAxisAlignment.YAxis:
                    offset = _openColliderSize.y / 2.0f;
                    if (!frontSide) offset = -offset;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, _closedColliderCenter.y - offset,
                        _closedColliderCenter.z);
                    break;

                case InteractiveDoorAxisAlignment.ZAxis:
                    offset = _openColliderSize.z / 2.0f;
                    if (!frontSide) offset = -offset;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, _closedColliderCenter.y,
                        _closedColliderCenter.z - offset);
                    break;
            }

            if (_offsetCollider) _boxCollider.center = _openColliderCenter;
            _boxCollider.size = _openColliderSize;

            // If StartAnimTime is non-zero we need to let some of the sound play before we start animating
            // the door so let's chew up that time here.
            if (startAnimTime > 0.0f)
                yield return new WaitForSeconds(startAnimTime);

            // Set the starting time of the animation
            time = duration * _normalizedTime;

            CarveDoors(false);

            // Now complete the animation for each door
            while (time <= duration) {
                // Calculate new _normalizedTime
                _normalizedTime = time / duration;

                foreach (InteractiveDoorInfo door in _doors) {
                    if (door != null && door.Transform != null) {
                        // Calculate new position and local rotation
                        door.Transform.position = Vector3.Lerp(door.ClosedPosition, door.OpenPosition, _normalizedTime);
                        door.Transform.localRotation = door.ClosedRotation *
                                                       Quaternion.Euler(frontSide
                                                           ? door.Rotation * _normalizedTime
                                                           : -door.Rotation * _normalizedTime);

                        // Enable Nav Obstacle
                        if (door.NavObstacle && !door.NavObstacle.enabled)
                            door.NavObstacle.enabled = true;
                    }
                }

                yield return null;
                time += Time.deltaTime;
            }


            // Disable colliders of any contents if in the closed position
            if (_contentsMount != null) {
                Collider[] colliders = _contentsMount.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders) {
                    col.enabled = true;
                }
            }

            // Reset time to zero 
            _normalizedTime = 0.0f;

            // Carve the open door into the nav mesh
            CarveDoors(true);

            // Let any dependants know the door is now closed
            OnStateChangedEvent.Invoke(!_isClosed);

            // If autoClose is active then spawn a new coroutine to close it again
            if (_autoClose) {
                _coroutine = Activate(frontSide, true, Random.Range(_autoCloseDelay.x, _autoCloseDelay.y));
                StartCoroutine(_coroutine);
            }

            yield break;
        }

        // The door is open so we wish to close it
        else {
            _isClosed = true;

            // Cache the door in its open position 
            foreach (InteractiveDoorInfo door in _doors) {
                if (door != null && door.Transform != null) {
                    Quaternion rotationToOpen = Quaternion.Euler(_openedFrontside ? door.Rotation : -door.Rotation);
                    door.OpenRotation = door.ClosedRotation * rotationToOpen;
                }
            }

            // Finally disable colliders of any contents if in the closed position
            if (_contentsMount != null) {
                Collider[] colliders = _contentsMount.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders) {
                    col.enabled = false;
                }
            }

            // Find a sound to play
            if (_doorSounds && AudioManager.instance) {
                // Stop any previous animaton sound that might be playing
                AudioManager.instance.StopSound(_oneShotSoundID);

                // Fetch a sound from the open bank
                clip = _doorSounds[autoClosing ? 3 : 1];
                if (clip) {
                    // BY default we set the length of the animation to the length of the audo clip
                    duration = clip.length;

                    if (_audioPunchInPunchOutDatabase) {
                        AudioPunchInPunchOutInfo info = _audioPunchInPunchOutDatabase.GetClipInfo(clip);
                        if (info != null) {
                            startAnimTime = Mathf.Min(info.StartTime, clip.length);
                            if (info.EndTime >= startAnimTime) {
                                duration = info.EndTime - startAnimTime;
                            }
                            else {
                                duration = clip.length - startAnimTime;
                            }
                        }
                    }

                    float playbackOffset = 0;
                    if (_normalizedTime > 0.0f || skipStartTime) {
                        playbackOffset = startAnimTime + (duration * _normalizedTime);
                        startAnimTime = 0.0f;
                    }

                    _oneShotSoundID = AudioManager.instance.PlayOneShotSound(_doorSounds.audioGroup,
                        clip,
                        transform.position,
                        _doorSounds.volume,
                        _doorSounds.spatialBlend,
                        _doorSounds.priority,
                        playbackOffset);
                }
            }

            if (startAnimTime > 0.0f)
                yield return new WaitForSeconds(startAnimTime);

            // Set the starting time
            time = duration * _normalizedTime;

            // Make sure the doors are not carving while closing
            CarveDoors(false);

            // Close over time
            while (time <= duration) {
                _normalizedTime = time / duration;

                foreach (InteractiveDoorInfo door in _doors) {
                    if (door != null && door.Transform != null) {
                        door.Transform.position = Vector3.Lerp(door.OpenPosition, door.ClosedPosition, _normalizedTime);
                        door.Transform.localRotation =
                            Quaternion.Lerp(door.OpenRotation, door.ClosedRotation, _normalizedTime);

                        // Enable Nav Obstacle
                        if (door.NavObstacle && !door.NavObstacle.enabled)
                            door.NavObstacle.enabled = true;
                    }
                }

                yield return null;
                time += Time.deltaTime;
            }

            foreach (InteractiveDoorInfo door in _doors) {
                if (door != null && door.Transform != null) {
                    door.Transform.localRotation = door.ClosedRotation;
                    door.Transform.position = door.ClosedPosition;

                    // Enable Nav Obstacle
                    if (door.NavObstacle) {
                        if (_aiDoorType != AIInteractiveDoorType.CarvedWhenClosed) {
                            door.NavObstacle.enabled = false;
                            door.NavObstacle.carving = false;
                        }
                        else {
                            door.NavObstacle.enabled = true;
                            door.NavObstacle.carving = true;
                        }
                    }
                }
            }


            _boxCollider.size = _closedColliderSize;
            _boxCollider.center = _closedColliderCenter;

            // Signal the door is open event
            OnStateChangedEvent.Invoke(!_isClosed);
        }

        _normalizedTime = 0.0f;
        _coroutine = null;
        yield break;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   OnTriggerEnter
    // Desd :   Used to automatically trigger the opening of a door
    // --------------------------------------------------------------------------------------------
    protected void OnTriggerEnter(Collider other) {
        if (!_autoOpen || !_isClosed) return;
        bool haveRequiredStates = true;
        if (_requiredStates.Count > 0) {
            if (ApplicationManager.instance == null) haveRequiredStates = false;
            else
                haveRequiredStates = ApplicationManager.instance.AreStatesSet(_requiredStates);
        }

        // Only activate the door if we meet all reuirements
        if (haveRequiredStates && HaveRequiredInvItems()) {
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = Activate(_plane.GetSide(other.transform.position));
            StartCoroutine(_coroutine);
        }
        else {
            // We are not allowed to activate this door so play its Can't Activate sound
            if (_doorSounds && AudioManager.instance) {
                // Fetch a sound from the open bank
                AudioClip clip = _doorSounds[2];
                if (clip) {
                    _oneShotSoundID = AudioManager.instance.PlayOneShotSound(_doorSounds.audioGroup,
                        clip,
                        transform.position,
                        _doorSounds.volume,
                        _doorSounds.spatialBlend,
                        _doorSounds.priority);
                }
            }
        }
    }
}