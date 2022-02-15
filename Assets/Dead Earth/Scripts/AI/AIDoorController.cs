using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDoorController : MonoBehaviour
{/*
    [SerializeField] protected InteractiveDoor  _door;
    [SerializeField] protected Transform        _IKTarget = null;
   

    Collider _trigger = null;
    

    int _openDoorHash               = Animator.StringToHash("Open Door");
    int _comChannelHash             = Animator.StringToHash("ComChannelDoor");
    int _openDoorMirrorHash         = Animator.StringToHash("Open Door Mirror");
    int _handsIKTargetWeightHash    = Animator.StringToHash("HandsIKTargetWeight");
    int _handsIKMirrorLeftHash      = Animator.StringToHash("HandsIKMirrorLeft");

    IEnumerator _coroutine  = null;
    float _timer = 0;
    bool _leftSide = false;
    AIStateMachine _npc = null;*/

    protected void Awake()
    {
       /* _trigger = GetComponent<Collider>();

        if (_door)
            _door._stateChangedEvent.AddListener(OnDoorStateChanged);*/
    }

    protected void OnTriggerEnter(Collider other)
    {
      /*  if (!GameSceneManager.instance || _door==null) return;

        // Does the collider belong to an NPC
        _npc = GameSceneManager.instance.GetAIStateMachine(other.GetInstanceID());
        if (!_npc) return;

        // We have triggered the opening so disable the trigger for now
        if (_trigger)
            _trigger.enabled = false;

        // Trigger the open door animation in the NPC animator
        _npc.animator.SetTrigger(_openDoorHash);
     
        // Start a new coroutine to wait for the correct part in the animation
        _leftSide = _npc.transform.InverseTransformPoint(_IKTarget.position).x < 0;
        _timer = 0;

        // Add listener to the State Machine so we can tap into its IK update
        _npc.AnimatorIKEvent.AddListener(UpdateNPCIK);*/

    }

   /* protected void UpdateNPCIK( AIStateMachine stateMachine, int layer, Animator animator )
    {
    
        AvatarIKGoal ikGoal;   
        _timer += Time.deltaTime;

        // Does this animation need to mirrored (fed by curve)
        bool mirrorNeededForLeft = animator.GetFloat(_handsIKMirrorLeftHash) > 0.99f;

        // Door handle is on left hand side so use the left hand of the NPC
        if (_leftSide)
        {
            // Set left hand as the IK Goal
            ikGoal = AvatarIKGoal.LeftHand;

            // If we need to mirror the animation for the left then set parameter in animator
            if (mirrorNeededForLeft)
                animator.SetBool(_openDoorMirrorHash, true);
            else
                animator.SetBool(_openDoorMirrorHash, false);
        }
        else
        {
            // Set the right hand as the IK goial
            ikGoal = AvatarIKGoal.RightHand;

            // If we don't need to mirror for left then we need to mirror for right
            if (!mirrorNeededForLeft)
                animator.SetBool(_openDoorMirrorHash, true);
            else
                animator.SetBool(_openDoorMirrorHash, false);
        }

        // Set the IK target and weight
         animator.SetIKPosition(ikGoal, _IKTarget.position);
         animator.SetIKPositionWeight(ikGoal, animator.GetFloat(_handsIKTargetWeightHash));

        // Are we at the point in the animation where we wish to
        // activate the door?
        if (animator.GetFloat(_comChannelHash) > 0.99f)
        {
            _door.Activate( _npc.transform.position, true);
            _npc.AnimatorIKEvent.RemoveListener(UpdateNPCIK);
        }
    }*/

   

    protected void OnDoorStateChanged ( bool open )
    {
       /*
        if (!_trigger) return;
        if (open)
            _trigger.enabled = false;
        else
            _trigger.enabled = true;*/
    }
}
