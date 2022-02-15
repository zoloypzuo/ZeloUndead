using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmsBaseSMB : StateMachineBehaviour
{
    public ScriptableObject Identifier = null;
    [HideInInspector] public CharacterManager CharacterManager = null;
    [HideInInspector] public AnimatorStateCallback CallbackHandler = null;
}