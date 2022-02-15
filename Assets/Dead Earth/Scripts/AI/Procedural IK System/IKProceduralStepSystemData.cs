using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum IKStepSystemDirection { up, down }
public enum IKStepSystemType {  Simple, Animated, AnimatedArcs}

// ------------------------------------------------------------------------------------------------
// CLASS    :   IKProceduralStepSystemData
// DESC     :   Contains per-animation settings for the procedural generation of footsteps
//              by animating IK targets
// ------------------------------------------------------------------------------------------------
[CreateAssetMenu(menuName = "Scriptable Objects/AI/IK Procedual Step System Data")]
public  class IKProceduralStepSystemData : ScriptableObject
{
    [Header("General Settings")]
    [Tooltip("IK Feet Targets are actually at the ankle bone usually so this is the distance from the ankle to the floor. Increase this is feet sink into geometry or decrease is feet hover above the ground.")]
    [SerializeField] protected float _footBoneFloorOffset     = 0.175f;
    [Tooltip("Distance above the feet to begin the raycast downwards to detect the graound.")]
    [SerializeField] protected  float _raycastOriginHeight     = 0.75f;
    [Tooltip("Length of the ray to cast downwards to detect the ground plane.")]
    [SerializeField] protected  float _raycastLength           = 2.0f;
    [Tooltip("This is multiplied by deltaTime to increase/decrease the IK to Original blend when coming out of IK step mode.")]
    [SerializeField] protected float _IKCooldownSpeed         = 0.2f;

    [Header("Animated Down Steps Settings")]
    [Tooltip("The type of IK you would like performed.")]
    [SerializeField] protected IKStepSystemType _stepDownType = IKStepSystemType.Simple; 
    [Tooltip("Controls the height of the pelvis during IK stepping. Decrease to look more crouched or increase to look more straightened.")]
    [SerializeField] protected float _stepDownPelvisHeight = 1.0f;
    [Tooltip("The speed at which the pelvis updated to the PelvisHeight when a change in the height of the ground plane is detected.")]
    [SerializeField] protected float _stepDownPelvisAdjustmentSpeed = 3.0f;
    [Range(0.0f, 1.0f)]
    [Tooltip("Weight at which to blend the IK targets with the original animation when stepping downwards.")]
    [SerializeField] protected  float _stepDownIKWeight   = 1.0f;
    [Tooltip("Length of stride")]
    [SerializeField] protected  float _stepDownStrideSize = 0.18f;
    [Tooltip("Width of Stride")]
    [SerializeField] protected float _stepDownStrideWidth = 0.1f;
    [Tooltip("Amount to offset the pelvis forward/backwards during stepping downwards.")]
    [SerializeField] protected  float _stepDownPelvisOffset  = 0.0f;
    [Tooltip("Amount to offset the raycasts in front of or behind the feet when detecting ground.")]
    [SerializeField] protected  float _stepDownRayOffset  = 0.0f;
    [Tooltip("Amount to scale the original speed of the animation when sliding down 'steps' ramp.")]
    [SerializeField] protected  float _stepDownSpeedScale = 0.7f;
    [Tooltip("Amount to apply smoothing between IK positions")]
    [SerializeField] protected float _stepDownFeetSmoothing = 5.0f;
    [Tooltip("Amount to scale the Arc Animation Curve when stepping down.")]
    [SerializeField] protected  float _stepDownArcScale   = 0.15f;

    [Header("Animated Up Steps Settings")]
    [Tooltip("The type of IK you would like performed.")]
    [SerializeField] protected IKStepSystemType _stepUpType = IKStepSystemType.Simple;
    [Tooltip("Controls the height of the pelvis during IK stepping. Decrease to look more crouched or increase to look more straightened.")]
    [SerializeField] protected float _stepUpPelvisHeight = 1.0f;
    [Tooltip("The speed at which the pelvis updated to the PelvisHeight when a change in the height of the ground plane is detected.")]
    [SerializeField] protected float _stepUpPelvisAdjustmentSpeed = 3.0f;
    [Range(0.0f, 1.0f)]
    [Tooltip("Weight at which to blend the IK targets with the original animation when stepping upwards.")]
    [SerializeField] protected  float _stepUpIKWeight     = 1.0f;
    [Tooltip("Length of stride")]
    [SerializeField] protected  float _stepUpStrideSize   = 0.21f;
    [Tooltip("Width of Stride")]
    [SerializeField] protected float _stepUpStrideWidth = 0.1f;
    [Tooltip("Amount to offset the pelvis forward/backwards during stepping downwards.")]
    [SerializeField] protected  float _stepUpPelvisOffset    = 0.0f;
    [Tooltip("Amount to offset the raycasts in front of or behind the feet when detecting ground.")]
    [SerializeField] protected  float _stepUpRayOffset    = 0.15f;
    [Tooltip("Amoutn to scale the original speed to the animation when sliding up 'steps' ramp.")]
    [SerializeField] protected  float _stepUpSpeedScale   = 0.7f;
    [Tooltip("Amount to apply smoothing between IK positions")]
    [SerializeField] protected float _stepUpFeetSmoothing = 5.0f;
    [Tooltip("Amount to scale the Arc Animation Curve when stepping down.")]
    [SerializeField] protected  float _stepUpArcScale     = 0.25f;
    
    // Public Get Functions 'General Settings' 
    public float GetFootBoneFloorOffset     ( float scale)  { return _footBoneFloorOffset * scale;  }
    public float GetRaycastOriginHeight     (float scale )  { return _raycastOriginHeight * scale;  }
    public float GetRaycastLength           (float scale)   { return _raycastLength * scale;        }
    public float GetIKCooldownSpeed         ()              { return _IKCooldownSpeed;      }


    public IKStepSystemType GetIKType ( IKStepSystemDirection dir)
    {
        if (dir == IKStepSystemDirection.up)
            return _stepUpType;
        else
            return _stepDownType;
    }

    public void SetIKType ( IKStepSystemDirection dir, IKStepSystemType type)
    {
        if (dir == IKStepSystemDirection.up)
            _stepUpType = type ;
        else
            _stepDownType = type;
    }

    // Public Get Functions - Direction Dependant
    public float GetIKWeight( IKStepSystemDirection dir, float scale )
    {
        if (dir == IKStepSystemDirection.up)
            return _stepUpIKWeight * scale;
        else
            return _stepDownIKWeight * scale;
    }

    // Public Get Functions - Direction Dependant
    public float GetPelvisHeight(IKStepSystemDirection dir, float scale)
    {
        if (dir == IKStepSystemDirection.up)
            return _stepUpPelvisHeight * scale;
        else
            return _stepDownPelvisHeight * scale;
    }

    // Public Get Functions - Direction Dependant
    public float GetPelvisAdjustmentSpeed(IKStepSystemDirection dir, float scale)
    {
        if (dir == IKStepSystemDirection.up)
            return _stepUpPelvisAdjustmentSpeed * scale;
        else
            return _stepDownPelvisAdjustmentSpeed * scale;
    }

    public float GetStrideSize(IKStepSystemDirection dir, float scale)
    {
        if (dir == IKStepSystemDirection.up)
            return _stepUpStrideSize * scale;
        else
            return _stepDownStrideSize * scale;
    }

    public float GetStrideWidth(IKStepSystemDirection dir, float scale)
    {
        if (dir == IKStepSystemDirection.up)
            return _stepUpStrideWidth * scale;
        else
            return _stepDownStrideWidth * scale;
    }

    public float GetPelvisOffset(IKStepSystemDirection dir, float scale)
    {
        if (dir == IKStepSystemDirection.up)
            return _stepUpPelvisOffset * scale;
        else
            return _stepDownPelvisOffset * scale;
    }

    public float GetRayOffset(IKStepSystemDirection dir, float scale)
    {
        if (dir == IKStepSystemDirection.up)
            return _stepUpRayOffset * scale;
        else
            return _stepDownRayOffset * scale;
    }

    public float GetSpeedScale(IKStepSystemDirection dir, float scale)
    {
        if (dir == IKStepSystemDirection.up)
            return _stepUpSpeedScale * scale;
        else
            return _stepDownSpeedScale * scale;
    }

    public float GetFeetSmoothing(IKStepSystemDirection dir, float scale)
    {
        if (dir == IKStepSystemDirection.up)
            return _stepUpFeetSmoothing * scale;
        else
            return _stepDownFeetSmoothing * scale;
    }

    public float GetArcScale(IKStepSystemDirection dir, float scale)
    {
        if (dir == IKStepSystemDirection.up)
            return _stepUpArcScale * scale;
        else
            return _stepDownArcScale * scale;
    }
}
