using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableWeapon : CollectableItem
{
    // Inspector Assigned
    [SerializeField] [Range(0.0f, 100.0f)]  protected float _condition = 100.0f;
    [SerializeField] [Range(0, 100)]        protected int   _rounds = 15;

    // Public Properties
    public float    condition   { get { return _condition; } set { _condition = value; } }
    public int      rounds      { get { return _rounds; }    set { _rounds = value; } }

    // --------------------------------------------------------------------------------------------
    // Name :   GetText (Override)
    // Desc :   Returns the Interactive Text for this weapon
    // --------------------------------------------------------------------------------------------
    public override string GetText()
    {
        // We will need to cast to a Weapon Item
        InventoryItemWeapon weapon = null; ;

        // If an item was assigned
        if (_inventoryItem != null)
            weapon = _inventoryItem as InventoryItemWeapon;

        // If the cast was not successful
        if (weapon == null)
        {
            _interactiveText = "ERROR: No InventoryItemWeapon assigned to " + name;
            return _interactiveText;
        }

        // If text is null this is first call so create text string
        if (_interactiveText == null)
        {
            if (weapon.weaponFeedType == InventoryWeaponFeedType.Ammunition)
                _interactiveText = _inventoryItem.inventoryName + " (Condition: " + _condition + "% - Rounds: " + _rounds + ")" + "\n" + _inventoryItem.pickupText;
            else
            if (weapon.weaponFeedType == InventoryWeaponFeedType.Melee)
                _interactiveText = _inventoryItem.inventoryName + " (Condition: " + _condition + "% )" + "\n" + _inventoryItem.pickupText;
        }

        return _interactiveText;
    }
}
