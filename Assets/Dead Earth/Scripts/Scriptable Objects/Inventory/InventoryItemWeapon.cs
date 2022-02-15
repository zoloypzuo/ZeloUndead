using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InventoryItemWeaponShakeType
{
    OnFire,
    OnHit
}

// ------------------------------------------------------------------------------------------------
// CLASS    :   InventoryItemWeapon
// DESC     :   Represents a Weapon in the game and its capabilities
// ------------------------------------------------------------------------------------------------
[CreateAssetMenu(menuName = "Scriptable Objects/Inventory System/Items/Weapon")]
public class InventoryItemWeapon : InventoryItem
{
    // Inspector Assigned
    [Header("Weapon Properties")] [Tooltip("Is this a Single Handed or Dual Handed weapon")] [SerializeField]
    protected InventoryWeaponType _weaponType = InventoryWeaponType.SingleHanded;

    [Tooltip("Is this a Melee Weapon or requires Ammunition")] [SerializeField]
    protected InventoryWeaponFeedType _weaponFeedType = InventoryWeaponFeedType.None;

    [Tooltip("What InventoryItem object does this weapon use for Ammunition.")] [SerializeField]
    protected InventoryItemAmmo _ammo = null;

    [Tooltip("What is the Ammo Capacity of the gun.\n\nOnly required for guns that support non-partial reload type.")]
    [SerializeField]
    protected int _ammoCapacity = 0;

    [Tooltip("What is the Reload Type (Partial or Non-Partial)\n\n" +
             "Partial - Individual rounds/cartridges can be loaded into the gun one at a time. Ammo capacity is determined by ammoCapacity (above) of the weapon.\n\n" +
             "NonPartial - Reloads are done by switching magazines. Weapon ammo cpacity is determined by capacity of ammo item.")]
    [SerializeField]
    protected InventoryWeaponReloadType _reloadType = InventoryWeaponReloadType.None;

    [Tooltip("Max Range in meters of this weapon.")] [SerializeField]
    protected float _range = 0.0f;

    [Tooltip("Sound Radius")] [Range(0, 20)] [SerializeField]
    protected int _soundRadius = 1;

    [Tooltip("Should this weapon auto-fire when FIRE button is being held in a pressed state.")] [SerializeField]
    protected bool _autoFire = false;

    [Tooltip("Does this weapon have a dual firing more (like a sniper mode)")] [SerializeField]
    protected bool _dualMode = false;

    [Tooltip("How much the condition of the weapon depletes wth each use")] [Range(0, 100)] [SerializeField]
    protected float _conditionDepletion = 1.0f;

    [Header("Damage Properties")]
    [Tooltip("Thinkness of Raycast used for raycasting potential damage. \n\n" +
             "If zero, a standard raycast is used.\nIf non-zero, a SphereCast is used with the desired radius.\n\n" +
             "Thickness can be used to emulate a blast radius like in the case of a Shotgun or Gravity Gun.")]
    [SerializeField]
    protected float _rayRadius = 0.0f;

    [Tooltip("Maximum damage done to the Head of an enemy with a single hit.")] [SerializeField]
    protected int _headDamage = 100;

    [Tooltip("Maximum damage done to the body of an enemy with a single hit.")] [SerializeField]
    protected int _bodyDamage = 20;

    [Tooltip("How damage is diluted over the range of the weapon.")] [SerializeField]
    protected AnimationCurve _damageAttenuation = new AnimationCurve();

    [Tooltip("Force applied by this weapon on a target.")] [SerializeField]
    protected float _force = 100.0f;

    [Tooltip("How force is diluted over the range of the weapon.")] [SerializeField]
    protected AnimationCurve _forceAttenuation = new AnimationCurve();

    [Header("FPS Arms Animation Properties")]
    [Tooltip("FPS Arms Animator Sub-State Machine index to use when performing animations for this weapon.")]
    [SerializeField]
    protected int _weaponAnim = -1;

    [Tooltip(
        "FPS Arms Animator Sub-State Machine attack animation index range.\n\n A value of 3 would be used if the sub-state has 3 attack variants.")]
    [SerializeField]
    protected int _attackAnimCount = 1;

    [Header("Camera Effects")]
    [Tooltip("Whether the camera shake happens at the point of fire or only when the weapon ray hits")]
    [SerializeField]
    protected InventoryItemWeaponShakeType _shakeType = InventoryItemWeaponShakeType.OnFire;

    [Tooltip("Duration of Shake Effect of this weapon")] [SerializeField]
    protected float _shakeDuration = 0.0f;

    [Tooltip("Magnitude of Shake Effect of this weapon")] [SerializeField]
    protected float _shakeMagnitude = 0.0f;

    [Tooltip("Damping of Shake Effect of this weapon")] [SerializeField]
    protected float _shakeDamping = 1.0f;

    [Tooltip("Dual Mode FOV")] [SerializeField]
    protected float _dualModeFOV = 45.0f;

    [Tooltip("Image used for weapon crosshair")] [SerializeField]
    protected Sprite _crosshair = null;

    // Public Properties
    public InventoryWeaponType weaponType {
        get { return _weaponType; }
    }

    public InventoryWeaponFeedType weaponFeedType {
        get { return _weaponFeedType; }
    }

    public InventoryItemAmmo ammo {
        get { return _ammo; }
    }

    public InventoryWeaponReloadType reloadType {
        get { return _reloadType; }
    }

    public float range {
        get { return _range; }
    }

    public float soundRadius {
        get { return _soundRadius; }
    }

    public float conditionDepletion {
        get { return _conditionDepletion; }
    }

    public bool autoFire {
        get { return _autoFire; }
    }

    public bool dualMode {
        get { return _dualMode; }
    }

    public int headDamage {
        get { return _headDamage; }
    }

    public int bodyDamage {
        get { return _bodyDamage; }
    }

    public float rayRadius {
        get { return _rayRadius; }
    }

    public float force {
        get { return _force; }
    }

    public int weaponAnim {
        get { return _weaponAnim; }
    }

    public int attackAnimCount {
        get { return _attackAnimCount; }
    }

    public float shakeDuration {
        get { return _shakeDuration; }
    }

    public float shakeMagnitude {
        get { return _shakeMagnitude; }
    }

    public float shakeDamping {
        get { return _shakeDamping; }
    }

    public float dualModeFOV {
        get { return _dualModeFOV; }
    }

    public Sprite crosshair {
        get { return _crosshair; }
    }

    public int ammoCapacity {
        // Returns the correct max capcity of the weapon based on ReloadType and Ammo.
        get {
            switch (_reloadType) {
                case InventoryWeaponReloadType.None: return 0;
                case InventoryWeaponReloadType.Partial: return _ammoCapacity;
                case InventoryWeaponReloadType.NonPartial:
                    if (_ammo == null) return -1;
                    return _ammo.capacity;
            }

            return -1;
        }
    }

    public InventoryItemWeaponShakeType shakeType {
        get { return _shakeType; }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   GetAttenuatedDamage
    // Desc :   Given a distance in meters and a body part string ("Head" or "Body") will return
    //          the damage that weapon does to that body part 
    // --------------------------------------------------------------------------------------------
    public int GetAttentuatedDamage(string bodyPart, float distance) {
        float normalizedDistance = Mathf.Clamp(distance / _range, 0.0f, 1.0f);
        if (bodyPart.Equals("Head"))
            return (int) (_damageAttenuation.Evaluate(normalizedDistance) * _headDamage);
        else
            return (int) (_damageAttenuation.Evaluate(normalizedDistance) * _bodyDamage);
    }

    // ---------------------------------------------------------------------------------------------
    // Name :   GetAttenuatedForce
    // Desc :   Given a distance to a target return the amount of force that will be recieved 
    //          from this weapon at this distance
    // ---------------------------------------------------------------------------------------------
    public float GetAttentuatedForce(float distance) {
        if (_force == 0.0f) return 0.0f;
        else
            return _forceAttenuation.Evaluate(Mathf.Clamp(distance / _range, 0.0f, 1.0f)) * _force;
    }
}