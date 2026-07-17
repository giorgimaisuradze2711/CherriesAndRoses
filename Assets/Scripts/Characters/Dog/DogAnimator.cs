using System;
using UnityEngine;

public class DogAnimator : MonoBehaviour
{
    private const string SAD_START_TRIGGER = "SadStart";
    private const string SAD_START_STATE = "SadStart";
    private const string IS_CHASING = "IsChasing";

    [SerializeField] private Dog dog;
    [SerializeField] private Animator animator;

    private bool _waitingForSadStartToFinish;
    private bool _enteredSadStart;

    private void Start() => dog.OnChaseRequested += OnChaseRequested;
    private void OnDestroy() => dog.OnChaseRequested -= OnChaseRequested;

    private void OnChaseRequested(object sender, EventArgs e)
    {
        animator.SetTrigger(SAD_START_TRIGGER);
        _waitingForSadStartToFinish = true;
        _enteredSadStart = false;
    }

    private void Update()
    {
        if (_waitingForSadStartToFinish)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

            if (!_enteredSadStart && info.IsName(SAD_START_STATE))
                _enteredSadStart = true;

            if (_enteredSadStart && !info.IsName(SAD_START_STATE) && !animator.IsInTransition(0))
            {
                _waitingForSadStartToFinish = false;
                _enteredSadStart = false;
                dog.ExecuteChase();
            }
        }

        animator.SetBool(IS_CHASING, dog.IsChasing || _waitingForSadStartToFinish);
    }
}
