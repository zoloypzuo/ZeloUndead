using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ------------------------------------------------------------------------------------------------
// CLASS    :   AnimatorEventHandler
// DESC     :   This script is added to the FPS arms bone with the Animator component on it so
//              that its functions can be called by Animation Clip Events. This script then
//              forwards that request further up the chain to the Character Manager.
//  -----------------------------------------------------------------------------------------------
public class AnimatorEventHandler : MonoBehaviour
{
    // Reference to the Character Manager that actually handles the events
    protected CharacterManager _characterManager = null;

    // --------------------------------------------------------------------------------------------
    // Name :   Start
    // Desc :   Caches a reference to the Character manager futher up the chain
    // --------------------------------------------------------------------------------------------
    void Start()
    {
        _characterManager = GetComponentInParent<CharacterManager>();
    }

    // --------------------------------------------------------------------------------------------
    // Name : FireWeaponEvent
    // Desc : Function called by Fire animation clip events to handle the fire processing.
    // --------------------------------------------------------------------------------------------
    public void FireWeaponEvent( int direction )
    {
        if (_characterManager)
            _characterManager.DoDamage(direction);
    }
   
}
