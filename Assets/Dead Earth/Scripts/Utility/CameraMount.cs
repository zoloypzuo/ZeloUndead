using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMount : MonoBehaviour
{
    public Transform target;
    public float damp = 1;

    // Start is called before the first frame update
    void Start() {
    }

    // Update is called once per frame
    void LateUpdate() {
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * damp);
        transform.position = Vector3.Slerp(transform.position, target.position, Time.deltaTime * damp);
    }
}