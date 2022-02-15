using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ------------------------------------------------------------------------------------------------
// Class    :   ActivateFlashlightSMB
// Desc     :   Used on the states that raise and lower the flashlight in the animator.
//              This script listens to the command stream for when to enable or disable
//              the light.
// ------------------------------------------------------------------------------------------------
public class ActivateFlashlightSMB : ArmsBaseSMB
{
    // Inspector Assigned
    public bool Activate = true;
    public FlashlightType FlashlightType = FlashlightType.Primary;

    // Internals
    protected int _commandStream = Animator.StringToHash("Command Stream");
    protected int _flashlightHash = Animator.StringToHash("Flashlight");
    protected bool _done = false;

    // --------------------------------------------------------------------------------------------
    // Name :   OnStateEnter
    // Desc :   Called on the first frame of the state. We use this to reset the _done variable
    //          so exactly 1 and only 1 toggle of the light can be performed in this animation.
    //          We also use this to activate the flashlight mesh (where applicable)
    // --------------------------------------------------------------------------------------------
    override public void OnStateEnter(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex) {
        // Activate the flashlight mesh
        if (CharacterManager)
            CharacterManager.ActivateFlashlightMesh_AnimatorCallback(true, FlashlightType);


        // We haven't processed a command for this state yet
        _done = false;
    }

    // --------------------------------------------------------------------------------------------
    // Name :   OnStateUpdate
    // Desc :   Called every frame of the animation state. We use this to test the command stream
    //          for a command to enable or disable the flashlight.
    //          Only one toggle can happen during the state so you can not have it turned on and off
    //          multiple times while the state is playing. This is a deliberate limitation put in
    //          this code for convenience. Listen to accompany lesson video for more info.
    // --------------------------------------------------------------------------------------------
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex) {
        if (!CharacterManager || _done) return;
        if (!animator.GetBool(_flashlightHash) && Activate) return;
        float commandValue = animator.GetFloat(_commandStream);

        if (commandValue > 0.75f) {
            CharacterManager.ActivateFlashlightLight_AnimatorCallback(Activate, FlashlightType);
            _done = true;
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   OnStateExit
    // Desc :   Called on the last frame. Used to disable the flashlight mesh.
    // --------------------------------------------------------------------------------------------
    override public void OnStateExit(Animator animator, AnimatorStateInfo animStateInfo, int layerIndex) {
        // Disable the mesh and light ONLY if this state was being used for deactivation
        if (CharacterManager && !Activate) {
            CharacterManager.ActivateFlashlightMesh_AnimatorCallback(false, FlashlightType);
            CharacterManager.ActivateFlashlightLight_AnimatorCallback(false, FlashlightType);
        }
    }
}