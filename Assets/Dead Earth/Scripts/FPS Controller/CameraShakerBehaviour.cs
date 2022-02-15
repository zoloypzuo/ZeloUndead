using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShakerBehaviour : MonoBehaviour
{
    // Inspector Assigned
    [SerializeField] SharedVector3 _cameraShakerOffset = null;
    [SerializeField] float _magnitudeScale = 1.0f;

    // Internals
    Vector3 _localPosition = Vector3.zero;

    // Start is called before the first frame update
    void Start() {
        _localPosition = transform.localPosition;
    }

    // Update is called once per frame
    void Update() {
        if (_cameraShakerOffset)
            transform.localPosition = _localPosition + (_cameraShakerOffset.value * _magnitudeScale);
    }
}