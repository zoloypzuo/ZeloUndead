using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotgunAnimatorStateCallback : WeaponAnimatorStateCallback
{
    [SerializeField] GameObject _shotgunShell = null;

    public override void OnAction(string context, CharacterManager characterManager = null) {
        if (!_shotgunShell) return;

        switch (context) {
            case "Disable Shotgun Shell":
                _shotgunShell.SetActive(false);
                break;
            case "Enable Shotgun Shell":
                _shotgunShell.SetActive(true);
                break;
        }
    }
}