using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchWeaponSMB : ArmsBaseSMB
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {

        if (CharacterManager != null)
            CharacterManager.DisableWeapon_AnimatorCallback();

    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex)
    {
        if (CharacterManager != null)
            CharacterManager.EnableWeapon_AnimatorCallback();
    }

}

