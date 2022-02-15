using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum FlashlightType {  Primary, Secondary }

// --------------------------------------------------------------------------------------------------
// CLASS    : Flashlight
// DESC     : Describes the pairing of a Unity Light and it's (optional) associated mesh
// --------------------------------------------------------------------------------------------------
[System.Serializable]
public class Flashlight
{
    [SerializeField] protected GameObject LightObject = null;
    [SerializeField] protected GameObject LightMesh   = null;

    // ----------------------------------------------------------------------------------------------
    // Name :   UpdateData
    // Desc :   Used to change the underlying light and mesh references at runtime
    // ----------------------------------------------------------------------------------------------
    public void UpdateData ( GameObject lightObject, GameObject lightMesh)
    {
        LightObject = lightObject;
        LightMesh   = lightMesh;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   ActivateMesh
    // Desc :   Enables/Disables the underlying game object containing the mesh
    // --------------------------------------------------------------------------------------------
    public void ActivateMesh ( bool enableMesh )
    {
        if (LightMesh)
            LightMesh.SetActive(enableMesh);

    }

    // --------------------------------------------------------------------------------------------
    // Name :   ActivateLight
    // Desc :   Enables/Disables the gameobject containing the light component
    // --------------------------------------------------------------------------------------------
    public void ActivateLight(bool enableLight)
    {
        if (LightObject)
            LightObject.SetActive(enableLight);
    }


}

[System.Serializable]
public class ArmsObject
{
    public ScriptableObject         Identifier                  = null;
    public List <GameObject>        SceneObjects                = new List<GameObject>();
    public Flashlight               Light                       = new Flashlight();
    public AnimatorStateCallback    Callback                    = null;
    public Transform                CrosshairPosition           = null;
    public Transform                CrosshairPosition_DualMode  = null;
}


public class CharacterManager : MonoBehaviour
{
    // Inspector Assigned
    [SerializeField] private CapsuleCollider        _meleeTrigger       = null;
    [SerializeField] private CameraBloodEffect      _cameraBloodEffect  = null;
    [SerializeField] private Camera                 _sceneCamera        = null;
    [SerializeField] private AISoundEmitter         _soundEmitter       = null;
    [SerializeField] private float                  _walkRadius         = 0.0f;
    [SerializeField] private float                  _runRadius          = 7.0f;
    [SerializeField] private float                  _landingRadius      = 12.0f;
    [SerializeField] private float                  _bloodRadiusScale   = 6.0f;
    [SerializeField] private PlayerHUD              _playerHUD          = null;

    // Pain Damage Audio
    [SerializeField] private AudioCollection _damageSounds  = null;
    [SerializeField] private AudioCollection _painSounds    = null;
    [SerializeField] private AudioCollection _tauntSounds   = null;

    [SerializeField] private float          _nextPainSoundTime  = 0.0f;
    [SerializeField] private float          _painSoundOffset    = 0.35f;
    [SerializeField] private float          _tauntRadius        = 10.0f;

    [Header("Inventory")]
    [SerializeField] private GameObject             _inventoryUI        = null;
    [SerializeField] private Inventory              _inventory          = null;
    [SerializeField] private Flashlight             _primaryFlashlight  = new Flashlight();
    [SerializeField] private bool                   _flashlightOnStart  = true;
    [SerializeField] private InventoryItemWeapon    _defaultWeapon      = null;
    
    [Header("Arms System")]
    [SerializeField] private Animator           _armsAnimator           = null;
    [SerializeField] private List<ArmsObject>   _armsObjects            = new List<ArmsObject>();
    [SerializeField] private LayerMask          _weaponRayLayerMask     = new LayerMask();


    [Header("Shared Variables")]
    [SerializeField] private SharedFloat    _health             = null;
    [SerializeField] private SharedFloat    _infection          = null;
    [SerializeField] private SharedString   _interactionText    = null;
    [SerializeField] private SharedFloat    _stamina            = null;
    [SerializeField] private VectorShaker   _cameraShaker       = null;
    [SerializeField] private SharedVector3  _crosshairPosition  = null;
    [SerializeField] private SharedFloat    _crosshairAlpha     = null;
    [SerializeField] private SharedSprite   _crosshairSprite    = null;

    
    // Private
    private Collider                _collider               = null;
    private FPSController           _fpsController          = null;
    private CharacterController     _characterController    = null;
    private GameSceneManager        _gameSceneManager       = null;
    private int                     _aiBodyPartLayer        = -1;
    private int                     _interactiveMask        = 0;
    private float                   _nextTauntTime          = 0;
   

    // Weapon & Arms
    private InventoryItemWeapon         _currentWeapon          = null;
    private InventoryItemWeapon         _nextWeapon             = null;
    private InventoryWeaponMountInfo    _nextWeaponMountInfo    = null;
    private bool                        _canSwitchWeapons       = false;
    private int                         _availableAmmo          = 0;
    private IEnumerator                 _switchWeaponCoroutine  = null;
    private float                       _initialFOV = 60.0f;
    private Flashlight                  _secondaryFlashLight    = null;

    // Dictionary for fast access at runtime
    // Store each ArmObject key'd by Scriptable Object ID
    private Dictionary<ScriptableObject, ArmsObject> _armsObjectsDictionary = new Dictionary<ScriptableObject, ArmsObject>();

    // Animation Hashes
    private int _weaponAnimHash             = Animator.StringToHash("Weapon Anim");         // The current sub-state machine to play for the selected weapon
    private int _weaponArmedHash            = Animator.StringToHash("Weapon Armed");        // Is the current weapon armed
    private int _flashlightHash             = Animator.StringToHash("Flashlight");          // Is flashlight on
    private int _speedHash                  = Animator.StringToHash("Speed");               // Speed setting of character (Idle, Walking or running)
    private int _attackAnimHash             = Animator.StringToHash("Attack Anim");         // Used by machines that have several random attack states
    private int _attackTriggerHash          = Animator.StringToHash("Attack");              // Used to trigger a transition into an attack state
    private int _canSwitchWeaponsHash       = Animator.StringToHash("Can Switch Weapons");  // Can we switch to a different weapon at the moment
    private int _dualHandedWeaponHash       = Animator.StringToHash("Dual Handed Weapon");  // Is the current weapon two handed
    private int _dualModeActive             = Animator.StringToHash("Dual Mode Active");    // Does the current weapon have a dual firing mode that is active
    private int _reloadHash                 = Animator.StringToHash("Reload");              // Do we require a reload
    private int _reloadRepeatHash           = Animator.StringToHash("Reload Repeat");       // How many times should the reload animation loop (used for partial reload types)
    private int _staminaHash                = Animator.StringToHash("Stamina");             // Stamina of the player
    private int _autoFireHash               = Animator.StringToHash("Auto Fire");           // Does the weapon support auto fire
    private int _playerSpeedOverrideHash    = Animator.StringToHash("Player Speed Override"); // Allows animation to override max speed of player
    private int _clearWeaponHash            = Animator.StringToHash("Clear Weapon");          // Hash of Clear Weapon Trigger in animator
    private int _dualModeFOVHash            = Animator.StringToHash("Dual Mode FOV Weight");  // Animation Curve Driven. Used to weight FOV transitions
    private int _crosshairAlphaHash         = Animator.StringToHash("Crosshair Alpha");       // Transparency of the crosshair

    public FPSController fpsController { get { return _fpsController; } }
    public InventoryItemWeapon currentWeapon { get { return _currentWeapon; } }
    

	// Use this for initialization
	void Start () 
	{
		_collider 			= GetComponent<Collider>();
		_fpsController 		= GetComponent<FPSController>();
		_characterController= GetComponent<CharacterController>();
		_gameSceneManager 	= GameSceneManager.instance;
		_aiBodyPartLayer 	= LayerMask.NameToLayer("AI Body Part");
		_interactiveMask	= 1 << LayerMask.NameToLayer("Interactive");

        if (_sceneCamera)
            _initialFOV = _sceneCamera.fieldOfView;

		if (_gameSceneManager!=null)
		{
			PlayerInfo info 		= new PlayerInfo();
			info.camera 			= _sceneCamera;
			info.characterManager 	= this;
			info.collider			= _collider;
			info.meleeTrigger		= _meleeTrigger;

			_gameSceneManager.RegisterPlayerInfo( _collider.GetInstanceID(), info );
		}

		// Get rid of really annoying mouse cursor
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;

		// Start fading in
		if (_playerHUD) _playerHUD.Fade( 2.0f, ScreenFadeType.FadeIn );

        // Start off with the primary flashlight and mesh disabled
        _primaryFlashlight.ActivateLight(false);
        _primaryFlashlight.ActivateMesh(false);

        // Set the starting state of the flashlight
        ActivateFlashlight(_flashlightOnStart);

        // An array of all SMB on the arms animator
        ArmsBaseSMB[] stateMachineBehaviours = _armsAnimator.GetBehaviours<ArmsBaseSMB>();
         
        // A Dictionary of SMBs all sorted into lists based on Identifier
        Dictionary<ScriptableObject, List<ArmsBaseSMB>> stateMachineBehavioursByID = new Dictionary<ScriptableObject, List<ArmsBaseSMB>>();

        // Sort all SMBs into lists by ID
        foreach (ArmsBaseSMB behaviour in stateMachineBehaviours)
        {
            // Store the Character Manager reference in the SMB
            behaviour.CharacterManager = this;

            if (behaviour.Identifier)
            {
                List<ArmsBaseSMB> behaviourList = null;
                if (stateMachineBehavioursByID.TryGetValue(behaviour.Identifier, out behaviourList))
                {
                    behaviourList.Add(behaviour);
                }
                else
                {
                    List<ArmsBaseSMB> newBehaviourList = new List<ArmsBaseSMB>();
                    newBehaviourList.Add(behaviour);
                    stateMachineBehavioursByID[behaviour.Identifier] = newBehaviourList;
                }
            }
        }

        // Copy over the FPS weapon prefabs in the scene (attached to our arms) stored in a list
        // into a dictionary for quick runtime access
        for (int i = 0; i < _armsObjects.Count; i++)
        {
            ArmsObject armsObject = _armsObjects[i];
            if (armsObject!=null && armsObject.Identifier)
            {
                // Store the GameObject list in the Dictionary by ID
                _armsObjectsDictionary[armsObject.Identifier] = armsObject;

                // Now we need to see if this weapon has an AnimatorStateCallback so that the animator
                // can call its functions.
                if (armsObject.Callback != null)
                {
                    // We now know the weapon in the scene has a callback script so that it can be triggered by
                    // the animator so we should have one or more ArmsBaseSMBs (stored above) that wish
                    // to obtain a reference to this callback script;
                    List<ArmsBaseSMB> behaviourList = null;
                    if ( stateMachineBehavioursByID.TryGetValue(armsObject.Identifier, out behaviourList))
                    {
                        foreach ( ArmsBaseSMB behaviour in behaviourList)
                        {
                            if (behaviour != null) behaviour.CallbackHandler = armsObject.Callback;
                        }
                    }
                }
            }
        }

      

    }

    private void OnEnable()
    {
        // Register Inventory Listeners
        if (_inventory)
        {
            _inventory.OnWeaponChange.AddListener(OnSwitchWeapon);
            _inventory.OnWeaponDropped.AddListener(OnDropWeapon);
        }
    }

    private void OnDisable()
    {
        // Unregister Inventory Listeners
        if (_inventory)
        {
            _inventory.OnWeaponChange.RemoveListener(OnSwitchWeapon);
            _inventory.OnWeaponDropped.RemoveListener(OnDropWeapon);
        }
    }

    // -------------------------------------------------------------------------------
    // Name	:	DisableWeapon_AnimatorCallback
    // Desc	:	This is called by the Animation Controller via a state behavior so that
    //			weapons are activated/deactivated in sync with the animations.
    // -------------------------------------------------------------------------------
    public void DisableWeapon_AnimatorCallback()
    {

        // If we have a weapon to deactivate
        if (_currentWeapon != null)
        {

            // Get its corresponding scene representation and switch it off
            ArmsObject armsObject = null ;
            if (_armsObjectsDictionary.TryGetValue(_currentWeapon, out armsObject))
            {
                foreach (GameObject subObject in armsObject.SceneObjects)
                {
                    if (subObject) subObject.SetActive(false);
                }
            }
        }

        // If we have a new weapon queued up that isn't the current weapon and if
        // we have WeaponMountInfo for that gun, it means we are replacing our weapon
        // with one at the same mount that has been invoked by the inventory system.
        // This means we are picking up a new weapon and wish to drop the current one.
        // When doing a regular weapon switch (between two weapons we own but on two
        // different mounts) _nextWeaponMountInfo will be null meaning the weapon will
        // be disabled on the player rig but not removed from the inventory and no
        // scene proxy instantiated.
        if (_nextWeapon != null && /*_nextWeapon!=_currentWeapon &&*/  _nextWeaponMountInfo!=null)
        {
            if (_inventory)
                _inventory.DropWeaponItem(_nextWeaponMountInfo.Weapon.weaponType == InventoryWeaponType.TwoHanded ? 1 :0);
        }
       

        // We currently have no weapon
        _currentWeapon = null;

        // Clear specialized crosshair
        if (_crosshairSprite)
            _crosshairSprite.value = null;

        // Clear secondary Flashlight
        _secondaryFlashLight = null;

            
    }

    // -------------------------------------------------------------------------------
    // Name	:	EnableWeapon_AnimatorCallback
    // Desc	:	This is called by the Animation Controller via a state behavior so that
    //			weapons are activated/deactived in sync with the animations
    // -------------------------------------------------------------------------------
    public void EnableWeapon_AnimatorCallback()
    {
        if (_nextWeapon!=null)
        {
            // If we are not simply toggling on and off the same weapon and if
            // _nextWeaponMountInfo is not null....this has been instantiated from
            // the inventory system due to a pick up. So we need to remove the proxy
            // from the scene and assign it to the inventory's mount
            if (_nextWeapon != _currentWeapon && _nextWeaponMountInfo!=null && _inventory)
            {
               int mountIndex = _nextWeapon.weaponType == InventoryWeaponType.SingleHanded ? 0 : 1;
               _inventory.AssignWeapon( mountIndex, _nextWeaponMountInfo); 
            }

            // In all cases, our new weapon choice needs to be enabled in the FPS rig.
            // So get the list of GameObjects from the scene that need to be enabled
            ArmsObject armsObject = null;
            if (_armsObjectsDictionary.TryGetValue(_nextWeapon, out armsObject))
            {
                // Enable all game objects that comprise this weapon
                foreach(GameObject subObject in armsObject.SceneObjects)
                {
                    if (subObject) subObject.SetActive(true);
                }

                // Store a reference to the weapon light (if exists)
                _secondaryFlashLight = armsObject.Light;
            }

            // This is our new current weapon
            _currentWeapon = _nextWeapon;

            // Assign weapon specific crosshair sprite
            if (_crosshairSprite)
                _crosshairSprite.value = _currentWeapon.crosshair;        

            // Get an update of the available ammo of the weapon we have just changed to.
            // Notice we pass 'NoWeaponAmmo' so rounds in gun are not included. This is because the character manager
            // uses this variable to determine if the reload can be activated. =0..no bullets left to reload
            // but may still be some in gun.
            if (_inventory)
                _availableAmmo = _inventory.GetAvailableAmmo(_currentWeapon.ammo, AmmoAmountRequestType.NoWeaponAmmo );

           
        }

        if (_armsAnimator)
            _armsAnimator.SetBool("Switching Weapons", false);
        
        // Weapon has been switched so there is no weapon waiting any longer
        _nextWeaponMountInfo = null;
        _nextWeapon = null;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   ReloadWeapon_AnimatorCallback
    // Desc :   Called by the Animator to complete the reload process. This function ACTUALLY
    //          performs the inventory reload - usually at the end of the reload animation.
    // ----------------------------------------------------------------------------------------------
    public void ReloadWeapon_AnimatorCallback( InventoryWeaponType weaponType)
    {
        if (!_inventory || !_currentWeapon || weaponType == InventoryWeaponType.None) return;

        // Tell the inventory to intelligently reload the specified mount but do NOT play
        // the weapons build in USE sound effect for the reload. We only use that in the inventory UI.
        // The sound will be directed from the animator and synced with animation.
        _inventory.ReloadWeapon(weaponType == InventoryWeaponType.SingleHanded ? 0 : 1, false);

        _availableAmmo = _inventory.GetAvailableAmmo(_currentWeapon.ammo, AmmoAmountRequestType.NoWeaponAmmo);
    }


    // --------------------------------------------------------------------------------------------
    // Name :   OnDropWeapon (Event Listener for Inventory)
    // Desc	:	This function exists only for use by the inventory system.
    //			When we choose to DROP a weapon via the inventory, the game is
    //          in a frozen state. We don't want the animations playing
    //			when we come out of the inventory as the weapon will have
    //			already been dropped by then and added to the scene.
    //			The inventory handles the droping of the item and the instaniating 
    //          of its proxy in the scene.
    //			We also want the weapon to disappear from the hands too
    //			and those hands be immediately updated to a disarmed state in the
    //          Animator.
    // ---------------------------------------------------------------------------------------------
    private void OnDropWeapon(InventoryItemWeapon weapon)
    {
        // We only want to process this event when the UI is active. This is out way of responding to
        // a DropWeapon event within the UI so that our Arms and Weapons hierarchy stays synced.
        if ( (_inventoryUI && !_inventoryUI.activeSelf) || !_inventoryUI) return;

        // Is the weapon we are dropping the current weapon we are using
        // because if so we need to remove if from our arms
        if (currentWeapon == weapon && currentWeapon!=null)
        {
            // We have processed this mouse action so clear it
            Input.ResetInputAxes();

            // Force the animator to an immediate disarmed state
            if (_armsAnimator)
            {
                _armsAnimator.SetTrigger    (_clearWeaponHash);
                _armsAnimator.SetBool       (_weaponArmedHash, false);
                _armsAnimator.SetInteger    (_weaponAnimHash, 0);
            }

            // Deactivate the corresponding arms object
            ArmsObject armsObject = null;
            if (_armsObjectsDictionary.TryGetValue( _currentWeapon, out armsObject))
            {
                foreach(GameObject item in armsObject.SceneObjects)
                {
                    if (item != null) item.SetActive(false);
                }
            }

            _currentWeapon = null;
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name : SwitchMount 
    // Desc : Called to switch between mounts
    // ---------------------------------------------------------------------------------------------
    public void SwitchMount( InventoryItemWeapon nextWeapon)
    {
        if (_canSwitchWeapons && _switchWeaponCoroutine == null)
        {
            _switchWeaponCoroutine = SwitchWeaponInternal(nextWeapon, null);
            StartCoroutine(_switchWeaponCoroutine);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Name : Switch Weapon (Event Listener for Inventory)
    // Desc : Called by inventory system to switch weapons
    // ---------------------------------------------------------------------------------------------
    private void OnSwitchWeapon(InventoryWeaponMountInfo wmi)
    {
        if (_canSwitchWeapons && wmi != null && wmi.Weapon && _switchWeaponCoroutine == null)
        {
            _switchWeaponCoroutine = SwitchWeaponInternal(wmi.Weapon, wmi);
            StartCoroutine(_switchWeaponCoroutine);
        }
    }



    // ---------------------------------------------------------------------------------------------
    // Name :   SwitchWeaponInternal
    // Desc :   This is the function that invokes the Arms Animation to switch to a new weapon
    //          or to switch between mount.
    // ----------------------------------------------------------------------------------------------
    private IEnumerator SwitchWeaponInternal(InventoryItemWeapon nextWeapon, InventoryWeaponMountInfo wmi)
    {
        // We need an animator to switch weapons
        if (!_armsAnimator)
        {
            _switchWeaponCoroutine = null;
            yield break;
        }

        // We are about to change weapon (or disarm) so make sure we cancel any pended reload
        // request in the animator that may have been intended for this new weapon.
        _armsAnimator.SetBool(_reloadHash, false);
        
        // Disarm the current weapon if there is one. This will force the animator to play
        // the dismount animation for the current active weapon (if any)
        _armsAnimator.SetBool(_weaponArmedHash, false);
        
        // This is the weapon we wish to transition to next
        _nextWeapon = nextWeapon;

        // This is Pickup information. If NULL, then the weapon is assumed to already be
        // mounted and will not be added to the inventory.
        _nextWeaponMountInfo = wmi;
        
        // Let the animator know whether or ot we are transitioning to a single handed or
        // dual handed weapon. In the case of a single handed weapon, this will allow
        // the flashlight layer to bring up the flashlight (if active) in the left hand.
        if (nextWeapon)
        {
            if (nextWeapon.weaponType == InventoryWeaponType.TwoHanded)
                _armsAnimator.SetBool(_dualHandedWeaponHash, true);
            else
                _armsAnimator.SetBool(_dualHandedWeaponHash, false);

            _armsAnimator.SetBool("Switching Weapons", true);

            // Force a wait state so the animator can pick up on a switch between
            // two weapons of the same type. 
            yield return new WaitForSecondsRealtime(0.2f);

            _armsAnimator.SetBool(_weaponArmedHash, true);
            _armsAnimator.SetInteger(_weaponAnimHash, nextWeapon.weaponAnim);
        }
        
        
        // Record that coroutine is no longer running
        _switchWeaponCoroutine = null;
    }

    public void TakeDamage ( float amount, bool doDamage, bool doPain )
	{
		_health.value = Mathf.Max ( _health.value - (amount *Time.deltaTime)  , 0.0f);

		if (_fpsController)
		{
			_fpsController.dragMultiplier = 0.0f; 

		}
		if (_cameraBloodEffect!=null)
		{
			_cameraBloodEffect.minBloodAmount = (1.0f - _health.value/100.0f) * 0.5f;
			_cameraBloodEffect.bloodAmount = Mathf.Min(_cameraBloodEffect.minBloodAmount + 0.3f, 1.0f);	
		}

		// Do Pain / Damage Sounds
		if (AudioManager.instance)
		{
			if (doDamage && _damageSounds!=null)
				AudioManager.instance.PlayOneShotSound( _damageSounds.audioGroup,
														_damageSounds.audioClip, transform.position,
														_damageSounds.volume,
														_damageSounds.spatialBlend,
														_damageSounds.priority );

			if (doPain && _painSounds!=null && _nextPainSoundTime<Time.time)
			{
				AudioClip painClip = _painSounds.audioClip;
				if (painClip)
				{
					_nextPainSoundTime = Time.time + painClip.length;
					StartCoroutine(AudioManager.instance.PlayOneShotSoundDelayed(	_painSounds.audioGroup,
																			 	 	painClip,
																			  		transform.position,
																			  		_painSounds.volume,
																			  		_painSounds.spatialBlend,
																			  		_painSoundOffset,
																			  		_painSounds.priority ));
				}
			}
		}

		if (_health.value<=0.0f) 
		{
			DoDeath();
		}
	}

    // --------------------------------------------------------------------------------------------
    // Name :   DoDamage
    // Desc :   This is the function that is called by an AnimationClip event when the player fires
    //          the weapon. This function decrements the ammo, enabled any muzzle flash the weapon
    //          supports and of course, finally casts the ray to find the object we have hit and
    //          apply the damage to it.
    // --------------------------------------------------------------------------------------------
	public void DoDamage( int hitDirection = 0 )
	{
       
        // Which mount are we firing from IF ANY?
        if (_inventory && 
            _currentWeapon && 
            _currentWeapon.weaponFeedType == InventoryWeaponFeedType.Ammunition && 
            _inventory.GetAvailableAmmo(_currentWeapon.ammo, AmmoAmountRequestType.WeaponAmmoOnly)>0)
        {
            // Decrease ammo in the weapon
            _inventory.DecreaseAmmoInWeapon(currentWeapon.weaponType == InventoryWeaponType.TwoHanded ? 1 : 0);

            // Get new available ammo and cancel autoFire if we have nothing left in the gun
            if (_inventory.GetAvailableAmmo( _currentWeapon.ammo, AmmoAmountRequestType.WeaponAmmoOnly)<1)
            {
                if (_armsAnimator)
                    _armsAnimator.SetBool( _autoFireHash, false);
            }

            // Get the ArmsObject that represents the weapon
            ArmsObject armsObject = _armsObjectsDictionary[currentWeapon];
            if (armsObject != null)
            {
                // Instruct its callback to handle muzzle flash
                WeaponAnimatorStateCallback weaponArmsCallback = armsObject.Callback as WeaponAnimatorStateCallback;
                if (weaponArmsCallback)
                    weaponArmsCallback.DoMuzzleFlash();
            }
        }

        // Setup the rest applied to rhe default weapon as well
        InventoryItemWeapon weapon = _currentWeapon == null ? _defaultWeapon : _currentWeapon;

        // If we have a weapon
        if (weapon)
        {
            // Emit a sound according the weapon propeties
            _soundEmitter.SetRadius( weapon.soundRadius );

            // Get the weapon mount
            InventoryWeaponMountInfo wmi = null;

            // Now we need to decrease the condition of the weapon on use
            if ( weapon!=_defaultWeapon && _inventory && weapon.weaponType!=InventoryWeaponType.None )
            {
                wmi = _inventory.GetWeapon(  weapon.weaponType == InventoryWeaponType.SingleHanded ? 0 : 1  );

                // Decrease its condition
                if (wmi!=null)
                        wmi.Condition = Mathf.Max(0, wmi.Condition - weapon.conditionDepletion);
                
            }

            // Shake the camera using the weapon's shake settings
            if (_cameraShaker && weapon.shakeType == InventoryItemWeaponShakeType.OnFire)
                _cameraShaker.ShakeVector(weapon.shakeDuration, weapon.shakeMagnitude, weapon.shakeDamping);

            if (_sceneCamera == null) return;
            if (_gameSceneManager == null) return;

            // Local Variables
            Ray ray;
            RaycastHit hit = new RaycastHit();
            RaycastHit[] hits;
            bool isSomethingHit = false;

            // Create a ray through _crosshair to see if we hit anything      
            ray = _sceneCamera.ScreenPointToRay(_crosshairPosition.value);

            // If this is a blast radius weapon things are a bit more complication
            if (weapon.rayRadius > 0.0f)
            {
                // First, we are going to sweep a sphere so we have to move the origin back a bit to account for sphere radius at start of capsule
                // because things there were inside the initial sphere to begin with didn't register
                ray.origin = ray.origin - transform.forward * weapon.rayRadius;
                
                // Perform a where cast for the weapon range with the specified radius of thr weapon and get back ALL that was hit
                hits = Physics.SphereCastAll(ray, weapon.rayRadius,  weapon.range, _weaponRayLayerMask.value, QueryTriggerInteraction.Ignore);

                // Now we need to iterate through all the hits because we might have hit geometry as well as body parts. 
                foreach ( RaycastHit potentialHit in hits)
                {               
                    // Ignore any hit that isn't an AI body part
                    if ( potentialHit.transform.gameObject.layer == LayerMask.NameToLayer("AI Body Part"))
                    {
                        // Now we need to see that we have line of sight with that body part as the sphere cast would return true for
                        // a body part even if a wall is inbetween the player. We make a copy of the first ray but adjust its direction to
                        // point at the potential hit point
                        RaycastHit sightTestHit;
                        Ray siteTestRay = ray;
                        siteTestRay.direction = potentialHit.point - siteTestRay.origin;
                        
                        // Now see if we get back the same object from a raycast which returns the closest object. If we do there is clear line of sight, otherwise, there is
                        // something else blocking line of sight to that hit and we should ignore it
                        if (Physics.Raycast(siteTestRay, out sightTestHit, 1000, _weaponRayLayerMask.value, QueryTriggerInteraction.Ignore))
                        {          
                            // Line of sight is good so lets use this at the hit point on the NPC
                            if (potentialHit.transform == sightTestHit.transform)
                            {                             
                                hit = sightTestHit;
                                isSomethingHit = true;
                                break;
                            }
                        }
                    }
                }
                

            }
            // With a non-radius weapon things are much simpler...just do a raytest for the closest object
            else
                isSomethingHit = Physics.Raycast(ray, out hit, weapon.range, _weaponRayLayerMask.value, QueryTriggerInteraction.Ignore);

            // Did we hit something
            if (isSomethingHit && hit.rigidbody)
            {
                
                // Is it a zombie
                AIStateMachine stateMachine = _gameSceneManager.GetAIStateMachine(hit.rigidbody.GetInstanceID());
                if (stateMachine)
                {
                    // Get the weapon range based damage and then scale by condition
                    float damage = weapon.GetAttentuatedDamage(hit.rigidbody.tag, hit.distance) * (wmi == null ? 1 : wmi.Condition / 100.0f);

                    // DO damge to the zombie based on weapon specs
                    stateMachine.TakeDamage(hit.point,
                                                ray.direction * weapon.GetAttentuatedForce(hit.distance),
                                                (int) damage,
                                                hit.rigidbody,
                                                this,
                                                hitDirection);

                    // Shake the camera using the weapon's shake settings
                    if (_cameraShaker && weapon.shakeType == InventoryItemWeaponShakeType.OnHit)
                        _cameraShaker.ShakeVector(weapon.shakeDuration, weapon.shakeMagnitude, weapon.shakeDamping);

                }
            }
        }

	}

    // --------------------------------------------------------------------------------------------
    // Name :   Update
    // Desc :   Called each frame by Unity 
    //          Processes Input and Interactions with scene objects
    // --------------------------------------------------------------------------------------------
	void Update()
	{      
        // Process Inventory Key Toggle
        if (Input.GetButtonDown("Inventory") && _inventoryUI)
        {
            // If its not visible...make it visible
            if (!_inventoryUI.activeSelf)
            {
                _inventoryUI.SetActive(true);
                if (_playerHUD) _playerHUD.gameObject.SetActive(false);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                return;
            }
            else
            {
                _inventoryUI.SetActive(false);
                if (_playerHUD) _playerHUD.gameObject.SetActive(true);
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        // Grab current state of Animator and set the Animators Stamina
        if (_armsAnimator)
        {
            // Get whether we can switch weapons or not
            _canSwitchWeapons = _armsAnimator.GetFloat(_canSwitchWeaponsHash) > 0.75f ? true : false;

            // Set the FPS Controllers speed override from Animator
            _fpsController.speedOverride = _armsAnimator.GetFloat(_playerSpeedOverrideHash);

            // Get current alpha of crosshair from animator and write it to shared float
            if (_crosshairAlpha)
                _crosshairAlpha.value = _armsAnimator.GetFloat(_crosshairAlphaHash);

            // If this is a dual mode weapon then
            float zoomFOVWeight = _armsAnimator.GetFloat(_dualModeFOVHash);
            if (_sceneCamera && !zoomFOVWeight.Equals(0.0f) && _currentWeapon && _currentWeapon.dualMode)
            {
                _sceneCamera.fieldOfView = Mathf.Lerp(_initialFOV, _currentWeapon.dualModeFOV, zoomFOVWeight);
            }
            else
                _sceneCamera.fieldOfView = _initialFOV;

            // Send player's stamina into the animator in the 0 - 1 range BUT we clamp the Animator
            // Stamina to a minimum of 0.5. Slowing an animation more than this looks wrong so
            // it's just to give a better feeling of stamina depletion.
            float normalizedStamina = 1.0f;
            if (_stamina)
                normalizedStamina = (_stamina.value / 100.0f) *  _fpsController.dragMultiplier;
           
            _armsAnimator.SetFloat( _staminaHash, Mathf.Min(normalizedStamina+0.1f, 1.0f) );
        }

        Ray ray;
		RaycastHit hit;
		RaycastHit [] hits;
		
		// PROCESS INTERACTIVE OBJECTS
		// Is the crosshair over a usuable item or descriptive item...first get ray from centre of screen
		ray = _sceneCamera.ScreenPointToRay( new Vector3(Screen.width/2, Screen.height/2, 0));

		// Calculate Ray Length
		float rayLength =  Mathf.Lerp( 1.0f, 1.8f, Mathf.Abs(Vector3.Dot( _sceneCamera.transform.forward, Vector3.up )));

		// Cast Ray and collect ALL hits
		hits = Physics.RaycastAll (ray, rayLength, _interactiveMask );

		// Process the hits for the one with the highest priorty
		if (hits.Length>0)
		{
			// Used to record the index of the highest priorty
			int 				highestPriority = int.MinValue;
			InteractiveItem		priorityObject	= null;	

			// Iterate through each hit
			for (int i=0; i<hits.Length; i++)
			{
				// Process next hit
				hit = hits[i];
               
				// Fetch its InteractiveItem script from the database
				InteractiveItem interactiveObject = _gameSceneManager.GetInteractiveItem( hit.collider.GetInstanceID());

				// If this is the highest priority object so far then remember it
				if (interactiveObject!=null && interactiveObject.priority>highestPriority)
				{
					priorityObject = interactiveObject;
					highestPriority= priorityObject.priority;
				}
			}

			// If we found an object then display its text and process any possible activation
			if (priorityObject!=null)
			{
                if (_interactionText)
                    _interactionText.value = priorityObject.GetText();

                // If its a weapon we can only pick it up (activate it) if the animator is in a state where
                // it will allow a weapons switch.
                if (!(priorityObject.GetType() == typeof(CollectableWeapon) && !_canSwitchWeapons))
                {
                    if (Input.GetButtonDown("Use"))
                    {
                        priorityObject.Activate(this);

                        if (_currentWeapon && _inventory)
                            _availableAmmo = _inventory.GetAvailableAmmo(_currentWeapon.ammo, AmmoAmountRequestType.NoWeaponAmmo);
                    }
                }
			}
		}
		else
		{
            if (_interactionText)
                _interactionText.value = null;
        }

		
        // Do we have an FPS Controller if so configure speed in Animator and set sound emitter radius based on speed
        if (_fpsController)
        {
            // Set animator to current speed
            if (_armsAnimator)
            {
                switch (_fpsController.movementStatus)
                {
                    case PlayerMoveStatus.Running:
                    case PlayerMoveStatus.Landing:
                        _armsAnimator.SetInteger(_speedHash, 2);
                        break;
                    case PlayerMoveStatus.Walking:
                        _armsAnimator.SetInteger(_speedHash, 1);
                        break;
                    default:
                    _armsAnimator.SetInteger(_speedHash, 0);
                    break;
                }
            }

            // Calculate sound emitter radius
            if (_soundEmitter != null && Time.frameCount>50)
            {
                float newRadius = Mathf.Max(_walkRadius, (100.0f - _health.value) / _bloodRadiusScale);
                switch (_fpsController.movementStatus)
                {
                    case PlayerMoveStatus.Landing: newRadius = Mathf.Max(newRadius, _landingRadius); break;
                    case PlayerMoveStatus.Running: newRadius = Mathf.Max(newRadius, _runRadius); break;
                }

                _soundEmitter.SetRadius(newRadius);

                _fpsController.dragMultiplierLimit = Mathf.Max(_health.value / 100.0f, 0.25f);
            }
        }

        // ---------- Process Game Input only when inventory is not active ----------------
        if  ( (_inventoryUI && !_inventoryUI.activeSelf) || !_inventoryUI)
        {
            // Set position of crosshair in 2d space
            if (_crosshairPosition && _sceneCamera)
            {
                // Default is crosshair in center of screen (0.5,0.5 in viewport space)
                _crosshairPosition.value = _sceneCamera.ViewportToScreenPoint( new Vector3(0.5f,0.5f,0));
                
                // If we have a weapon armed then map crosshair position to weapon crosshair reference position
                if (currentWeapon)
                {
                    ArmsObject armsObject = _armsObjectsDictionary[currentWeapon];
                    if (armsObject != null)
                    {
                        if ( currentWeapon.dualMode)
                        {
                            if (armsObject.CrosshairPosition_DualMode && armsObject.CrosshairPosition)
                                _crosshairPosition.value = Vector3.Lerp(
                                                                            _sceneCamera.WorldToScreenPoint(armsObject.CrosshairPosition.position),
                                                                            _sceneCamera.WorldToScreenPoint(armsObject.CrosshairPosition_DualMode.position),
                                                                            _armsAnimator.GetFloat(_dualModeFOVHash)
                                                                        );

                        }   
                        else
                        {
                            if (armsObject.CrosshairPosition)
                                _crosshairPosition.value = _sceneCamera.WorldToScreenPoint(armsObject.CrosshairPosition.position);
                        }
                    }
                }
            }

            if (_armsAnimator)
            {
                // FLASHLIGHT TOGGLE
                // Toggle the activate state of the flashlight
                if (Input.GetButtonDown("Flashlight"))
                {
                    ActivateFlashlight(!_armsAnimator.GetBool(_flashlightHash));
                }

                int mountIndex = -1;

                // Will the animator allow us to switch weapons at this time
                if (_canSwitchWeapons)
                {
                    if (Input.GetButtonDown("Single Handed Mount")) mountIndex = 0;
                    else
                    if (Input.GetButtonDown("Dual Handed Mount")) mountIndex = 1;
                }

                // Do we wish to activate a different mount or change the armed status
                // of the currently active mount;
                if (mountIndex != -1)
                {

                    // Get the weapon currently at the single handed mount
                    InventoryWeaponMountInfo weaponMountInfo = null;
                    if (_inventory)
                        weaponMountInfo = _inventory.GetWeapon(mountIndex);

                   
                    // Only process this keypress if we have something at the mount 
                    if (weaponMountInfo != null && weaponMountInfo.Weapon != null)
                    {
                        // If the weapon at this mounnt is our current weapon then we
                        // simply want to toggle its armed status
                        if (_currentWeapon == weaponMountInfo.Weapon)
                        {
                 
                            // Get current armed status from the animator, flip-it and 
                            // re-set back in the animator
                            bool weaponArmed = _armsAnimator.GetBool(_weaponArmedHash);
                            weaponArmed = !weaponArmed;

                            // If we are arming then set this weapon as the next weapon
                            // otherwise set next weapon to null.
                            if (weaponArmed)
                                _nextWeapon = weaponMountInfo.Weapon;
                            else
                                _nextWeapon = null;

                            // Instruct Animator to Arm/Disarm weapon
                            _armsAnimator.SetBool(_weaponArmedHash, weaponArmed);
                        }
                        // Otherwise we are switching from one mount type
                        // to another (eq: single handed to dual handed)
                        else
                        {
                            SwitchMount(weaponMountInfo.Weapon);
                        }
                    }
                }


                // Process Fire Buttons being pressed or depressed
                if (Input.GetButtonDown("Fire1"))
                {
                    // Set defaults in case we have no weapon
                    int attackAnim          = 1;
                    bool autoFireEnabled    = false;
                    bool canFire            = true;

                    // If we have a default weapon intially set its attack anim range
                    if (_defaultWeapon)
                        attackAnim = Random.Range(1, _defaultWeapon.attackAnimCount + 1);

                    // Override default weapon if we have a REAL weapon armed? 
                    if (_currentWeapon)
                    {
                        // What mount are we trying to fire from
                        mountIndex = _currentWeapon.weaponType == InventoryWeaponType.TwoHanded ? 1 : 0;

                        // Get the mount info
                        InventoryWeaponMountInfo currWMI = _inventory.GetWeapon(mountIndex);

                        if ((_currentWeapon.weaponFeedType == InventoryWeaponFeedType.Ammunition && currWMI.InGunRounds > 0 && currWMI.Condition>0 ) ||
                            _currentWeapon.weaponFeedType != InventoryWeaponFeedType.Ammunition)
                        {

                            // Enable Auto Fire in Animator (if sub-state machine wants to use it)
                            autoFireEnabled = _currentWeapon.autoFire;

                            // Generate a random attack anim between 1 and the max anim count of the weapon
                            // We add 1 as second parameter is exclusive. So a 2nd parameter of 4 would 
                            // generate value between 1 and 3.
                            attackAnim = Random.Range(1, _currentWeapon.attackAnimCount + 1);
                        }
                        else
                        {
                            canFire = false;
                        }
                    }

                    // We can either fire a weapon or we have no weapon armed so can attack with our fists
                    if (canFire)
                    {
                        // Set the animator with these values
                        _armsAnimator.SetTrigger(_attackTriggerHash);
                        _armsAnimator.SetInteger(_attackAnimHash, attackAnim);
                        _armsAnimator.SetBool(_autoFireHash, autoFireEnabled);
                    }
                }
                else
                if (Input.GetButtonUp("Fire1"))
                {
                    // Pushing button down will have (potentially) enabled auto-fire
                    // so we must disable it on deprerssion of the button
                    _armsAnimator.SetBool(_autoFireHash, false);
                }

                if ( Input.GetButtonDown("Reload") && 
                     _currentWeapon!=null && 
                     _currentWeapon.weaponFeedType == InventoryWeaponFeedType.Ammunition &&
                     _availableAmmo > 0)
                {
                    // What mount are we reloading
                    mountIndex = _currentWeapon.weaponType == InventoryWeaponType.TwoHanded ? 1 : 0;
                    
                    // Get the mount info
                    InventoryWeaponMountInfo currWMI = _inventory.GetWeapon(mountIndex);

                    // If the gun is not at capacity then do the test to see if a reload makes sense
                    if (currWMI != null && currWMI.InGunRounds < _currentWeapon.ammoCapacity)
                    {
                        // Does an item exist in the ammo belt that makes sense to swap
                        // with the current one. We only want to reload if we are changing to a more plentiful clip
                        if (_inventory.IsReloadAvailable(mountIndex))
                        {
                            // Store in the Animator the number of times we need to repeat the reload sequence.
                            // This is only applicable to partial reloaders
                            if (_currentWeapon.reloadType == InventoryWeaponReloadType.Partial)
                                _armsAnimator.SetInteger(_reloadRepeatHash, _currentWeapon.ammoCapacity - currWMI.InGunRounds);
                             else
                                _armsAnimator.SetInteger(_reloadRepeatHash, 0);
                            
                            // If so, instruct the animator to do a reload animation in the current sub-state machine
                            _armsAnimator.SetBool(_reloadHash, true);
                        }
                    }

                }

                // Process Right Button click which will set the DualModeActive boolean in the Animator.
                // Any SubStateMachine for a weapon that supports dual action will use this to know
                // when to transition into the dual action subtree (think of a zoom feature)
                if (Input.GetButtonDown("DualMode") && _currentWeapon != null && _currentWeapon.dualMode)
                {
                    _armsAnimator.SetBool(_dualModeActive, !_armsAnimator.GetBool(_dualModeActive));
                }
            }

            // Do Insult
            if (Input.GetButtonDown("Taunt"))
            {
                DoTaunt();
            }
        }
		
	}

    // --------------------------------------------------------------------------------------------
    // Name :   ActivateFlashlight
    // Desc :   This is the function that scripts should use to enable the flashlight. This
    //          correctly makes the request through the animator.
    // --------------------------------------------------------------------------------------------
    public void ActivateFlashlight ( bool activate )
    {
        if (_armsAnimator)
        {
            _armsAnimator.SetBool(_flashlightHash, activate );
        }

        if (_secondaryFlashLight!=null)
        {
            _secondaryFlashLight.ActivateLight(activate);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Name :   DoTaunt
    // Desc :   Make a noise to attract an AI
    // ---------------------------------------------------------------------------------------------
    void DoTaunt()
    {
        if (_tauntSounds == null || Time.time < _nextTauntTime || !AudioManager.instance) return;
        AudioClip taunt = _tauntSounds[0];
        AudioManager.instance.PlayOneShotSound(_tauntSounds.audioGroup,
                                                taunt,
                                                transform.position,
                                                _tauntSounds.volume,
                                                _tauntSounds.spatialBlend,
                                                _tauntSounds.priority
                                                 );
        if (_soundEmitter != null)
            _soundEmitter.SetRadius(_tauntRadius);
        _nextTauntTime = Time.time + taunt.length;
    }

    // --------------------------------------------------------------------------------------------
    // Name : ActivateFlashlight_AnimatorCallback (AnimatorCallback)
    // Desc : This function is called by the Animator when it is time to activate / deactive
    //        the physical light of the flashlight.
    // Note : This function should never be called directly by code. This is an animator callback.
    //        If you call this directly the mesh and light will be instantly enabled/disabled whithout
    //        correct animation.
    // --------------------------------------------------------------------------------------------
    public void ActivateFlashlightMesh_AnimatorCallback( bool enableMesh, FlashlightType type )
    {
       if (type==FlashlightType.Primary)
        {
           _primaryFlashlight.ActivateMesh( enableMesh );
        }
    }

    public void ActivateFlashlightLight_AnimatorCallback(bool enableLight, FlashlightType type)
    {
        if (type == FlashlightType.Primary)
        {
            _primaryFlashlight.ActivateLight(enableLight);
        }
        else
        if (_secondaryFlashLight!=null)
        {
            _secondaryFlashLight.ActivateLight(enableLight);
        }
    }

    public void DoLevelComplete()
	{
		if (_fpsController) 
			_fpsController.freezeMovement = true;

	/*	if (_playerHUD)
		{
			_playerHUD.Fade( 4.0f, ScreenFadeType.FadeOut );
			_playerHUD.ShowMissionText( "Mission Completed");
			_playerHUD.Invalidate(this);
		}*/

		Invoke( "GameOver", 4.0f);
	}


	public void DoDeath()
	{
		if (_fpsController) 
			_fpsController.freezeMovement = true;

		/*if (_playerHUD)
		{
			_playerHUD.Fade( 3.0f, ScreenFadeType.FadeOut );
			_playerHUD.ShowMissionText( "Mission Failed");
			_playerHUD.Invalidate(this);
		}*/

		Invoke( "GameOver", 3.0f);
	}

	void GameOver()
	{
		// Show the cursor again
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;

		if (ApplicationManager.instance)
			ApplicationManager.instance.LoadMainMenu();
	}
}
