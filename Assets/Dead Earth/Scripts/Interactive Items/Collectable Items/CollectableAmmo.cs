using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableAmmo : CollectableItem
{
    // Inspector Assigned
    [SerializeField] [Range(0, 100)] protected int _capacity = 25;
    [SerializeField] [Range(0, 100)] protected int _rounds = 15;

    // Public Properties
    public int capacity { get { return _capacity; } }
    public int rounds { get { return _rounds; } set { _rounds = value; } }

    // --------------------------------------------------------------------------------------------
    // Name :   GetText (Override)
    // Desc :   Returns the Interactive Text for this Ammo
    // --------------------------------------------------------------------------------------------
    public override string GetText()
    { 
        // If text not generated yet
        if (_interactiveText == null)
        {
            // If the cast was not successful
            if (_inventoryItem == null)
              _interactiveText = "ERROR: No InventoryItem assigned to " + name;
            else   
              _interactiveText = _inventoryItem.inventoryName + " (Rounds: " + _rounds + " / " + _capacity + ")" + "\n" + _inventoryItem.pickupText;
        }

        return _interactiveText;
    }
}
