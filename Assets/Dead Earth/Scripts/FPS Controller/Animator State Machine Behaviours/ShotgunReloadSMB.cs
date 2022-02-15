using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotgunReloadSMB : ArmsBaseSMB
{
    public InventoryWeaponType weaponType = InventoryWeaponType.None;

    // Internals
    protected int _reloadHash = Animator.StringToHash("Reload");
    protected int _reloadRepeatHash = Animator.StringToHash("Reload Repeat");
    protected int _commandStreamHash = Animator.StringToHash("Command Stream");
    protected float _previousCommand = 0.0f;

    // --------------------------------------------------------------------------------------------
    // Name :   OnEnterExit
    // Desc :   Called on the First frame of a repeating loop of roload states.
    //          Decrements the loop count and performs the actual reload was loop count is zero.
    // --------------------------------------------------------------------------------------------
    override public void OnStateEnter(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex) {
        // Decrement Reload Repeat
        int reloadRepeat = animator.GetInteger(_reloadRepeatHash);
        reloadRepeat = Mathf.Max(reloadRepeat - 1, 0);
        animator.SetInteger(_reloadRepeatHash, reloadRepeat);

        // If we have repeated the necessary amount...perform the reload and
        // exit the reload loop
        if (CharacterManager && reloadRepeat == 0) {
            CharacterManager.ReloadWeapon_AnimatorCallback(weaponType);
            animator.SetBool(_reloadHash, false);
        }
    }

    // ----------------------------------------------------------------------------------------------
    // Name :   OnStateUpdate
    // Desc :   Processes any commands in the command stream and dispatches them to the scene handler
    // -----------------------------------------------------------------------------------------------
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex) {
        float command = animator.GetFloat(_commandStreamHash);

        if (CallbackHandler != null && !command.Equals(_previousCommand) && !command.Equals(0.0f)) {
            _previousCommand = command;
            if (command.Equals(1.0f)) CallbackHandler.OnAction("Disable Shotgun Shell");
            else if (command.Equals(2.0f)) CallbackHandler.OnAction("Enable Shotgun Shell");
        }
    }
}