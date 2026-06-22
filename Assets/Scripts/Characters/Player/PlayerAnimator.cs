using System;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private const string IS_WALKING = "IsWalking";
    private const string IS_RUNNING = "IsRunning";
    private const string IS_ON_ROPE = "IsOnRope";
    private const string IS_VICTORY = "IsVictory";
    private const string ON_INTERACT = "OnInteract";
    private const string ON_STUNNED = "OnStunned";
    private const string PICK_DOWN_ANIMATION_NAME = "Pick Down";

    public event EventHandler OnInteractAnimationFinished;

    [SerializeField] private Player player;
    [SerializeField] private Animator animator;

    private bool isInteracting = false;

    private void Start()
    {
        ScoreManager.Instance.OnVictory += ScoreManager_OnVictory;
        player.OnObjectPickUpAnimate += Player_OnObjectPickUpAnimate;
        player.OnStunned += Player_OnStunned;
    }

    private void Player_OnStunned(object sender, EventArgs e)
    {
        Debug.Log("ON STUNNED!");
        animator.SetTrigger(ON_STUNNED);
    }

    private void ScoreManager_OnVictory(object sender, EventArgs e)
    {
        InputManager.Instance.DisablePlayerInputs();
        animator.SetBool(IS_ON_ROPE, false);
        animator.SetBool(IS_WALKING, false);
        animator.SetBool(IS_RUNNING, false);
        animator.SetBool(IS_VICTORY, true);
    }

    private void Player_OnObjectPickUpAnimate(object sender, EventArgs e)
    {
        animator.SetTrigger(ON_INTERACT);
        isInteracting = true;
    }

    void Update()
    {
        if (isInteracting)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName(PICK_DOWN_ANIMATION_NAME))
            {
                if (stateInfo.normalizedTime >= .90f)
                {
                    isInteracting = false;
                    OnInteractAnimationFinished?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        bool isWalking = player.IsWalking();
        bool isRunning = player.IsRunning() && isWalking; // only run if actually moving

        animator.SetBool(IS_ON_ROPE, player.IsOnRope());
        animator.SetBool(IS_WALKING, isWalking && !isRunning); // walk only when not running
        animator.SetBool(IS_RUNNING, isRunning);

        if (player.IsOnRope() && !isWalking)
        {
            animator.speed = 0f;
        }
        else
        {
            animator.speed = 1f;
        }
    }
}