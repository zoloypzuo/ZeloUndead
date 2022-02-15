using UnityEngine;
using System.Collections;

// ------------------------------------------------------------------------------------------------
// Class    :   AnimatorStateCallback
// Desc     :   Describes the interface for all AnimatorStateCallback derived classes. Each has a
//              empty default implementation so you only have to implement the one(s) you need in
//              derived classes
// ------------------------------------------------------------------------------------------------
public class AnimatorStateCallback : MonoBehaviour
{
    public virtual void OnAction(string context, CharacterManager characterManager = null) {
    }

    public virtual void OnAction(int context, CharacterManager characterManager = null) {
    }

    public virtual void OnAction(float context, CharacterManager characterManager = null) {
    }

    public virtual void OnAction(UnityEngine.Object context, CharacterManager characterManager = null) {
    }
}