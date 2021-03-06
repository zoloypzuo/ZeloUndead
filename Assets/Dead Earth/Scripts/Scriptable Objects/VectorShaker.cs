using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ------------------------------------------------------------------------------------------------
// Class    :   VectorShaker  - Scriptable Object
// Desc     :   Can be used to randomly shake any SharedVector3 over time. Can be used for
//              camera shake or to shake any other arbitrary objects
// Note     :   Requires a CoroutineRunner to be in the scene
// ------------------------------------------------------------------------------------------------
[CreateAssetMenu(menuName = "Scriptable Objects/VectorShaker")]
public class VectorShaker : ScriptableObject
{
    // Inspector Assigned
    // The vector we wish to shake
    [SerializeField] SharedVector3 _shakeVector = null;

    // Internals
    protected IEnumerator _coroutine = null;

    // --------------------------------------------------------------------------------------------
    // Name :   ShakeVector
    // Desc :   Public API used to shake the vector
    // --------------------------------------------------------------------------------------------
    public void ShakeVector(float duration, float magnitude, float damping = 1.0f) {
        if (duration < 0.001 || magnitude.Equals(0.0f)) return;
        if (_shakeVector && CoroutineRunner.Instance) {
            if (_coroutine != null)
                CoroutineRunner.Instance.StopCoroutine(_coroutine);


            _coroutine = Shake(duration, magnitude, damping);
            CoroutineRunner.Instance.StartCoroutine(_coroutine);
        }
    }

    // --------------------------------------------------------------------------------------------
    // Name :   Shake - Coroutine
    // Desc :   Performs the smoothed shake over time
    // --------------------------------------------------------------------------------------------
    protected IEnumerator Shake(float duration, float magnitude, float damping) {
        float time = 0;

        // Loop for duration
        while (time <= duration) {
            // Generate random offsets in x and y
            float x = Random.Range(-1.0f, 1.0f) * magnitude;
            float y = Random.Range(-1.0f, 1.0f) * magnitude;

            // Create a vector from the offsets
            Vector3 unSmoothedVector = new Vector3(x, y, 0.0f);

            // Output vector is generated by lerping from unsmoothed vector to zero over time
            if (_shakeVector)
                _shakeVector.value = Vector3.Lerp(unSmoothedVector, Vector3.zero, (time / duration) * damping);

            // Increase time
            time += Time.deltaTime;

            // Until next frame
            yield return null;
        }

        // Reset shake vector back to zero
        if (_shakeVector)
            _shakeVector.value = Vector3.zero;

        // Coroutine no longer running
        _coroutine = null;
    }
}