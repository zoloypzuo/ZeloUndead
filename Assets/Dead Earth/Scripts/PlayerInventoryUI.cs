using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryPanelType
{
    None,
    Backpack,
    AmmoBelt,
    Weapons,
    PDA
}

[System.Serializable]
public struct InventoryUI_PDAReferences
{
    public Transform _logEntries;
    public RawImage _pDAImage;
    public Text _pDAAuthor;
    public Text _pDASubject;
    public Slider _timelineSlider;
    public Toggle _autoplayOnPickup;
    public GameObject _logEntryPrefab;
    public Text _transcriptText;
    public Text _notificationText;
}

[System.Serializable]
public struct InventoryUI_Status
{
    public Slider HealthSlider;
    public Slider InfectionSlider;
    public Slider StaminaSlider;
    public Slider FlashlightSlider;
    public Slider NightVisionSlider;
}


[System.Serializable]
public class InventoryUI_TabGroupItem
{
    public Text TabText;
    public GameObject LayoutContainer;
}

[System.Serializable]
public class InventoryUI_TabGroup
{
    public List<InventoryUI_TabGroupItem> Items;
    public int ActiveItem;
    public Color TextHover;
    public Color TextInactive;
    public Color TextColor;
}

[System.Serializable]
public struct InventoryUI_DescriptionLayout
{
    public GameObject LayoutContainer;
    public Image Image;
    public Text Title;
    public ScrollRect ScrollView;
    public Text Description;
}

[System.Serializable]
public struct InventoryUI_ActionButton
{
    public GameObject GameObject;
    public Text ButtonText;
}

// ------------------------------------------------------------------------------------------------
// CLASS    :   PlayerInventoryUI
// DESC     :   Manages the UI used to interact and display the player's inventory
// ------------------------------------------------------------------------------------------------
public class PlayerInventoryUI : MonoBehaviour
{
    // The Inventory to manage
    [Header("Inventory")] [SerializeField] protected Inventory _inventory = null;

    // Backpack Mounts
    [Header("Equipment Mount References")] [SerializeField]
    protected List<GameObject> _backpackMounts = new List<GameObject>();

    protected List<Image> _backpackMountImages = new List<Image>();
    protected List<Text> _backpackMountText = new List<Text>();

    // Weapon Mounts
    [SerializeField] protected List<GameObject> _weaponMounts = new List<GameObject>();
    protected List<Text> _weaponMountNames = new List<Text>();
    protected List<Slider> _weaponMountSliders = new List<Slider>();
    protected List<Image> _weaponMountImages = new List<Image>();
    protected List<GameObject> _weaponMountAmmoInfo = new List<GameObject>();
    protected List<Text> _weaponMountRounds = new List<Text>();
    protected List<Text> _weaponMountReloadType = new List<Text>();


    [SerializeField] protected List<GameObject> _ammoMounts = new List<GameObject>();
    protected List<Image> _ammoMountImages = new List<Image>();
    protected List<Text> _ammoMountEmptyText = new List<Text>();
    protected List<Text> _ammoMountRoundsText = new List<Text>();

    // PDA UI References
    [SerializeField] protected InventoryUI_PDAReferences _pDAReferences;

    // Backpack Scroll Rect
    [Header("Backpack Scroll Rect")] [SerializeField]
    protected ScrollRect _backpackScrollRect = null;


    // Inspector Assigned Meter References
    [Header("UI Meter References")] [SerializeField]
    protected InventoryUI_Status _statusPanelUI;

    [Header("Tab Groups")] [SerializeField]
    protected List<InventoryUI_TabGroup> _tabGroups = new List<InventoryUI_TabGroup>();

    [Header("Description Layouts")] [SerializeField]
    protected InventoryUI_DescriptionLayout _generalDescriptionLayout;

    [SerializeField] protected InventoryUI_DescriptionLayout _weaponDescriptionLayout;

    // Action Button UI References
    [Header("Action Button UI References")] [SerializeField]
    protected InventoryUI_ActionButton _actionButton1;

    [SerializeField] protected InventoryUI_ActionButton _actionButton2;

    [Header("Shared Variables")] [SerializeField]
    SharedFloat _health = null;

    [SerializeField] SharedFloat _infection = null;
    [SerializeField] SharedFloat _stamina = null;
    [SerializeField] SharedFloat _flashlight = null;
    [SerializeField] SharedFloat _nightvision = null;
    [SerializeField] SharedString _transcriptText = null;
    [SerializeField] SharedTimedStringQueue _notificationQueue = null;


    [Header("Colors")] [SerializeField] Color _backpackMountHover = Color.cyan;
    [SerializeField] Color _ammoMountHover = Color.grey;
    [SerializeField] Color _weaponMountHover = Color.red;

    // Properties
    // Facilitates runtime hot-swapping of Inventories
    public Inventory inventory {
        get { return _inventory; }
        set { _inventory = value; }
    }

    // Internals
    protected Color _backpackMountColor;
    protected Color _weaponMountColor;
    protected Color _ammoMountColor;

    protected InventoryPanelType _selectedPanelType = InventoryPanelType.None;
    protected int _selectedMount = -1;
    protected bool _isInitialized = false;


    // --------------------------------------------------------------------------------------------
    // Name : OnEnable
    // Desc : Called by UNITY everytime the UI is enabled
    // --------------------------------------------------------------------------------------------
    protected virtual void OnEnable() {
        // Clear buffered input
        Input.ResetInputAxes();

        // Freeze Time
        Time.timeScale = 0.0f;

        // Freeze Audio
        AudioListener.pause = true;

        // Update the UI to display the current state of Inventory in its
        // reset position
        Invalidate();
    }

    protected virtual void OnDisable() {
        // Unpause gameplay
        Time.timeScale = 1.0f;

        // Unpause Audio
        AudioListener.pause = false;

        // Clear buffered input
        Input.ResetInputAxes();
    }

    // --------------------------------------------------------------------------------------------
    // Name :   Invalidate
    // Desc :   This function updates the UI so all elements reflect the current state of the
    //          user's inventory. This function also resets the Inventory to an unselected
    //          state.
    // --------------------------------------------------------------------------------------------
    protected virtual void Invalidate() {
        // Make sure its initialized before its is rendered for the first time
        if (!_isInitialized)
            Initialize();

        // Reset Selections
        _selectedPanelType = InventoryPanelType.None;
        _selectedMount = -1;

        // Deactivate Description Panels
        if (_generalDescriptionLayout.LayoutContainer != null)
            _generalDescriptionLayout.LayoutContainer.SetActive(false);

        if (_weaponDescriptionLayout.LayoutContainer != null)
            _weaponDescriptionLayout.LayoutContainer.SetActive(false);

        // Deactivate the action buttons
        if (_actionButton1.GameObject != null)
            _actionButton1.GameObject.SetActive(false);

        if (_actionButton2.GameObject != null)
            _actionButton2.GameObject.SetActive(false);

        // Clear the Weapon Mounts
        for (int i = 0; i < _weaponMounts.Count; i++) {
            if (_weaponMounts[i] != null) {
                if (_weaponMountImages[i] != null) _weaponMountImages[i].sprite = null;
                if (_weaponMountNames[i] != null) _weaponMountNames[i].text = "";
                if (_weaponMountSliders[i] != null) _weaponMountSliders[i].enabled = false;

                _weaponMounts[i].SetActive(false);
                _weaponMounts[i].transform.GetComponent<Image>().fillCenter = false;
            }
        }

        // Iterate over the UI Backpack mounts and set all to empty and unselected
        for (int i = 0; i < _backpackMounts.Count; i++) {
            // Clear sprite and deactivate mount
            if (_backpackMountImages[i] != null) {
                _backpackMountImages[i].gameObject.SetActive(false);
                _backpackMountImages[i].sprite = null;
            }

            // Enable the text for this slot that says "EMPTY"
            if (_backpackMountText[i] != null)
                _backpackMountText[i].gameObject.SetActive(true);

            // Make all mounts look unselected
            if (_backpackMounts[i] != null) {
                // Get the image of the mount itself (the frame)
                Image img = _backpackMounts[i].GetComponent<Image>();
                if (img) {
                    img.fillCenter = false;
                    img.color = _backpackMountColor;
                }
            }
        }

        // Configure the ammo slots
        for (int i = 0; i < _ammoMounts.Count; i++) {
            // Clear Sprite and deactivate mount
            if (_ammoMounts[i] != null) {
                if (_ammoMountImages[i]) {
                    _ammoMountImages[i].gameObject.SetActive(false);
                    _ammoMountImages[i].sprite = null;
                }
            }

            // Enable the text for this slot that says "EMPTY"
            if (_ammoMountEmptyText[i] != null) _ammoMountEmptyText[i].gameObject.SetActive(true);
            if (_ammoMountRoundsText[i] != null) _ammoMountRoundsText[i].gameObject.SetActive(false);

            // Give Mount Frame unselected look
            if (_ammoMounts[i] != null) {
                Image img = _ammoMounts[i].GetComponent<Image>();
                if (img) {
                    img.fillCenter = false;
                    img.color = _ammoMountColor;
                }
            }
        }

        // Other PDA things
        if (_pDAReferences._autoplayOnPickup && _inventory) {
            _pDAReferences._autoplayOnPickup.isOn = _inventory.autoPlayOnPickup;
        }

        // Finally update the status panel
        if (_statusPanelUI.HealthSlider) _statusPanelUI.HealthSlider.value = _health.value;
        if (_statusPanelUI.InfectionSlider) _statusPanelUI.InfectionSlider.value = _infection.value;
        if (_statusPanelUI.StaminaSlider) _statusPanelUI.StaminaSlider.value = _stamina.value;
        if (_statusPanelUI.FlashlightSlider) _statusPanelUI.FlashlightSlider.value = _flashlight.value;
        if (_statusPanelUI.NightVisionSlider) _statusPanelUI.NightVisionSlider.value = _nightvision.value;

        // DO we have a valid Inventory Referenc. If so...let's paint it
        if (_inventory != null) {
            // Configure Weapons Panel by iterating through each mount
            for (int i = 0; i < _weaponMounts.Count; i++) {
                // Do we have a weapon mount here
                if (_weaponMounts[i] != null) {
                    // Get the matching mount and weapon data from the inventory
                    InventoryWeaponMountInfo weaponMountInfo = _inventory.GetWeapon(i);
                    InventoryItemWeapon weapon = null;
                    if (weaponMountInfo != null)
                        weapon = weaponMountInfo.Weapon;

                    // No weapon info here to skip this mount
                    if (weapon == null) continue;

                    // Set sprite and name of weapon
                    if (_weaponMountImages[i] != null) _weaponMountImages[i].sprite = weapon.inventoryImage;
                    if (_weaponMountNames[i] != null) _weaponMountNames[i].text = weapon.inventoryName;

                    // If its a melee weapon then deactivate the entire AmmoInfo section of the UI
                    // otherwise Enabled it and show the Reload Type and Rounds in Gun
                    if (_weaponMountAmmoInfo[i] != null) {
                        if (weapon.weaponFeedType == InventoryWeaponFeedType.Melee) {
                            _weaponMountAmmoInfo[i].SetActive(false);
                        }
                        else {
                            // Activate Mount
                            _weaponMountAmmoInfo[i].SetActive(true);

                            // Display Reload Type
                            if (_weaponMountReloadType[i] != null)
                                _weaponMountReloadType[i].text = weapon.reloadType.ToString();


                            if (_weaponMountRounds[i] != null)
                                _weaponMountRounds[i].text = weaponMountInfo.InGunRounds + " / " + weapon.ammoCapacity;
                        }
                    }

                    // Update the condition slider
                    if (_weaponMountSliders[i] != null) {
                        _weaponMountSliders[i].enabled = true;
                        _weaponMountSliders[i].value = weaponMountInfo.Condition;
                    }

                    _weaponMounts[i].SetActive(true);
                }
            }

            // Configure Ammo Mounts
            for (int i = 0; i < _ammoMounts.Count; i++) {
                // Clear Sprite and deactivate mount
                if (_ammoMounts[i] != null) {
                    // Get the ammo and it's mount info for this mount
                    InventoryAmmoMountInfo ammoMountInfo = _inventory.GetAmmo(i);
                    InventoryItemAmmo ammo = null;
                    if (ammoMountInfo != null)
                        ammo = ammoMountInfo.Ammo;

                    // No weapon at this mount so skip
                    if (ammo == null) continue;

                    // Set image 
                    if (_ammoMountImages[i]) {
                        _ammoMountImages[i].gameObject.SetActive(true);
                        _ammoMountImages[i].sprite = ammoMountInfo.Ammo.inventoryImage;
                    }

                    // Set and Enable Rounds Text
                    if (_ammoMountRoundsText[i] != null) {
                        _ammoMountRoundsText[i].gameObject.SetActive(true);
                        _ammoMountRoundsText[i].text = ammoMountInfo.Rounds.ToString();
                    }

                    // Disable Empty text
                    if (_ammoMountEmptyText[i] != null)
                        _ammoMountEmptyText[i].gameObject.SetActive(false);
                }
            }

            // Iterate over the UI Backpack mounts and set all to empty and unselected
            for (int i = 0; i < _backpackMounts.Count; i++) {
                if (_backpackMounts[i] != null) {
                    InventoryBackpackMountInfo backpackMountInfo = _inventory.GetBackpack(i);
                    InventoryItem item = null;
                    if (backpackMountInfo != null)
                        item = backpackMountInfo.Item;

                    if (item != null) {
                        // Set sprite and activate mount
                        if (_backpackMountImages[i] != null) {
                            _backpackMountImages[i].gameObject.SetActive(true);
                            _backpackMountImages[i].sprite = item.inventoryImage;
                        }

                        // Disable the text for this slot that says "EMPTY"
                        if (_backpackMountText[i] != null)
                            _backpackMountText[i].gameObject.SetActive(false);
                    }
                }
            }

            // If we have a parent to connect entries to
            if (_pDAReferences._logEntries) {
                int audioCount = _inventory.GetAudioRecordingCount();
                int displayCount = _pDAReferences._logEntries.childCount;

                // Process each recording we own in our inventory
                for (int i = 0; i < audioCount; i++) {
                    // Get the prefab that represents this audio log
                    InventoryItemAudio audioItem = _inventory.GetAudioRecording(i);

                    // Do we need to create a new entry in the scroll view
                    if (i >= displayCount) {
                        // Instantiate a new PDAEntry game object
                        GameObject go = Instantiate(_pDAReferences._logEntryPrefab);
                        if (go) {
                            // Grab the InventoryPDAEntry component
                            PlayerInventoryUI_PDAEntry pdaEntry = go.GetComponent<PlayerInventoryUI_PDAEntry>();
                            if (pdaEntry) {
                                // Make child of container object in UI
                                pdaEntry.transform.SetParent(_pDAReferences._logEntries, false);

                                // Order it in the list according to the order in which it was collected
                                pdaEntry.transform.SetSiblingIndex(i);

                                // Set the display name, the subject and the ID of the recording in the pda entry
                                pdaEntry.SetData(audioItem, i);
                            }
                        }
                    }
                    else {
                        // In this instance we have an entry in the scroll view that we can simply re-use and refresh
                        // with the data. So, get the PDAEntry component on the current child 
                        PlayerInventoryUI_PDAEntry pdaEntry = _pDAReferences._logEntries.GetChild(i)
                            .GetComponent<PlayerInventoryUI_PDAEntry>();
                        if (pdaEntry) {
                            // And set the data
                            pdaEntry.SetData(audioItem, i);
                        }
                    }
                }

                // Delete any entires on the drop down we are not using (just in case there are
                // more enties instantiated in our list than we have recordings in our inventory)
                for (int i = audioCount; i < _pDAReferences._logEntries.childCount; i++) {
                    Destroy(_pDAReferences._logEntries.GetChild(i).gameObject);
                }
            }
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   RefreshPDAEntries
    // Desc :   This is essentially a handler function that is called from the child PDA Entries
    //          when a new selection has been made ad we want to repaint all the colors of the
    //          entries
    // ---------------------------------------------------------------------------------------------
    public void RefreshPDAEntries() {
        if (!inventory) return;

        // Iterate through all entries in the list
        for (int i = 0; i < _pDAReferences._logEntries.childCount; i++) {
            // Get the PDA Entry UI
            PlayerInventoryUI_PDAEntry entry = _pDAReferences._logEntries.GetChild(i)
                .GetComponent<PlayerInventoryUI_PDAEntry>();
            InventoryItemAudio audioItem = _inventory.GetAudioRecording(i);

            // If we have the PDAEntry reference and its matching data then repaint it
            if (audioItem && entry)
                entry.SetData(audioItem, i);
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   Initialize
    // Desc :   This function is called ONCE the very first time the InventoryUI game object
    //          is enabled to display the inventory. It finds additional UI objects in the
    //          UI hierarchy and caches references to them. This saves us the work of having
    //          to hook up EVERY SINGLE REFERENCE via the inspector.
    // --------------------------------------------------------------------------------------------
    protected virtual void Initialize() {
        // This function should only be called once
        if (_isInitialized) return;

        // It has now been called
        _isInitialized = true;

        // Cache Original color of Backpack frame color so we can restore
        // when not selected
        if (_backpackMounts.Count > 0 && _backpackMounts[0] != null) {
            Image tmp = _backpackMounts[0].GetComponent<Image>();
            if (tmp) _backpackMountColor = tmp.color;
        }

        // Do the same for Ammount Mount Frame
        if (_ammoMounts.Count > 0 && _ammoMounts[0] != null) {
            Image tmp = _ammoMounts[0].GetComponent<Image>();
            if (tmp) _ammoMountColor = tmp.color;
        }

        // Do the same for the Weapon Mount Frame
        if (_weaponMounts.Count > 0 && _weaponMounts[0] != null) {
            Image tmp = _weaponMounts[0].GetComponent<Image>();
            if (tmp) _weaponMountColor = tmp.color;
        }

        // Cache the normal colors of all tab groups
        for (int i = 0; i < _tabGroups.Count; i++) {
            // Fetch the current Tab Group
            InventoryUI_TabGroup tabGroup = _tabGroups[i];

            // Remember the normal color for the Tab Text
            if (tabGroup.Items.Count > 0 &&
                tabGroup.Items[0].TabText != null) {
                tabGroup.TextColor = tabGroup.Items[0].TabText.color;
            }
        }

        // Cache the UI references for the Image and Text for each backpack mount
        for (int i = 0; i < _backpackMounts.Count; i++) {
            // All empty slots to begin with
            _backpackMountImages.Add(null);
            _backpackMountText.Add(null);

            // Cache the image and text slot references
            // A slot should have exactly one image game object and one text game object
            if (_backpackMounts[i] != null) {
                Transform parent = _backpackMounts[i].transform;
                Transform tmp;
                tmp = parent.Find("Image");
                if (tmp) _backpackMountImages[i] = tmp.GetComponent<Image>();

                tmp = parent.Find("Text");
                if (tmp) _backpackMountText[i] = tmp.GetComponent<Text>();
            }
        }

        // Cache the UI references for the UI children of each ammo mount
        for (int i = 0; i < _ammoMounts.Count; i++) {
            _ammoMountImages.Add(null);
            _ammoMountEmptyText.Add(null);
            _ammoMountRoundsText.Add(null);

            if (_ammoMounts[i] != null) {
                Transform parent = _ammoMounts[i].transform;
                Transform tmp;
                tmp = parent.Find("Image");
                if (tmp) _ammoMountImages[i] = tmp.GetComponent<Image>();
                tmp = parent.Find("Empty");
                if (tmp) _ammoMountEmptyText[i] = tmp.GetComponent<Text>();
                tmp = parent.Find("Rounds");
                if (tmp) _ammoMountRoundsText[i] = tmp.GetComponent<Text>();
            }
        }

        // Cache all weapon mount referenes
        for (int i = 0; i < _weaponMounts.Count; i++) {
            // Create entry in list
            _weaponMountNames.Add(null);
            _weaponMountImages.Add(null);
            _weaponMountAmmoInfo.Add(null);
            _weaponMountReloadType.Add(null);
            _weaponMountRounds.Add(null);
            _weaponMountSliders.Add(null);

            // Next we need to hook up all the references of the weapon mounts
            if (_weaponMounts[i] != null) {
                Transform trans = _weaponMounts[i].transform;
                Transform tmp;

                // Find child objects
                tmp = trans.Find("Image");
                if (tmp) {
                    _weaponMountImages[i] = tmp.GetComponent<Image>();
                }

                tmp = trans.Find("Name");
                if (tmp) {
                    _weaponMountNames[i] = tmp.GetComponent<Text>();
                }

                tmp = trans.Find("Slider");
                if (tmp) {
                    _weaponMountSliders[i] = tmp.GetComponent<Slider>();
                }

                tmp = trans.Find("Ammo Info");
                if (tmp) _weaponMountAmmoInfo[i] = tmp.gameObject;
                tmp = trans.Find("Ammo Info/Rounds");
                if (tmp) _weaponMountRounds[i] = tmp.GetComponent<Text>();
                tmp = trans.Find("Ammo Info/Reload Type");
                if (tmp) _weaponMountReloadType[i] = tmp.GetComponent<Text>();
            }
        }

        // Set the initial Tab selection of our Tab Groups
        SelectTabGroup(0, 0);
        SelectTabGroup(1, 0);

        // Set all scrolling content to its top-most position
        StartCoroutine(ResetScrollViews());
    }

    // -------------------------------------------------------------------------------
    // Name :   ResetScrollViews (Coroutine)
    // Desc :   Reset all scroll views to their topmost position.
    // -------------------------------------------------------------------------------
    protected IEnumerator ResetScrollViews() {
        yield return null;
        if (_backpackScrollRect)
            _backpackScrollRect.verticalNormalizedPosition = 1.0f;

        if (_generalDescriptionLayout.ScrollView)
            _generalDescriptionLayout.ScrollView.verticalNormalizedPosition = 1.0f;

        if (_weaponDescriptionLayout.ScrollView)
            _weaponDescriptionLayout.ScrollView.verticalNormalizedPosition = 1.0f;
    }

    // -------------------------------------------------------------------------------
    // Name : SelectTab
    // Desc : Called to select a Tab in a Tab group. This will set the correct
    //        colours for the Tabs based on which one is selected and also
    //        enable the LayoutContainer for the currently active tab.
    // -------------------------------------------------------------------------------
    public void SelectTabGroup(int tabGroupIndex, int panel) {
        // Bogus group index so return
        if (tabGroupIndex < 0 || tabGroupIndex >= _tabGroups.Count) return;

        // Fetch the current tab group we wish to process
        InventoryUI_TabGroup tabGroup = _tabGroups[tabGroupIndex];

        tabGroup.ActiveItem = panel;

        // Iterate through all the tabs in the tab group
        for (int i = 0; i < tabGroup.Items.Count; i++) {
            // If this is the selected tab then enable it's Layout
            // and set it tab' text color to the active color.
            if (i == tabGroup.ActiveItem) {
                if (tabGroup.Items[i].LayoutContainer)
                    tabGroup.Items[i].LayoutContainer.SetActive(true);

                if (tabGroup.Items[i].TabText)
                    tabGroup.Items[i].TabText.color = tabGroup.TextColor;
            }
            else {
                // Not activate tab so disable layout and set tab's text color
                // to the inactive color.
                if (tabGroup.Items[i].LayoutContainer)
                    tabGroup.Items[i].LayoutContainer.SetActive(false);

                if (tabGroup.Items[i].TabText)
                    tabGroup.Items[i].TabText.color = tabGroup.TextInactive;
            }
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   DisplayWeaponDescription
    // Desc :   Enable the Weapon Description layout and Disable the General Description Layout
    //          and Configure Weapon Description UI elements to reflect the details of the
    //          currently selected weapon.
    // --------------------------------------------------------------------------------------------
    protected void DisplayWeaponDescription(InventoryItem item) {
        if (item == null) {
            HideDescription();
            return;
        }

        // Disable Non-Weapon Layout
        if (_generalDescriptionLayout.LayoutContainer != null)
            _generalDescriptionLayout.LayoutContainer.SetActive(false);

        // Enable Weapons Layout
        if (_weaponDescriptionLayout.LayoutContainer != null)
            _weaponDescriptionLayout.LayoutContainer.SetActive(true);

        // Set Sprite, Title and Description
        if (_weaponDescriptionLayout.Image != null)
            _weaponDescriptionLayout.Image.sprite = item.inventoryImage;

        if (_weaponDescriptionLayout.Title != null)
            _weaponDescriptionLayout.Title.text = item.inventoryName;

        if (_weaponDescriptionLayout.Description != null)
            _weaponDescriptionLayout.Description.text = item.inventoryDescription;


        // Enable Action Buttons
        if (_actionButton1.GameObject != null) {
            if (item.inventoryAction != InventoryAction.None) {
                _actionButton1.GameObject.SetActive(true);
                if (_actionButton1.ButtonText)
                    _actionButton1.ButtonText.text = item.inventoryActionText;
            }
            else {
                _actionButton1.GameObject.SetActive(false);
            }
        }

        if (_actionButton2.GameObject != null) {
            _actionButton2.GameObject.SetActive(true);
            if (_actionButton2.ButtonText)
                _actionButton2.ButtonText.text = "Drop";
        }

        // Scroll the ScrollRect to the top
        if (_weaponDescriptionLayout.ScrollView != null)
            _weaponDescriptionLayout.ScrollView.verticalNormalizedPosition = 1.0f;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   DisplayGeneralDescription
    // Desc :   Enable the General Description layout and Disable the Weapon Description Layout
    //          and Configure General Description UI elements to reflect the details of the
    //          currently selected Item.
    // --------------------------------------------------------------------------------------------
    protected void DisplayGeneralDescription(InventoryItem item) {
        if (item == null) {
            HideDescription();
            return;
        }

        // Enable Non-Weapon Layout
        if (_generalDescriptionLayout.LayoutContainer != null)
            _generalDescriptionLayout.LayoutContainer.SetActive(true);

        // Disable Weapons Layout
        if (_weaponDescriptionLayout.LayoutContainer != null)
            _weaponDescriptionLayout.LayoutContainer.SetActive(false);

        // Set Sprite, Title and Description
        if (_generalDescriptionLayout.Image != null)
            _generalDescriptionLayout.Image.sprite = item.inventoryImage;

        if (_generalDescriptionLayout.Title != null)
            _generalDescriptionLayout.Title.text = item.inventoryName;

        if (_generalDescriptionLayout.Description != null)
            _generalDescriptionLayout.Description.text = item.inventoryDescription;


        // Enable Action Buttons
        if (_actionButton1.GameObject != null) {
            if (item.inventoryAction != InventoryAction.None) {
                _actionButton1.GameObject.SetActive(true);

                if (_actionButton1.ButtonText)
                    _actionButton1.ButtonText.text = item.inventoryActionText;
            }
            else {
                _actionButton1.GameObject.SetActive(false);
            }
        }

        if (_actionButton2.GameObject != null) {
            _actionButton2.GameObject.SetActive(true);
            if (_actionButton2.ButtonText)
                _actionButton2.ButtonText.text = "Drop";
        }

        // Scroll the ScrollRect to the top
        if (_generalDescriptionLayout.ScrollView != null)
            _generalDescriptionLayout.ScrollView.verticalNormalizedPosition = 1.0f;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   HideDescription
    // Desc :   Hide all description layouts
    // --------------------------------------------------------------------------------------------
    protected void HideDescription() {
        // Disable Non-Weapon Layout
        if (_generalDescriptionLayout.LayoutContainer != null)
            _generalDescriptionLayout.LayoutContainer.SetActive(false);

        // Disable Weapons Layout
        if (_weaponDescriptionLayout.LayoutContainer != null)
            _weaponDescriptionLayout.LayoutContainer.SetActive(false);

        // Disable Action Buttons
        if (_actionButton1.GameObject != null)
            _actionButton1.GameObject.SetActive(false);

        if (_actionButton2.GameObject != null)
            _actionButton2.GameObject.SetActive(false);
    }

    // -------------------------------------------------------------------------------------------
    // Name	: OnEnterBackpackMount
    // Desc	: Called when mouse enters a Backpack Mount's Frame
    // -------------------------------------------------------------------------------------------
    public void OnEnterBackpackMount(Image image) {
        int mount;

        // Get the slot index from the name of the object passed
        if (image == null || !int.TryParse(image.name, out mount)) {
            Debug.Log("OnEnterBackpackMount Error! Could not parse image name as INT");
            return;
        }

        // Valid Index?
        if (mount >= 0 && mount < _backpackMounts.Count) {
            // Get Inventory Item
            InventoryBackpackMountInfo itemMount = _inventory.GetBackpack(mount);

            // No highlight if nothing at this slot
            if (itemMount == null || itemMount.Item == null) return;

            // Set the color of the frame of this slot to the hover color
            if (_selectedPanelType != InventoryPanelType.Backpack || _selectedMount != mount)
                image.color = _backpackMountHover;

            // If the selected panel is not none then something else is selected at the
            // moment so don't update the info pane
            if (_selectedPanelType != InventoryPanelType.None) return;

            // Update Description Window
            DisplayGeneralDescription(itemMount.Item);
        }
    }

    // ------------------------------------------------------------------------------
    // Name	: OnExitBackpackMount
    // Desc	: Called when mouse exits a Backpack Mount's frame
    // ------------------------------------------------------------------------------
    public void OnExitBackpackMount(Image image) {
        if (image != null)
            image.color = _backpackMountColor;

        if (_selectedPanelType != InventoryPanelType.None) return;

        // Hide Description Window
        HideDescription();
    }

    // --------------------------------------------------------------------------------
    // Name	:	OnPointerClickBackpackSlot
    // Desc	:	Called when user clicks on a Backpack Mount's frame
    // --------------------------------------------------------------------------------
    public void OnClickBackpackMount(Image image) {
        // Get mountfrom name
        int mount;
        if (image == null || !int.TryParse(image.name, out mount)) {
            Debug.Log("OnClickBackpackError : Could not parse image name as INT");
            return;
        }

        // Is this a valid mount that's been clicked
        if (mount >= 0 && mount < _backpackMounts.Count) {
            // Get Inventory Item
            InventoryBackpackMountInfo itemMount = _inventory.GetBackpack(mount);

            // No highlight if nothing at this slot
            if (itemMount == null || itemMount.Item == null) return;

            // We are clicking on the selected item so unselect
            if (mount == _selectedMount && _selectedPanelType == InventoryPanelType.Backpack) {
                Invalidate();
                image.color = _backpackMountHover;
                image.fillCenter = false;
                DisplayGeneralDescription(itemMount.Item);
            }
            else {
                Invalidate();
                _selectedPanelType = InventoryPanelType.Backpack;
                _selectedMount = mount;
                image.color = _backpackMountColor;
                image.fillCenter = true;
                DisplayGeneralDescription(itemMount.Item);
            }
        }
    }

    // -------------------------------------------------------------------------------------------
    // Name	: OnEnterAmmoMount
    // Desc	: Called when the mouse enters an Ammo Mount's frame
    // -------------------------------------------------------------------------------------------
    public void OnEnterAmmoMount(Image image) {
        int mount;
        if (image == null || !int.TryParse(image.name, out mount)) {
            Debug.Log("OnEnterAmmoMount Error! Could not parse image name as int");
            return;
        }

        // Valid slot index?
        if (mount >= 0 && mount < _ammoMounts.Count) {
            // Get Inventory Item
            InventoryAmmoMountInfo itemMount = _inventory.GetAmmo(mount);

            // No highlight if nothing at this slot
            if (itemMount == null || itemMount.Ammo == null) return;

            // Set the hover color of the frame
            if (_selectedPanelType != InventoryPanelType.AmmoBelt || _selectedMount != mount)
                image.color = _ammoMountHover;

            // If something is selected then return without updating the Info pane
            if (_selectedPanelType != InventoryPanelType.None) return;

            // Update Description Window
            DisplayGeneralDescription(itemMount.Ammo);
        }
    }

    // ----------------------------------------------------------------------------
    // Name	:	OnClickAmmoMount
    // Desc	:	Called when the mouse clicks an Ammo Mount's frame
    // ----------------------------------------------------------------------------
    public void OnClickAmmoMount(Image image) {
        int mount;
        if (image == null || !int.TryParse(image.name, out mount)) {
            Debug.Log("OnEnterAmmoMount Error! Could not parse image name as int");
            return;
        }

        // Is this a valid mount that's been clicked
        if (mount >= 0 && mount < _ammoMounts.Count) {
            // Get Inventory Item
            InventoryAmmoMountInfo itemMount = _inventory.GetAmmo(mount);

            // No highlight if nothing at this slot
            if (itemMount == null || itemMount.Ammo == null) return;

            // We are clicking on the selected item so unselect
            if (mount == _selectedMount && _selectedPanelType == InventoryPanelType.AmmoBelt) {
                Invalidate();
                image.color = _ammoMountHover;
                image.fillCenter = false;
                DisplayGeneralDescription(itemMount.Ammo);
            }
            else {
                Invalidate();
                _selectedPanelType = InventoryPanelType.AmmoBelt;
                _selectedMount = mount;
                image.color = _ammoMountColor;
                image.fillCenter = true;
                DisplayGeneralDescription(itemMount.Ammo);
            }
        }
    }

    // ------------------------------------------------------------------------------
    // Name	: OnExitAmmoMount
    // Desc	: Called when the mouse exits an Ammo Mount's frame
    // ------------------------------------------------------------------------------
    public void OnExitAmmoMount(Image image) {
        // Reset the frame image to its starting color (non selected/hovered)
        if (image != null) {
            image.color = _ammoMountColor;
            if (_selectedPanelType != InventoryPanelType.None) return;
        }

        // Hide Description Panel
        HideDescription();
    }

    // -------------------------------------------------------------------------------------------
    // Name	: OnEnterWeaponMount
    // Desc	: Called when mouse enters a Weapon Mount's frame
    // -------------------------------------------------------------------------------------------
    public void OnEnterWeaponMount(Image image) {
        int mount;
        if (image == null || !int.TryParse(image.name, out mount)) {
            Debug.Log("OnEnterWeaponMount Error! Could not parse image name as int");
            return;
        }

        // Valid mount index?
        if (mount >= 0 && mount < _weaponMounts.Count) {
            // Get Inventory Item
            InventoryWeaponMountInfo itemMount = _inventory.GetWeapon(mount);

            // No highlight if nothing at this slot
            if (itemMount == null || itemMount.Weapon == null) return;

            // Set the hover color of the frame
            if (_selectedPanelType != InventoryPanelType.Weapons || _selectedMount != mount)
                image.color = _weaponMountHover;

            // If something is selected then return without updating the Info pane
            if (_selectedPanelType != InventoryPanelType.None) return;

            // Display Description Window
            DisplayWeaponDescription(itemMount.Weapon);
        }
    }

    // ----------------------------------------------------------------------------
    // Name	:	OnClickWeaponMount
    // Desc	:	Called when mouse clicks on a Weapon Mount's frame
    // ----------------------------------------------------------------------------
    public void OnClickWeaponMount(Image image) {
        int mount;
        if (image == null || !int.TryParse(image.name, out mount)) {
            Debug.Log("OnEnterWeaponMount Error! Could not parse image name as int");
            return;
        }

        // Is this a valid mount that's been clicked
        if (mount >= 0 && mount < _ammoMounts.Count) {
            // Get Inventory Item
            InventoryWeaponMountInfo itemMount = _inventory.GetWeapon(mount);

            // No highlight if nothing at this slot
            if (itemMount == null || itemMount.Weapon == null) return;

            // We are clicking on the selected item so unselect
            if (mount == _selectedMount && _selectedPanelType == InventoryPanelType.Weapons) {
                Invalidate();
                image.color = _weaponMountHover;
                image.fillCenter = false;
                DisplayWeaponDescription(itemMount.Weapon);
            }
            else {
                Invalidate();
                _selectedPanelType = InventoryPanelType.Weapons;
                _selectedMount = mount;
                image.color = _weaponMountColor;
                image.fillCenter = true;
                DisplayWeaponDescription(itemMount.Weapon);
            }
        }
    }

    // ------------------------------------------------------------------------------
    // Name	: OnExitWeaponMount
    // Desc	: Called when mouse exits a Weapon Mounts Frame
    // ------------------------------------------------------------------------------
    public void OnExitWeaponMount(Image image) {
        // Reset the frame image to its starting color (non selected/hovered)
        if (image != null) {
            image.color = _weaponMountColor;
            if (_selectedPanelType != InventoryPanelType.None) return;
        }

        // Hide Description Window
        HideDescription();
    }


    // --------------------------------------------------------------------------------------------
    // Name :   OnEnterTabZero
    // Desc :   Called when mouse Enters a Tab in the TabGroup
    // --------------------------------------------------------------------------------------------
    public void OnEnterTabZero(int index) {
        InventoryUI_TabGroup tabGroup = _tabGroups[0];

        if (index >= 0 && index < tabGroup.Items.Count) {
            if (tabGroup.Items[index].TabText != null) {
                tabGroup.Items[index].TabText.color =
                    (tabGroup.ActiveItem != index) ? tabGroup.TextHover : tabGroup.TextColor;
            }
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   OnExitTabZero
    // Desc :   Called when mouse Exits a Tab in the TabGroup
    // --------------------------------------------------------------------------------------------
    public void OnExitTabZero(int index) {
        InventoryUI_TabGroup tabGroup = _tabGroups[0];

        if (index >= 0 && index < tabGroup.Items.Count) {
            if (tabGroup.Items[index].TabText != null) {
                tabGroup.Items[index].TabText.color =
                    (tabGroup.ActiveItem == index) ? tabGroup.TextColor : tabGroup.TextInactive;
            }
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   OnClickTabZero
    // Desc :   Called when mouse Clicks a Tab in the TabGroup
    // --------------------------------------------------------------------------------------------
    public void OnClickTabZero(int index) {
        InventoryUI_TabGroup tabGroup = _tabGroups[0];

        if (index >= 0 && index < tabGroup.Items.Count) {
            SelectTabGroup(0, index);
        }
    }


    // --------------------------------------------------------------------------------------------
    // Name :   OnEnterTabOne
    // Desc :   Called when mouse Enters a Tab in the TabGroup
    // --------------------------------------------------------------------------------------------
    public void OnEnterTabOne(int index) {
        InventoryUI_TabGroup tabGroup = _tabGroups[1];

        if (index >= 0 && index < tabGroup.Items.Count) {
            if (tabGroup.Items[index].TabText != null) {
                tabGroup.Items[index].TabText.color =
                    (tabGroup.ActiveItem != index) ? tabGroup.TextHover : tabGroup.TextColor;
            }
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   OnExitTabOne
    // Desc :   Called when mouse Exits a Tab in the TabGroup
    // --------------------------------------------------------------------------------------------
    public void OnExitTabOne(int index) {
        InventoryUI_TabGroup tabGroup = _tabGroups[1];

        if (index >= 0 && index < tabGroup.Items.Count) {
            if (tabGroup.Items[index].TabText != null) {
                tabGroup.Items[index].TabText.color =
                    (tabGroup.ActiveItem == index) ? tabGroup.TextColor : tabGroup.TextInactive;
            }
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   OnClickTabOne
    // Desc :   Called when mouse Clicks a Tab in the TabGroup
    // --------------------------------------------------------------------------------------------
    public void OnClickTabOne(int index) {
        InventoryUI_TabGroup tabGroup = _tabGroups[1];

        if (index >= 0 && index < tabGroup.Items.Count) {
            SelectTabGroup(1, index);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Name :   OnActionButton1
    // Desc :   Called when ActionButton1 is clicked in the UI
    // ---------------------------------------------------------------------------------------------
    public void OnActionButton1() {
        if (!_inventory) return;

        // Ammo belt items do not have actions in my Inventory Implementation.
        // They are Consumed by Weapons during reload.
        switch (_selectedPanelType) {
            case InventoryPanelType.Backpack:
                _inventory.UseBackpackItem(_selectedMount);
                break;
            case InventoryPanelType.Weapons:
                _inventory.ReloadWeapon(_selectedMount);
                break;
        }

        // Repaint Inventory
        Invalidate();
    }

    // ---------------------------------------------------------------------------------------------
    // Name :   OnActionButton2
    // Desc :   Called when ActionButton2 is clicked in the UI
    // ---------------------------------------------------------------------------------------------
    public void OnActionButton2() {
        // No Inventory so bail
        if (_inventory == null) return;

        // Which panel is selected as this depends what the buttons do and say
        switch (_selectedPanelType) {
            case InventoryPanelType.Backpack:
                _inventory.DropBackpackItem(_selectedMount);
                break;
            case InventoryPanelType.Weapons:
                _inventory.DropWeaponItem(_selectedMount);
                break;
            case InventoryPanelType.AmmoBelt:
                _inventory.DropAmmoItem(_selectedMount);
                break;
        }

        // Repaint Inventory to reflect changes
        Invalidate();
    }

    // --------------------------------------------------------------------------------------------
    // Name :   OnAutoPlayOnPickup
    // Desc :   Called by UI System when toggle is clicked. Updates the internal state of the
    //          Inventory
    // --------------------------------------------------------------------------------------------
    public void OnAutoplayOnPickup() {
        if (_pDAReferences._autoplayOnPickup && _inventory)
            _inventory.autoPlayOnPickup = _pDAReferences._autoplayOnPickup.isOn;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   OnBeginAudio
    // Desc :   This is the listener function invoked by the Inventory Audio Player. It configures
    //          the PDA UI elements for beginning audio playback.
    // --------------------------------------------------------------------------------------------
    public void OnBeginAudio(InventoryItemAudio audio) {
        // The function must be passed a reference to the audio item being played
        if (!audio) return;

        // Set the UI elements for the audio item, Image, Author, Subject etc.
        if (_pDAReferences._pDAImage) {
            _pDAReferences._pDAImage.texture = audio.image;
            _pDAReferences._pDAImage.color = Color.white;
        }

        if (_pDAReferences._pDASubject)
            _pDAReferences._pDASubject.text = audio.subject;

        if (_pDAReferences._pDAAuthor)
            _pDAReferences._pDAAuthor.text = audio.person;

        // Clear the transcript text UI element initially
        if (_pDAReferences._transcriptText)
            _pDAReferences._transcriptText.text = null;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   OnUpdateAudio
    // Desc :   This is the listener function called by the Inventory Audio Player each frame
    //          that the audio is playing. The function is passed the normalized time within
    //          the audio clips timeline.
    // --------------------------------------------------------------------------------------------
    public void OnUpdateAudio(float time) {
        // Update UI Timeline slider
        if (_pDAReferences._timelineSlider)
            _pDAReferences._timelineSlider.value = time;

        // Update transcript UI text
        if (_pDAReferences._transcriptText && _transcriptText)
            _pDAReferences._transcriptText.text = _transcriptText.value;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   OnEndAudio
    // Desc :   Listener function called by Inventory Audio Player when audio has finished.
    // --------------------------------------------------------------------------------------------
    public void OnEndAudio() {
        // Set timeline UI slider back to zero
        if (_pDAReferences._timelineSlider)
            _pDAReferences._timelineSlider.value = 0.0f;

        // Clear PDA Panel's Image, Author and Subject
        if (_pDAReferences._pDAImage) {
            _pDAReferences._pDAImage.texture = null;
            _pDAReferences._pDAImage.color = Color.black;
        }

        if (_pDAReferences._pDASubject)
            _pDAReferences._pDASubject.text = null;

        if (_pDAReferences._pDAAuthor)
            _pDAReferences._pDAAuthor.text = null;

        // Clear transcript UI element
        if (_pDAReferences._transcriptText)
            _pDAReferences._transcriptText.text = null;

        // Defer PDA Entry List refresh until next frame.
        if (gameObject.activeInHierarchy)
            StartCoroutine(LateOnEndAudio());
    }

    // -------------------------------------------------------------------------------------------
    // Name :   LateOnEndAudio
    // Desc :   Coroutine used to defer refreshing of the PDA entries list until the next frame
    // -------------------------------------------------------------------------------------------
    protected IEnumerator LateOnEndAudio() {
        yield return null;

        RefreshPDAEntries();
    }

    // --------------------------------------------------------------------------------------------
    // Name :   Update
    // Desc :   I do not like this very much :)
    //          Would be niced to have our TimeStringQueue invoke an OnChange event and then
    //          objects could subscribe to it.
    // --------------------------------------------------------------------------------------------
    protected void Update() {
        if (_pDAReferences._notificationText && _notificationQueue)
            _pDAReferences._notificationText.text = _notificationQueue.text;
    }
}