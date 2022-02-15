using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Scriptable Objects/Shared Variables/Shared Sprite")]
public class SharedSprite : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField] private Sprite _value = null;
    private Sprite _runtimeValue = null;

    public Sprite value {
        get { return this._runtimeValue; }
        set { this._runtimeValue = value; }
    }

    public void OnAfterDeserialize() {
        _runtimeValue = _value;
    }

    public void OnBeforeSerialize() {
    }
}