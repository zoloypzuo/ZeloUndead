using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Objects/Inventory System/Player Inventory")]
public class PlayerInventory : Inventory, ISerializationCallbackReceiver
{
    // Serialized Fields
    [Header("Mount Configuration and Starting Items")] [SerializeField]
    protected List<InventoryWeaponMountInfo> _weaponMounts = new List<InventoryWeaponMountInfo>();

    [SerializeField] protected List<InventoryAmmoMountInfo> _ammoMounts = new List<InventoryAmmoMountInfo>();

    [SerializeField]
    protected List<InventoryBackpackMountInfo> _backpackMounts = new List<InventoryBackpackMountInfo>();

    [Header("Audio Recordings")] [SerializeField]
    protected bool _autoPlayOnPickup = true;

    [SerializeField] protected List<InventoryItemAudio> _audioRecordings = new List<InventoryItemAudio>();

    [Header("Shared Variables")] [SerializeField]
    protected SharedTimedStringQueue _notificationQueue = null;

    [Header("Shared Variables - Broadcasters")] [SerializeField]
    protected SharedVector3 _playerPosition = null;

    [SerializeField] protected SharedVector3 _playerDirection = null;

    // Private
    // Runtime Mount Lists
    protected List<InventoryWeaponMountInfo> _weapons = new List<InventoryWeaponMountInfo>();
    protected List<InventoryAmmoMountInfo> _ammo = new List<InventoryAmmoMountInfo>();
    protected List<InventoryBackpackMountInfo> _backpack = new List<InventoryBackpackMountInfo>();
    protected List<InventoryItemAudio> _recordings = new List<InventoryItemAudio>();

    // The index of a recording currently being played
    protected int _activeAudioRecordingIndex = -1;

    // ISerializationCallbackReceiver
    public void OnBeforeSerialize() {
    }

    // Public Propeties
    public override bool autoPlayOnPickup {
        get { return _autoPlayOnPickup; }
        set { _autoPlayOnPickup = value; }
    }


    // --------------------------------------------------------------------------------------------
    // Name :   OnAfterDeserialize()
    // Desc :   We use this function so at runtime we clone the Mounts into copies that we can
    //          mutate without any affecting our original lists as defined in the inpsector. 
    // Note :   This has to be a deep copy as having two seperate lists referencing the same
    //          Mount objects will still mutate the original data.
    // --------------------------------------------------------------------------------------------
    public void OnAfterDeserialize() {
        // Clear our runtime lists
        _weapons.Clear();
        _ammo.Clear();
        _backpack.Clear();
        _recordings.Clear();

        // Clone inspector lists into runtime lists
        foreach (InventoryWeaponMountInfo info in _weaponMounts) {
            InventoryWeaponMountInfo clone = new InventoryWeaponMountInfo();
            clone.Condition = info.Condition;
            clone.InGunRounds = info.InGunRounds;
            clone.Weapon = info.Weapon;
            _weapons.Add(clone);

            // This implementation supports only two weapons so ignore any others specified
            if (_weapons.Count == 2) break;
        }

        foreach (InventoryAmmoMountInfo info in _ammoMounts) {
            InventoryAmmoMountInfo clone = new InventoryAmmoMountInfo();
            clone.Ammo = info.Ammo;
            clone.Rounds = info.Rounds;
            _ammo.Add(clone);
        }

        foreach (InventoryBackpackMountInfo info in _backpackMounts) {
            InventoryBackpackMountInfo clone = new InventoryBackpackMountInfo();
            clone.Item = info.Item;
            _backpack.Add(clone);
        }

        foreach (InventoryItemAudio recording in _audioRecordings) {
            _recordings.Add(recording);
        }

        // Reset the audio recording selection
        _activeAudioRecordingIndex = -1;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   GetWeapon
    // Desc :   Returns all information about the weapon at the requested mount
    // --------------------------------------------------------------------------------------------
    public override InventoryWeaponMountInfo GetWeapon(int mountIndex) {
        // In my implementation I allow only two weapon mounts but
        // your own derived classes may choose not to follow this pattern
        if (mountIndex < 0 || mountIndex > 1 || mountIndex >= _weapons.Count) return null;

        // Return the Weapon Mount Info
        return _weapons[mountIndex];
    }

    // --------------------------------------------------------------------------------------------
    // Name :   GetAmmo
    // Desc :   Returns all information about the ammo at the specified ammo mount
    // --------------------------------------------------------------------------------------------
    public override InventoryAmmoMountInfo GetAmmo(int mountIndex) {
        if (mountIndex < 0 || mountIndex >= _ammo.Count) return null;
        return _ammo[mountIndex];
    }

    // --------------------------------------------------------------------------------------------
    // Name :   GetBackpack
    // Desc :   Returns information about the item at the specified mount in the backpack
    // --------------------------------------------------------------------------------------------
    public override InventoryBackpackMountInfo GetBackpack(int mountIndex) {
        if (mountIndex < 0 || mountIndex >= _backpack.Count) return null;
        return _backpack[mountIndex];
    }

    // --------------------------------------------------------------------------------------------
    // Name :   GetAudioRecording
    // Desc :   Returns a recording at the specified index
    // --------------------------------------------------------------------------------------------
    public override InventoryItemAudio GetAudioRecording(int recordingIndex) {
        if (recordingIndex < 0 || recordingIndex >= _recordings.Count) return null;
        return _recordings[recordingIndex];
    }

    // --------------------------------------------------------------------------------------------
    // Name :   GetAudioRecordingCount
    // Desc :   Returns the number of Audio Logs in the list
    // --------------------------------------------------------------------------------------------
    public override int GetAudioRecordingCount() {
        return _recordings.Count;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   GetActiveAudioRecording
    // Desc :   Return the index of any Audio Recording currently playing.
    // --------------------------------------------------------------------------------------------
    public override int GetActiveAudioRecording() {
        return _activeAudioRecordingIndex;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   PlayAudioRecording
    // Desc :   Instructs audio player to play the Audio Log and sets the current active index
    // --------------------------------------------------------------------------------------------
    public override bool PlayAudioRecording(int recordingIndex) {
        if (recordingIndex < 0 || recordingIndex >= _recordings.Count) return false;

        InventoryAudioPlayer audioPlayer = InventoryAudioPlayer.instance;
        if (audioPlayer) {
            audioPlayer.OnEndAudio.RemoveListener(StopAudioListener);
            audioPlayer.OnEndAudio.AddListener(StopAudioListener);

            audioPlayer.PlayAudio(_recordings[recordingIndex]);
            _activeAudioRecordingIndex = recordingIndex;
        }

        return true;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   StopAudioListener
    // Desc :   Called by the AudioPlayer when the audio stops
    // ---------------------------------------------------------------------------------------------
    protected void StopAudioListener() {
        InventoryAudioPlayer audioPlayer = InventoryAudioPlayer.instance;
        if (audioPlayer)
            audioPlayer.OnEndAudio.RemoveListener(StopAudioListener);
        _activeAudioRecordingIndex = -1;
    }

    // -------------------------------------------------------------------------------------------
    // Name :   StopAudioRecording
    // Desc :   Instructs the Audio Player to stop playing it's audio. Triggers a manual stop of
    //          the audio player.
    // --------------------------------------------------------------------------------------------
    public override void StopAudioRecording() {
        InventoryAudioPlayer audioPlayer = InventoryAudioPlayer.instance;
        if (audioPlayer)
            audioPlayer.StopAudio();
    }


    // --------------------------------------------------------------------------------------------
    // Name :   DropAmmoItem
    // Desc :   Drop the item assign to the specified mount in the Ammo Belt
    // --------------------------------------------------------------------------------------------
    public override void DropAmmoItem(int mountIndex, bool playAudio = true) {
        if (mountIndex < 0 || mountIndex >= _backpack.Count) return;

        // Chck we have a valid BackPack mount in the inventory
        InventoryAmmoMountInfo itemMount = _ammo[mountIndex];
        if (itemMount == null || itemMount.Ammo == null) return;

        // Tell the item to drop itself (this will usually instantiate the proxy CollectableItem
        // in the scene.
        Vector3 position = _playerPosition != null ? _playerPosition.value : Vector3.zero;
        position += _playerDirection != null ? _playerDirection.value : Vector3.zero;
        CollectableAmmo sceneAmmo = itemMount.Ammo.Drop(position, playAudio) as CollectableAmmo;

        // Was a CollectableAmmo object created in the scene
        if (sceneAmmo) {
            // if so copy over instance data from mount into proxy
            sceneAmmo.rounds = _ammo[mountIndex].Rounds;
        }

        // Nullify the slot so it is empty
        _ammo[mountIndex].Ammo = null;
        _ammo[mountIndex].Rounds = 0;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   DropBackpackItem
    // Desc :   Drop the item at the specified mount in the Backpack
    // --------------------------------------------------------------------------------------------
    public override void DropBackpackItem(int mountIndex, bool playAudio = true) {
        if (mountIndex < 0 || mountIndex >= _backpack.Count) return;

        // Chck we have a valid BackPack mount in the inventory
        InventoryBackpackMountInfo itemMount = _backpack[mountIndex];
        if (itemMount == null || itemMount.Item == null) return;

        // Put it in the scene
        Vector3 position = _playerPosition != null ? _playerPosition.value : Vector3.zero;
        position += _playerDirection != null ? _playerDirection.value : Vector3.zero;
        itemMount.Item.Drop(position, playAudio);

        // Nullify the slot so it is empty
        _backpack[mountIndex].Item = null;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   DropWeaponItem
    // Desc :   Drop the weapon at the specified mount into the scene
    // --------------------------------------------------------------------------------------------
    public override void DropWeaponItem(int mountIndex, bool playAudio = true) {
        if (mountIndex < 0 || mountIndex >= _weapons.Count) return;

        // Chck we have a valid BackPack mount in the inventory
        InventoryWeaponMountInfo itemMount = _weapons[mountIndex];
        if (itemMount == null || itemMount.Weapon == null) return;

        // This is the weapon we want to drop
        InventoryItemWeapon weapon = itemMount.Weapon;

        // Drop it into the scene
        Vector3 position = _playerPosition != null ? _playerPosition.value : Vector3.zero;
        position += _playerDirection != null ? _playerDirection.value : Vector3.zero;
        CollectableWeapon sceneWeapon = weapon.Drop(position, playAudio) as CollectableWeapon;
        if (sceneWeapon) {
            // Copy over instance data from mount into proxy
            sceneWeapon.condition = itemMount.Condition;
            sceneWeapon.rounds = itemMount.InGunRounds;
        }

        // Nullify the slot so it is empty
        _weapons[mountIndex].Weapon = null;

        // Broadcast event that this weapon has been dropped
        OnWeaponDropped.Invoke(weapon);
    }

    // --------------------------------------------------------------------------------------------
    // Name :   UseBackpackItem
    // Desc :   Uses a backpack item.
    // --------------------------------------------------------------------------------------------
    public override bool UseBackpackItem(int mountIndex, bool playAudio = true) {
        // Is the selected slot valid to be consumed
        if (mountIndex < 0 || mountIndex >= _backpack.Count) return false;

        // Get weapon mount and return if no weapon assigned
        InventoryBackpackMountInfo backpackMountInfo = _backpack[mountIndex];
        if (backpackMountInfo.Item == null) return false;


        // Get the prefab from the app dictionary for this item
        InventoryItem backpackItem = backpackMountInfo.Item;

        // Tell the item to consume itself
        Vector3 position = _playerPosition != null ? _playerPosition.value : Vector3.zero;
        InventoryItem replacement = backpackItem.Use(position, playAudio);

        // Assign either null or a replacement item to that inventory slot
        _backpack[mountIndex].Item = replacement;

        // Mission Success
        return true;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   ReloadWeapon
    // Desc :   This function can be called from the InventoryUI and the main game code. Usually,
    //          We will want to play the audio when at the inventory screen but pass false to this
    //          function when calling from real time. 
    // --------------------------------------------------------------------------------------------
    public override bool ReloadWeapon(int mountIndex, bool playAudio = true) {
        // Invalid mount index (This invetory supports only 2 weapon mounts
        if (mountIndex < 0 || mountIndex > 1) return false;

        // Get weapon mount and return if no weapon assigned
        InventoryWeaponMountInfo weaponMountInfo = _weapons[mountIndex];
        if (weaponMountInfo.Weapon == null) return false;

        // To reduce typing :)
        InventoryItemWeapon weapon = weaponMountInfo.Weapon;
        InventoryItemAmmo ammo = weapon.ammo;

        // If no ammo assigned or this mount is already at the gun's capacity...just bail
        if (ammo == null || weaponMountInfo.InGunRounds >= weapon.ammoCapacity) return false;

        // If its a Non-Partial Reload type then simply search the belt for a clip
        // with the most bullets
        if (weapon.reloadType == InventoryWeaponReloadType.NonPartial) {
            // Search for a clip in our belt that has the highest round count
            // that matches the ammoID of the gun
            int ammoMountCandidate = -1;
            int roundCount = -1;
            for (int i = 0; i < _ammo.Count; i++) {
                InventoryAmmoMountInfo ammoMountInfo = _ammo[i];

                // If ammo at this mount is not the correct type of ammo skip it
                if (ammoMountInfo.Ammo != ammo) continue;

                // If the ammo we have found has more rounds than currently stored
                // at then record this as the best candidate for a swap
                if (ammoMountInfo.Rounds > roundCount) {
                    roundCount = ammoMountInfo.Rounds;
                    ammoMountCandidate = i;
                }
            }

            // If we didn't find any matching ammo or if we found no ammo
            // with more rounds in then is currently in the gun ... bail
            if (ammoMountCandidate == -1 || roundCount <= weaponMountInfo.InGunRounds) return false;

            // We have found some ammo we can use so consume it. First
            // remember how many rounds were in the clip that is in the gun currently
            int oldInGunRounds = weaponMountInfo.InGunRounds;

            // Set the round count to that of the new clip
            weaponMountInfo.InGunRounds = _ammo[ammoMountCandidate].Rounds;

            // In terms of the inventory...using a weapon is reloading it. Firing the weapon
            // is handled somewhere else entirely
            weapon.Use(Vector3.zero, playAudio);

            // If no rounds where in the gun before the reload, we have nothing to
            // swap with the mount so remove the item from the mount
            if (oldInGunRounds == 0) {
                // Clear the mount
                _ammo[ammoMountCandidate].Ammo = null;

                // Lets now create an instance of the ammo type in the scene
                // to simulate the clip we have ejected from the gun
                Vector3 position = _playerPosition != null ? _playerPosition.value : Vector3.zero;
                position += _playerDirection != null ? _playerDirection.value : Vector3.zero;
                CollectableAmmo sceneAmmo = ammo.Drop(position, playAudio) as CollectableAmmo;

                // Now we must configure the scene ammo we just instantiated to reflect the number
                // of rounds that was in the gun
                if (sceneAmmo) {
                    sceneAmmo.rounds = 0;
                }
            }
            // Otherwise simply swap the the clip
            else {
                _ammo[ammoMountCandidate].Rounds = oldInGunRounds;
            }
        }
        else if (weapon.reloadType == InventoryWeaponReloadType.Partial) {
            // If the gun is full, abort reload
            int roundsWanted = weapon.ammoCapacity - weaponMountInfo.InGunRounds;
            if (roundsWanted < 1) return false;

            // Loop through items on ammo belt searching for the correct ammo type
            for (int i = 0; i < _ammo.Count; i++) {
                // If not the right type of ammo then continue
                InventoryAmmoMountInfo ammoMountInfo = _ammo[i];
                if (ammoMountInfo.Ammo != weapon.ammo) continue;

                // Otherwise we have found some ammo so lets remove
                // some items from it
                int ammoTaken = Mathf.Min(roundsWanted, ammoMountInfo.Rounds);
                weaponMountInfo.InGunRounds += ammoTaken;
                ammoMountInfo.Rounds -= ammoTaken;
                roundsWanted -= ammoTaken;

                // Use the weapon to reload it
                weapon.Use(Vector3.zero, playAudio);

                // If we have emptied this ammo item then remove from belt
                if (ammoMountInfo.Rounds == 0) ammoMountInfo.Ammo = null;

                // If the gun is full then return and don't continue to seek
                if (roundsWanted <= 0) break;
            }
        }

        // Weapon Successfully Reload
        return true;
    }

    // --------------------------------------------------------------------------------------------
    // Name : AddItem
    // Desc : Adds an item to the inventory from a passed CollectableItem 
    // --------------------------------------------------------------------------------------------

    public override bool AddItem(CollectableItem collectableItem, bool playAudio = true) {
        // Can't add if passed null or the CollectableItem has no associated InventoryItem
        if (collectableItem == null || collectableItem.inventoryItem == null) return false;

        // Determine the item type and call the appropriate function to the perform the
        // add to the inventory and remove the item from the scene.
        InventoryItem invItem = collectableItem.inventoryItem;

        switch (invItem.category) {
            // If its a standard backpack item
            case InventoryItemType.Consumable:
                return AddBackpackItem(invItem, collectableItem, playAudio);

            // If its a weapon
            case InventoryItemType.Weapon:
                return AddWeaponItem(invItem as InventoryItemWeapon, collectableItem as CollectableWeapon, playAudio);

            // If its ammo
            case InventoryItemType.Ammunition:
                return AddAmmoItem(invItem as InventoryItemAmmo, collectableItem as CollectableAmmo, playAudio);

            // Audio Stuff we will do later
            case InventoryItemType.Recording:
                return AddRecordingItem(invItem as InventoryItemAudio, collectableItem as CollectableAudio, playAudio);
        }

        return false;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   AddRecordingItem
    // Desc :   Adds an AudioRecording to the Inventory and begins playing is AutoPlay is enabled
    // --------------------------------------------------------------------------------------------
    protected bool AddRecordingItem(InventoryItemAudio inventoryAudio, CollectableAudio collectableAudio,
        bool playAudio) {
        if (inventoryAudio) {
            // Play the pickup sound
            inventoryAudio.Pickup(collectableAudio.transform.position, playAudio);

            // Add audio recording to the list
            _recordings.Add(inventoryAudio);

            // Play on Pick if configured to do so
            if (_autoPlayOnPickup) {
                // Tell Inventory to play this audio recording immediately
                // This should be the one at end of the recordings list
                PlayAudioRecording(_recordings.Count - 1);
            }

            if (_notificationQueue)
                _notificationQueue.Enqueue("Audio Recording Added");


            // Data successfully retrieved
            return true;
        }

        return false;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   AddBackpackItem
    // Desc :   Searches for the first available Backpack Mount and assigns the passed item to it.
    // --------------------------------------------------------------------------------------------
    protected bool AddBackpackItem(InventoryItem inventoryItem, CollectableItem collectableItem, bool playAudio) {
        // Search for empty mount in Backpack
        for (int i = 0; i < _backpack.Count; i++) {
            // A free mount is one with no item assigned
            if (_backpack[i].Item == null) {
                // Store this Item type at the mount
                _backpack[i].Item = inventoryItem;

                // Pickup
                inventoryItem.Pickup(collectableItem.transform.position, playAudio);

                // Broadcast that attempt was successful
                if (_notificationQueue)
                    _notificationQueue.Enqueue("Added " + inventoryItem.inventoryName + " to Backpack");

                // Success so return
                return true;
            }
        }

        // Broadcast that attempt was NOT successful
        if (_notificationQueue)
            _notificationQueue.Enqueue("Could not pickup " + inventoryItem.inventoryName + "\nNo room in Backpack");

        return false;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   AddAmmoItem
    // Desc :   Searches for the first available Ammo Mount and assigns the passed ammo to it.
    // --------------------------------------------------------------------------------------------
    protected bool AddAmmoItem(InventoryItemAmmo inventoryItemAmmo, CollectableAmmo collectableAmmo, bool playAudio) {
        // Search for empty mount on ammo belt
        for (int i = 0; i < _ammo.Count; i++) {
            // A free mount is one with no Ammo assigned (.Ammo==null)
            if (_ammo[i].Ammo == null) {
                // Store this Ammo type at the mount
                _ammo[i].Ammo = inventoryItemAmmo;

                // Copy over instance data
                _ammo[i].Rounds = collectableAmmo.rounds;

                // Pickup
                inventoryItemAmmo.Pickup(collectableAmmo.transform.position, playAudio);

                // Broadcast that attempt was successful
                if (_notificationQueue)
                    _notificationQueue.Enqueue("Added " + inventoryItemAmmo.inventoryName + " to Ammo Belt");

                // Success so return
                return true;
            }
        }

        // Broadcast that attempt was NOT successful
        if (_notificationQueue)
            _notificationQueue.Enqueue("Could not pickup " + inventoryItemAmmo.inventoryName +
                                       "\nNo room in Ammo Belt");

        return false;
    }

    // --------------------------------------------------------------------------------------------
    // Name	:	AddWeaponItem
    // Desc	:	Adding weapons is actually handled very differently from other
    //			objects. All this function does is record the object we wish
    //			to change to and send out a message to invoke the correct dismount/mount 
    //          animations. The ACTUAL pickup is deferred until AssignWeapon function
    //          is called by the Listener.
    // Note :   In DeadEarth the listener is the CharacterManager's which handles syncing
    //          weapon animation.
    // --------------------------------------------------------------------------------------------
    protected bool AddWeaponItem(InventoryItemWeapon inventoryItemWeapon, CollectableWeapon collectableWeapon,
        bool playAudio) {
        // Create a mount info object to describe the weapon and instance data we wish
        // to change to.
        InventoryWeaponMountInfo wmi = new InventoryWeaponMountInfo();
        wmi.Weapon = inventoryItemWeapon;
        wmi.Condition = collectableWeapon.condition;
        wmi.InGunRounds = collectableWeapon.rounds;

        // Invoke event so the object in charge of weapon switching and animation (our CharacterManager for example)
        // can set the process of switching in motion.
        OnWeaponChange.Invoke(wmi);

        return true;
    }

    // ----------------------------------------------------------------------------------
    // Name	:	AssignWeaponToMount
    // Desc	:	This function assigns the passed weaponMountInfo DIRECTLY to the specified
    //          mount.
    // NOTE	:	This function does no house keeping...it does NOT remove/drop/enable/disable
    //			any weapons and will overwrite anything stored there. You should use
    //			AddItem to add a weapon to the inventory in a usual case.
    //			This function will be used by the FPS Arms Animation System.
    // -----------------------------------------------------------------------------------
    public override void AssignWeapon(int mountIndex, InventoryWeaponMountInfo mountInfo) {
        if (mountInfo == null || mountInfo.Weapon == null) return;
        if (mountIndex < 0 || mountIndex >= _weapons.Count) return;

        // Assign the new mount info
        _weapons[mountIndex] = mountInfo;

        // Play the pickup sound 
        _weapons[mountIndex].Weapon.Pickup(_playerPosition != null ? _playerPosition.value : Vector3.zero);

        // And then notify them the new weapon has been added
        if (_notificationQueue)
            _notificationQueue.Enqueue("Weapon Mounted : " + mountInfo.Weapon.inventoryName);
    }

    // --------------------------------------------------------------------------------------------
    // Name :   GetAvailableAmmo
    // Desc :   Returns the number of bullets in the inventory for the passed ammo type. If true
    //          is passed for the second parameter this will include bullet at the weapon mount
    //          as well.
    // --------------------------------------------------------------------------------------------
    public override int GetAvailableAmmo(InventoryItemAmmo ammo,
        AmmoAmountRequestType requestType = AmmoAmountRequestType.NoWeaponAmmo) {
        if (!ammo) return 0;

        // Sum all ammo of the correct type on the ammo belt
        int roundCount = 0;

        // If we want to inlcude ammo belt ammo
        if (requestType != AmmoAmountRequestType.WeaponAmmoOnly) {
            for (int i = 0; i < _ammo.Count; i++) {
                InventoryAmmoMountInfo ammoMountInfo = _ammo[i];
                if (ammoMountInfo.Ammo != ammo) continue;
                roundCount += ammoMountInfo.Rounds;
            }
        }

        // If we want to include in gun rounds
        if (requestType != AmmoAmountRequestType.NoWeaponAmmo) {
            for (int i = 0; i < 2; i++) {
                InventoryWeaponMountInfo weaponMountInfo = _weapons[i];
                if (weaponMountInfo.Weapon == null || weaponMountInfo.Weapon.ammo != ammo) continue;
                roundCount += weaponMountInfo.InGunRounds;
            }
        }

        return roundCount;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   DecreaseAmmoInWeapon
    // Desc :   Decreases the ammunition currently in the weapon at the specified mount by the
    //          specified amount if possible. This only takes ammo from the weapon. Has no effect
    //          in the case of a non ammunition weapon.
    // Return : The remaining ammo in the weapon
    // --------------------------------------------------------------------------------------------
    public override int DecreaseAmmoInWeapon(int mountIndex, int amount = 1) {
        // If mount Index is out of range return -1 (error)
        if (mountIndex < 0 || mountIndex >= _weapons.Count || _weapons[mountIndex] == null) return -1;

        // If there is no weapon at the mount then do nothing and return zero
        InventoryWeaponMountInfo wmi = _weapons[mountIndex];
        if (wmi.Weapon == null) return 0;

        // If this is an ammunition fed weapon then let's check the ammo
        if (wmi.Weapon.weaponFeedType == InventoryWeaponFeedType.Ammunition) {
            // Subtract the amount from the weapon
            wmi.InGunRounds = Mathf.Max(wmi.InGunRounds - amount, 0);

            return wmi.InGunRounds;
        }

        // It's a weapon that doesn't take ammo (melee for example) so return 0 as the ammo count
        return 0;
    }

    // --------------------------------------------------------------------------------------------
    // Name :	IsReloadAvailable
    // Desc	:	Returns true if ammo exists in the users inventory that has a greater
    //			number of rounds than the number passed. For Partial reload weapons
    //			such as Shotgun, the passed amount will always be 1 as rounds can
    //			be loaded one at a time. For NonPartial (magazine based) it will 
    //			only return true if a clip is found that contains MORE rounds
    //			than those currently in the Weapon Mount
    // --------------------------------------------------------------------------------------------
    public override bool IsReloadAvailable(int weaponMountIndex) {
        if (weaponMountIndex < 0 || weaponMountIndex >= _weapons.Count) return false;

        // Get the weapon mount and the weapon from that mount
        InventoryWeaponMountInfo weaponMountInfo = _weapons[weaponMountIndex];
        InventoryItemWeapon weapon = weaponMountInfo.Weapon;

        // If no weapon, no ammo or no ammo needed return false
        if (!weapon ||
            weapon.reloadType == InventoryWeaponReloadType.None ||
            weapon.weaponType == InventoryWeaponType.None ||
            weaponMountInfo.InGunRounds >= weapon.ammoCapacity ||
            weapon.ammo == null) return false;


        // If its a Non-Partial Reload type then simply search the belt for a clip
        // with the most bullets
        if (weapon.reloadType == InventoryWeaponReloadType.NonPartial) {
            // Search for a clip in our belt that has the highest round count
            // that matches the ammo of the gun
            int roundCount = -1;
            for (int i = 0; i < _ammo.Count; i++) {
                InventoryAmmoMountInfo ammoMountInfo = _ammo[i];
                if (ammoMountInfo.Ammo != weapon.ammo) continue;
                if (ammoMountInfo.Rounds > roundCount) {
                    roundCount = ammoMountInfo.Rounds;
                }
            }

            // If the highest available rounds found is less than or equal to the
            // amount we are looking for as a minimum to make a reload worth while return false
            if (roundCount <= weaponMountInfo.InGunRounds) return false;

            // A clip has been found so return true to allow a reload to proceed
            return true;
        }
        else if (weapon.reloadType == InventoryWeaponReloadType.Partial) {
            // Loop through items on ammo belt searching for the correct ammo type
            for (int i = 0; i < _ammo.Count; i++) {
                // If not the right type of ammo then continue
                InventoryAmmoMountInfo ammoMountInfo = _ammo[i];
                if (ammoMountInfo.Ammo != weapon.ammo) continue;

                // Partial reload so only looks for at last 1
                if (ammoMountInfo.Rounds > 0) return true;
            }

            return false;
        }

        // Fall through case in case we add more reload types
        return false;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   Search
    // Desc :   Searches the Inventory for the FIRST item that matches the passed type and
    //          return the mount. Returns NULL if item type does not exist.
    // --------------------------------------------------------------------------------------------
    public override InventoryMountInfo Search(InventoryItem matchItem) {
        // Don't be silly :)
        if (matchItem == null) return null;

        // Search backpack first
        for (int i = 0; i < _backpack.Count; i++) {
            if (_backpack[i].Item == matchItem) return _backpack[i];
        }

        // ...then Ammo Belt
        for (int i = 0; i < _ammo.Count; i++) {
            if (_ammo[i].Ammo == matchItem) return _ammo[i];
        }

        // ...then weapon's mounts
        for (int i = 0; i < _weapons.Count; i++) {
            if (_weapons[i].Weapon == matchItem) return _weapons[i];
        }

        // Not found
        return null;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   Remove
    // Desc :   Removes all items (from all mounts) of the specified type if found. 
    // NOTE :   This will perform a silent remove...evapouration...of the items. This will not
    //          be instantiated in the scene, they will not have their drop handlers called.
    // Ret  :   Returns number of items removed
    // --------------------------------------------------------------------------------------------
    public override int Remove(InventoryItem matchItem) {
        // Don't be silly :)
        if (matchItem == null) return 0;
        int removeCount = 0;

        // Search backpack first
        for (int i = 0; i < _backpack.Count; i++) {
            if (_backpack[i].Item == matchItem) {
                _backpack[i].Item = null;
                removeCount++;
            }
        }

        // ...then Ammo Belt
        for (int i = 0; i < _ammo.Count; i++) {
            if (_ammo[i].Ammo == matchItem) {
                _ammo[i].Ammo = null;
                _ammo[i].Rounds = 0;
                removeCount++;
            }
        }

        // ...then weapon's mounts
        for (int i = 0; i < _weapons.Count; i++) {
            if (_weapons[i].Weapon == matchItem) {
                _weapons[i].Weapon = null;
                _weapons[i].Condition = 0;
                _weapons[i].InGunRounds = 0;
                removeCount++;
            }
        }

        // Return item count removed
        return removeCount;
    }


    // --------------------------------------------------------------------------------------------
    // Name : RemoveWeapon
    // Desc : Clears the specified Weapon Mount
    // --------------------------------------------------------------------------------------------
    public override bool RemoveWeapon(int mountIndex) {
        // Is there something to remove...if not return false
        if (mountIndex < 0 || mountIndex >= _weapons.Count || _weapons[mountIndex].Weapon == null) return false;

        _weapons[mountIndex].Weapon = null;
        _weapons[mountIndex].Condition = 0;
        _weapons[mountIndex].InGunRounds = 0;

        return true;
    }

    // --------------------------------------------------------------------------------------------
    // Name : RemoveAmmo
    // Desc : Clears the specified Ammo Mount
    // --------------------------------------------------------------------------------------------
    public override bool RemoveAmmo(int mountIndex) {
        // Is there something to remove...if not return false
        if (mountIndex < 0 || mountIndex >= _ammo.Count || _ammo[mountIndex].Ammo == null) return false;

        _ammo[mountIndex].Ammo = null;
        _ammo[mountIndex].Rounds = 0;

        return true;
    }

    // --------------------------------------------------------------------------------------------
    // Name : RemoveBackpack
    // Desc : Clears the specified Backpack Mount
    // --------------------------------------------------------------------------------------------
    public override bool RemoveBackpack(int mountIndex) {
        // Is there something to remove...if not return false
        if (mountIndex < 0 || mountIndex >= _backpack.Count || _backpack[mountIndex].Item == null) return false;

        _backpack[mountIndex].Item = null;

        return true;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   MOUNT GETTERS
    // Desc :   Low-Level access to the underlying runtime mount lists.
    // --------------------------------------------------------------------------------------------
    public override List<InventoryWeaponMountInfo> GetAllWeapons() {
        return _weapons;
    }

    public override List<InventoryAmmoMountInfo> GetAllAmmo() {
        return _ammo;
    }

    public override List<InventoryBackpackMountInfo> GetAllBackpack() {
        return _backpack;
    }
}