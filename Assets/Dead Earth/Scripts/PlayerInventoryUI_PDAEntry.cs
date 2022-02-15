using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ------------------------------------------------------------------------------------------------
// CLASS    :   PlayerInventoryUI_PDAEntry
// DESC     :   This script is added to the prefab of the PDA Recording Entry Text prefab in
//              the player inventory UI. Handles the setting of the text elements and the
//              mouse event handling
// ------------------------------------------------------------------------------------------------
public class PlayerInventoryUI_PDAEntry : MonoBehaviour
{
    // Inspector Assigned
    [SerializeField] protected Text _name           = null;
    [SerializeField] protected Text _subject        = null;
    [SerializeField] protected Color _normalColor   = Color.cyan;
    [SerializeField] protected Color _hoverColor    = Color.yellow;
    [SerializeField] protected Color _activeColor   = Color.red;

    // Internals
    protected PlayerInventoryUI     _inventoryUI        = null;
    protected InventoryItemAudio    _inventoryItemAudio = null;
    protected int                   _index              = -1;

    // Helper Propetry
    // Grabs the InventoryUI from the parent hierarchy if not done so already
    PlayerInventoryUI inventoryUI
    {
        get
        {
            if (!_inventoryUI)
                _inventoryUI = GetComponentInParent<PlayerInventoryUI>();

            return _inventoryUI;
        }
    }

    // --------------------------------------------------------------------
    // Name :   IsActive
    // Desc :   Returns true if this audio recording entry is 
    //          currently playing.
    // --------------------------------------------------------------------
    bool IsActive()
    {
        if (!inventoryUI || !inventoryUI.inventory || !_inventoryItemAudio) return false;
        return inventoryUI.inventory.GetActiveAudioRecording() == _index ? true : false;
    }

    // --------------------------------------------------------------------
    // Name	:	SetData
    // Desc	:	Called by the InventoryUI to configure the entry with
    //          with its text data
    // --------------------------------------------------------------------
    public void SetData(InventoryItemAudio itemAudio, int index)
    {
        // Store Audio Item
        _inventoryItemAudio = itemAudio;

        // Store the index of this item in the list
        _index = index;

        // Is this the sound that is currently playing
        bool isActive = IsActive();

        // Set the actual Text
        if (_inventoryItemAudio)
        {
            if (_name)      _name.text      = _inventoryItemAudio.person;
            if (_subject)   _subject.text   = _inventoryItemAudio.subject;
        }
        else
        {
            if (_name)      _name.text      = null;
            if (_subject)   _subject.text   = null;
        }

        // Set Text Colors
        if (_name)      _name.color     = isActive ? _activeColor : _normalColor;
        if (_subject)   _subject.color  = isActive ? _activeColor : _normalColor;
    }

    // --------------------------------------------------------------------
    // Name :   OnPointerEnter
    // Desc :   Called by Event System when mouse moves over the entry
    // --------------------------------------------------------------------
    public void OnPointerEnter()
    {
        if (IsActive()) return;
        if (_name)      _name.color     = _hoverColor;
        if (_subject)   _subject.color  = _hoverColor;

    }

    // --------------------------------------------------------------------
    // Name :   OnPointerExit
    // Desc :   Called by Event System when mouse exits the entry
    // --------------------------------------------------------------------
    public void OnPointerExit()
    {
        if (IsActive()) return;
        if (_name)      _name.color     = _normalColor;
        if (_subject)   _subject.color  = _normalColor;
    }

    // --------------------------------------------------------------------
    // Name :   OnPointerClick
    // Desc :   Called by Event System when mouse Clicks on Entry
    // --------------------------------------------------------------------
    public void OnPointerClick()
    {
        if (!_inventoryItemAudio) return;
        
        // Tell the inventory to play the audio recording at the
        // corresponding index
        if (inventoryUI && inventoryUI.inventory)
        {
            inventoryUI.inventory.PlayAudioRecording(_index);
        }

        // Tell the parent UI to repaint all entries which is essentially done
        // to deselect the previously selected audio
        inventoryUI.RefreshPDAEntries();

        inventoryUI.SelectTabGroup(1, 1);

    } 
}
