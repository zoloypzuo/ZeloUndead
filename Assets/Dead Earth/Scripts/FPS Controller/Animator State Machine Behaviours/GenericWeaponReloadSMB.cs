using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericWeaponReloadSMB : ArmsBaseSMB
{
    public InventoryWeaponType weaponType = InventoryWeaponType.None;

    // Internals
    protected int _reloadHash = Animator.StringToHash("Reload");

    // --------------------------------------------------------------------------------------------
    // Name :   OnStateExit
    // Desc :   Called on the last frame. Used to apply the reload to the Inventory and
    //          cancel the Reloading status in the Animator
    // --------------------------------------------------------------------------------------------
    override public void OnStateExit(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex) {
        // Instruct the character manager to use its inventory reference to reload the weapon and
        // refresh it's avalable ammo member
        if (CharacterManager)
            CharacterManager.ReloadWeapon_AnimatorCallback(weaponType);

        // Turn off reloading boolean in Animator. Reloading procedure is now over.
        animator.SetBool(_reloadHash, false);
    }
}