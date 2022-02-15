using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKProceduralStepSystemPlayer : MonoBehaviour
{
    // Inspector
    [Header("Runtime Settings")]
    [Tooltip("Layer Mask for the Steps System to test for Step Colliders.")]
    [SerializeField]
    protected LayerMask _stepSystemColliderLayer;

    [Tooltip("Amount to scale the Step System data for this NPC instance.")] [SerializeField]
    protected float _stepSystemDataScale = 1;

    [Tooltip("The amount of weight the IK Step System should contribute the final foot positions.")]
    [Range(0, 1)]
    [SerializeField]
    protected float _stepSystemWeight = 1;

    // Animator Hashes
    protected int _leftIKHash = Animator.StringToHash("IKSSLeftStride");
    protected int _rightIKHash = Animator.StringToHash("IKSSRightStride");
    protected int _leftFootArcHash = Animator.StringToHash("IKSSLeftFootArc");
    protected int _rightFootArcHash = Animator.StringToHash("IKSSRightFootArc");
    protected int _plantFootLeftHash = Animator.StringToHash("IKSSLeftFootPlanted");
    protected int _plantFootRightHash = Animator.StringToHash("IKSSRightFootPlanted");

    // Internals
    protected Animator _animator = null;
    protected AIStateMachine _stateMachine = null;

    // Steps System Data
    protected IKProceduralStepSystemData _stepSystemData = null;

    protected float _prevPelvisHeight = float.MinValue;
    protected int _leftPreviousColliderID = 0;
    protected int _rightPreviousColliderID = 0;
    protected int _firstStepColliderID = 0;
    protected float _leftIKCooldown = 0;
    protected float _rightIKCooldown = 0;
    protected Vector3 _prevLeftRayOrigin = new Vector3(float.MinValue, float.MinValue, float.MinValue);
    protected Vector3 _prevRightRayOrigin = new Vector3(float.MinValue, float.MinValue, float.MinValue);
    protected bool _goingUp = true;
    protected bool _onLeftStep = false;
    protected bool _onRightStep = false;
    protected float _prevLeftIKY = float.MaxValue;
    protected float _prevRightIKY = float.MaxValue;

    // Used to store the target we wish our feet to lerp toward
    Vector3 leftTarget = Vector3.zero, rightTarget = Vector3.zero;

    // ---------------------------------------------------------------------------------------------
    // Name :   Start
    // Desc :   Called prior to first update to cache needed component
    // ---------------------------------------------------------------------------------------------
    protected virtual void Start() {
        // Cache component references
        _animator = GetComponent<Animator>();
        _stateMachine = GetComponent<AIStateMachine>();

        // Fetch all IKProceduralStepSystemSMB derived behaviours from the animator
        // and set their State Machine and Ik Player references.
        if (_animator) {
            IKProceduralStepSystemSMB[] scripts = _animator.GetBehaviours<IKProceduralStepSystemSMB>();
            foreach (IKProceduralStepSystemSMB script in scripts) {
                script.stateMachine = _stateMachine;
                script.ikProcPlayer = this;
            }
        }
    }

    public IKProceduralStepSystemData data {
        set { _stepSystemData = value; }
        get { return _stepSystemData; }
    }

    protected virtual void Update() {
        if (!_stateMachine) return;

        // What direction are we heading (if on steps)
        IKStepSystemDirection dir = _goingUp ? IKStepSystemDirection.up : IKStepSystemDirection.down;

        // If this is simple IK or we don't have data return 1 - no speed scaling
        if (!_stepSystemData || _stepSystemData.GetIKType(dir) == IKStepSystemType.Simple)
            _stateMachine.speedModifier = 1.0f;
        else
            // Using Animated IK so return the Up/Down speed if we are on steps
        if ((_onLeftStep || _onRightStep) && _stepSystemData) {
            _stateMachine.speedModifier = _stepSystemData.GetSpeedScale(dir, _stepSystemDataScale);
        }
        else
            // We are using animated IK but are no longer on steps so implemented the speed ramp up to normal speed during the IK cooldown period
            _stateMachine.speedModifier = Mathf.Lerp(1.0f,
                _stepSystemData.GetSpeedScale(_goingUp ? IKStepSystemDirection.up : IKStepSystemDirection.down,
                    _stepSystemDataScale), (_leftIKCooldown + _rightIKCooldown) / 2.0f);
    }

    protected virtual void OnAnimatorIK(int layerIndex) {
        // If step system isn't enabled the bail
        if (_stepSystemData == null) return;

        // First call of the function so give previous pelvis height the current height
        // so we don't have a bogus value on first update
        if (_prevPelvisHeight.Equals(float.MinValue)) {
            _prevPelvisHeight = _animator.bodyPosition.y;
        }

        if (!_onLeftStep && !_onRightStep) {
            _firstStepColliderID = 0;
        }

        // Which direction are we currently travelling in up or down the steps
        IKStepSystemDirection dir = _goingUp ? IKStepSystemDirection.up : IKStepSystemDirection.down;

        // What type of IK are we doing
        IKStepSystemType IKType = _stepSystemData.GetIKType(dir);

        // Used to record the Y of the step contact points of the ray so we can adjust the hips to the lowest contact point so feet reach
        float leftFootContactHeight = float.MaxValue;
        float rightFootContactHeight = float.MaxValue;

        // Read from animator whether the left or right foot should be planted
        float leftPlant = _animator.GetFloat(_plantFootLeftHash);
        float rightPlant = _animator.GetFloat(_plantFootRightHash);
        float leftStride = _animator.GetFloat(_leftIKHash);
        float rightStride = _animator.GetFloat(_rightIKHash);

        // get the Arc Scale, the IK Weight and the Pelvis Speed Adjustment values.
        float arcScale = _stepSystemData.GetArcScale(dir, _stepSystemDataScale);
        ;
        float IKWeight = _stepSystemData.GetIKWeight(dir, _stepSystemDataScale) * _stepSystemWeight;
        float pelvisAdjustmentSpeed = _stepSystemData.GetPelvisAdjustmentSpeed(dir, _stepSystemDataScale);
        float rayFootOffset = _stepSystemData.GetRayOffset(dir, _stepSystemDataScale);
        float feetSmoothing = _stepSystemData.GetFeetSmoothing(dir, _stepSystemDataScale);

        // Used to store the resulting IK targets in this function
        Vector3 LeftIKPosition;
        Vector3 RightIKPosition;
        Vector3 leftFootRayOrigin;
        Vector3 rightFootRayOrigin;
        Vector3 leftIKFoot = Vector3.zero;
        Vector3 rightIKFoot = Vector3.zero;

        if (IKType != IKStepSystemType.Simple) {
            // Start with ray origin at base of character transform but adjust out left and right by the stide width
            leftFootRayOrigin = transform.position -
                                transform.right * _stepSystemData.GetStrideWidth(dir, _stepSystemDataScale);
            rightFootRayOrigin = transform.position +
                                 transform.right * _stepSystemData.GetStrideWidth(dir, _stepSystemDataScale);

            // If a foot is not planted then position along the forward vector acording the IK curves for the left and right feet in the animation state scaled by stride size.
            // Also add on any pelvis offset. Otherwise, if foot is planted then use same position from previous frame
            if (leftPlant < 0.9f || !_onLeftStep ||
                _prevLeftRayOrigin.Equals(new Vector3(float.MinValue, float.MinValue, float.MinValue)))
                leftFootRayOrigin += transform.forward *
                                     (leftStride *
                                      _stepSystemData.GetStrideSize(dir, _stepSystemDataScale) +
                                      _stepSystemData.GetPelvisOffset(dir, _stepSystemDataScale));
            else
                leftFootRayOrigin = _prevLeftRayOrigin;

            if (rightPlant < 0.9f || !_onRightStep ||
                _prevRightRayOrigin.Equals(new Vector3(float.MinValue, float.MinValue, float.MinValue)))
                rightFootRayOrigin += transform.forward *
                                      (rightStride *
                                       _stepSystemData.GetStrideSize(dir, _stepSystemDataScale) +
                                       _stepSystemData.GetPelvisOffset(dir, _stepSystemDataScale));
            else
                rightFootRayOrigin = _prevRightRayOrigin;

            // Copy these ray positions to cary them into the next frame
            _prevLeftRayOrigin = leftFootRayOrigin;
            _prevRightRayOrigin = rightFootRayOrigin;
            leftIKFoot = leftFootRayOrigin;
            rightIKFoot = rightFootRayOrigin;

            // If we wish the ray origin to seek slightly ahead or behind the feet position for the next step then add the specified offset along the
            // forward vector fo the transform.
            leftFootRayOrigin += transform.forward * rayFootOffset;
            rightFootRayOrigin += transform.forward * rayFootOffset;
        }
        else {
            leftFootRayOrigin = _animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            rightFootRayOrigin = _animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
        }


        // Finally, move the ray origin upwards to the correct casting height
        leftFootRayOrigin.y += _stepSystemData.GetRaycastOriginHeight(_stepSystemDataScale);
        rightFootRayOrigin.y += _stepSystemData.GetRaycastOriginHeight(_stepSystemDataScale);

        Debug.DrawLine(leftFootRayOrigin, leftFootRayOrigin + Vector3.down);
        Debug.DrawLine(rightFootRayOrigin, rightFootRayOrigin + Vector3.down);

        //Raycast handling section 
        RaycastHit feetOutHit = new RaycastHit();
        bool IKProcessed = false;

        float snapOffset = 0.1f;

        // =========================================================================================    
        // Raycast the Left Foot first
        // =========================================================================================
        if (Physics.Raycast(leftFootRayOrigin, Vector3.down, out feetOutHit,
            _stepSystemData.GetRaycastLength(_stepSystemDataScale), _stepSystemColliderLayer)) {
            // Measure the angle between our forward vector and our step collider forward vector. If facing away we are
            // going up steps otherwise going down. (Colliders MUST be placed in rhe scene in this way)
            _goingUp = Vector3.Angle(feetOutHit.transform.right, transform.forward) < 90 ? true : false;

            // We are on stairs and will process Stair IK in this frame
            IKProcessed = true;

            // Remember the height of the point of impact so we can adjust pelvis later
            leftFootContactHeight = feetOutHit.point.y;

            // If we are supposed to plant the foot at this point and we are not over the same step as the other foot
            if ((leftPlant > 0.9f && feetOutHit.collider.GetInstanceID() != _rightPreviousColliderID) ||
                _firstStepColliderID == 0 ||
                (_firstStepColliderID == _leftPreviousColliderID) ||
                IKType != IKStepSystemType.AnimatedArcs) {
                if (leftPlant > 0.9 && feetOutHit.collider.GetInstanceID() == _firstStepColliderID)
                    _firstStepColliderID = int.MinValue;

                leftTarget = IKType != IKStepSystemType.Simple
                    ? leftIKFoot /*feetOutHit.collider.ClosestPoint(leftFootRayOrigin)*/
                    : leftFootRayOrigin;

                leftTarget.y = feetOutHit.point.y + _stepSystemData.GetFootBoneFloorOffset(_stepSystemDataScale);


                if (IKType != IKStepSystemType.Simple)
                    leftTarget += (_goingUp ? -feetOutHit.transform.right : feetOutHit.transform.right) * snapOffset;

                // This is our IK Position for this frame
                LeftIKPosition = leftTarget;
                LeftIKPosition.y = Mathf.Lerp(!_prevLeftIKY.Equals(float.MaxValue) ? _prevLeftIKY : LeftIKPosition.y,
                    LeftIKPosition.y, Time.deltaTime * feetSmoothing);
                _prevLeftIKY = LeftIKPosition.y;

                if (_firstStepColliderID == 0)
                    _firstStepColliderID = feetOutHit.collider.GetInstanceID();

                // Set IK Rotation to match the surface normal 
                _animator.SetIKRotation(AvatarIKGoal.LeftFoot,
                    Quaternion.FromToRotation(Vector3.up, feetOutHit.normal) * transform.rotation);

                _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, IKWeight);

                // Remember this collider ID so we don't try to process it again
                _leftPreviousColliderID = feetOutHit.collider.GetInstanceID();
            }
            else {
                // Foot should not be planted so set its position according to the foot curves in the animation and adjust height
                // by the arc curve
                LeftIKPosition = leftIKFoot;
                LeftIKPosition.y = feetOutHit.point.y + _stepSystemData.GetFootBoneFloorOffset(_stepSystemDataScale) +
                                   _animator.GetFloat(_leftFootArcHash) * arcScale;
                LeftIKPosition.y = Mathf.Lerp(!_prevLeftIKY.Equals(float.MaxValue) ? _prevLeftIKY : LeftIKPosition.y,
                    LeftIKPosition.y, Time.deltaTime * feetSmoothing);
                _prevLeftIKY = LeftIKPosition.y;
            }

            // Set Foot IK Goals and weight
            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, LeftIKPosition);
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, IKWeight);

            // We have interected the correct layer type so we are on stairs
            _onLeftStep = true;
            _leftIKCooldown = 1.0f;
        }
        else {
            // This foot is not on steps any more so clear goals
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0);
            _leftPreviousColliderID = 0;
            _prevLeftIKY = float.MaxValue;
            _onLeftStep = false;
        }


        // =========================================================================================    
        // Raycast the Right Foot 
        // =========================================================================================
        if (Physics.Raycast(rightFootRayOrigin, Vector3.down, out feetOutHit,
            _stepSystemData.GetRaycastLength(_stepSystemDataScale), _stepSystemColliderLayer)) {
            _goingUp = Vector3.Angle(feetOutHit.transform.right, transform.forward) < 90 ? true : false;
            IKProcessed = true;
            rightFootContactHeight = feetOutHit.point.y;

            if ((rightPlant > 0.9f && feetOutHit.collider.GetInstanceID() != _leftPreviousColliderID) ||
                _firstStepColliderID == 0 ||
                _firstStepColliderID == _rightPreviousColliderID ||
                IKType != IKStepSystemType.AnimatedArcs) {
                if (rightPlant > 0.9 && feetOutHit.collider.GetInstanceID() == _firstStepColliderID)
                    _firstStepColliderID = int.MinValue;

                rightTarget = IKType != IKStepSystemType.Simple
                    ?
                    /*feetOutHit.collider.ClosestPoint(rightFootRayOrigin)*/ rightIKFoot
                    : rightFootRayOrigin;
                rightTarget.y = feetOutHit.point.y + _stepSystemData.GetFootBoneFloorOffset(_stepSystemDataScale);


                if (IKType != IKStepSystemType.Simple)
                    rightTarget += (_goingUp ? -feetOutHit.transform.right : feetOutHit.transform.right) * snapOffset;

                RightIKPosition = rightTarget;
                RightIKPosition.y =
                    Mathf.Lerp(!_prevRightIKY.Equals(float.MaxValue) ? _prevRightIKY : RightIKPosition.y,
                        RightIKPosition.y, Time.deltaTime * feetSmoothing);
                _prevRightIKY = RightIKPosition.y;

                if (_firstStepColliderID == 0)
                    _firstStepColliderID = feetOutHit.collider.GetInstanceID();

                //finding our feet ik positions from the sky position
                _animator.SetIKRotation(AvatarIKGoal.RightFoot,
                    Quaternion.FromToRotation(Vector3.up, feetOutHit.normal) * transform.rotation);

                _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, IKWeight);

                _rightPreviousColliderID = feetOutHit.collider.GetInstanceID();
            }
            else {
                RightIKPosition = rightIKFoot;
                RightIKPosition.y = feetOutHit.point.y + _stepSystemData.GetFootBoneFloorOffset(_stepSystemDataScale) +
                                    _animator.GetFloat(_rightFootArcHash) * arcScale;
                RightIKPosition.y =
                    Mathf.Lerp(!_prevRightIKY.Equals(float.MaxValue) ? _prevRightIKY : RightIKPosition.y,
                        RightIKPosition.y, Time.deltaTime * feetSmoothing);
                _prevRightIKY = RightIKPosition.y;
            }

            // Set IK Goals
            _animator.SetIKPosition(AvatarIKGoal.RightFoot, RightIKPosition);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, IKWeight);
            _onRightStep = true;
            _rightIKCooldown = 1.0f;
        }
        else {
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0);
            _rightPreviousColliderID = 0;
            _prevRightIKY = float.MaxValue;
            _onRightStep = false;
        }

        // Get the lowest foot so we can move the hips of the character down so it touches the floor
        float hipDisplacement = leftFootContactHeight < rightFootContactHeight
            ? leftFootContactHeight
            : rightFootContactHeight;

        // Get current pelvis position from the animator
        Vector3 currentBodyPosition = _animator.bodyPosition;

        // If any IK was processed - a ray intersection happened - then lerp towards the new pelvis height
        if (IKProcessed)
            currentBodyPosition.y = Mathf.Lerp(_prevPelvisHeight,
                hipDisplacement + _stepSystemData.GetPelvisHeight(dir, _stepSystemDataScale),
                Time.deltaTime * pelvisAdjustmentSpeed);

        // Set the new pelvis position
        _animator.bodyPosition = currentBodyPosition;

        // Remember hip height to lerp from next frame
        _prevPelvisHeight = currentBodyPosition.y;


        if (IKType != IKStepSystemType.Simple) {
            // Process IK Cooldown period for each leg
            // Decrement coodown timers
            float smooth = _stepSystemData.GetIKCooldownSpeed();
            _leftIKCooldown -= Time.deltaTime * smooth;
            _rightIKCooldown -= Time.deltaTime * smooth;

            // If cooling down left foot generate procedural footstep position
            // set it as an IK target and lower IK weight to zero over time to
            // blend into the original animation
            if (!_onLeftStep && _leftIKCooldown > 0.0f) {
                Vector3 procLeftFoot = leftIKFoot;
                procLeftFoot.y += _animator.GetFloat(_leftFootArcHash) * arcScale;
                _animator.SetIKPosition(AvatarIKGoal.LeftFoot, procLeftFoot);
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, _leftIKCooldown);
            }

            // Do the same for right foot
            if (!_onRightStep && _rightIKCooldown > 0.0f) {
                Vector3 procRightFoot = rightIKFoot;
                procRightFoot.y += _animator.GetFloat(_rightFootArcHash) * arcScale;
                _animator.SetIKPosition(AvatarIKGoal.RightFoot, procRightFoot);
                _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, _rightIKCooldown);
            }
        }
    }
}