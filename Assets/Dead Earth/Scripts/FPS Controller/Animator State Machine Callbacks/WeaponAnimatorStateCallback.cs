using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ------------------------------------------------------------------------------------------------
// CLASS    :   MuzzleFlashDescriptor
// DESC     :   Describes a single muzzle flash in a sequence of muzzle flashes
// ------------------------------------------------------------------------------------------------
[System.Serializable]
public class MuzzleFlashDescriptor
{
    public GameObject   MuzzleFlash = null;
    public float        LightIntensity = 1.0f;
    public Color        LightColor = Color.white;
    public float        Range = 10.0f;
}

// ------------------------------------------------------------------------------------------------
// Name :   WeaponAnimatorStateCallback
// Desc :   The base weapon callback implementation providing a common interface for activating
//          muzzle flashes.
// ------------------------------------------------------------------------------------------------
public class WeaponAnimatorStateCallback : AnimatorStateCallback
{
    // Inspector Assigned
    public Light                        MuzzleFlashLight;
    public List<MuzzleFlashDescriptor>  MuzzleFlashFrames = new List<MuzzleFlashDescriptor>();
    public float                        MuzzleFlashTime = 0.1f;
    public int                          MuzzleFlashesPerShot     = 1;

    // Private Internals
    protected int _currentMuzzleFlashIndex = 0;
    protected int _lightReferenceCount = 0;

    public  void DoMuzzleFlash()
    {
        if (MuzzleFlashesPerShot < 1) return;

        if (MuzzleFlashesPerShot > 1)
            StartCoroutine(EnableMuzzleFlashSequence());
        else
            EnableMuzzleFlash();
        
    }

    protected void EnableMuzzleFlash()
    {
        // Do we have a valid frame to process for the muzzle data
        if (MuzzleFlashFrames.Count > 0 && MuzzleFlashFrames[_currentMuzzleFlashIndex] != null)
        {
            // Get the next muzzle flash object we wish to activate
            MuzzleFlashDescriptor frame = MuzzleFlashFrames[_currentMuzzleFlashIndex];

            // Activate it
            if (frame.MuzzleFlash != null)
                frame.MuzzleFlash.SetActive(true);

            // Now set the light for this frame
            if (MuzzleFlashLight != null)
            {
                MuzzleFlashLight.color = frame.LightColor;
                MuzzleFlashLight.intensity = frame.LightIntensity;
                MuzzleFlashLight.range = frame.Range;
                MuzzleFlashLight.gameObject.SetActive(true);
            }

            // Set to disable in MuzzleFlashTime
            _lightReferenceCount++;
            StartCoroutine(DisableMuzzleFlash(_currentMuzzleFlashIndex));

            _currentMuzzleFlashIndex++;
            if (_currentMuzzleFlashIndex >= MuzzleFlashFrames.Count)
                _currentMuzzleFlashIndex = 0;
        }

    }

    protected IEnumerator EnableMuzzleFlashSequence()
    {
        int counter = 0;
        float timer = float.MaxValue;

        while (counter < MuzzleFlashesPerShot)
        {
            timer += Time.deltaTime;
            if (timer > MuzzleFlashTime)
            {
                EnableMuzzleFlash();
                counter++;
                timer = 0.0f;
            }
            yield return null;
        }
    }

    protected IEnumerator DisableMuzzleFlash(int index)
    {
        yield return new WaitForSeconds(MuzzleFlashTime);
        MuzzleFlashFrames[index].MuzzleFlash.SetActive(false);
        _lightReferenceCount--;
        if (_lightReferenceCount <= 0 && MuzzleFlashLight)
            MuzzleFlashLight.gameObject.SetActive(false);

    }

    protected virtual void OnEnable()
    {
        _lightReferenceCount = 0;
        _currentMuzzleFlashIndex = 0;
    }

    protected virtual void OnDisable()
    {
        if (MuzzleFlashLight) MuzzleFlashLight.gameObject.SetActive(false);
        for (int i = 0; i < MuzzleFlashFrames.Count; i++)
        {
            if (MuzzleFlashFrames[i].MuzzleFlash)
                MuzzleFlashFrames[i].MuzzleFlash.SetActive(false);
        }
    }
}