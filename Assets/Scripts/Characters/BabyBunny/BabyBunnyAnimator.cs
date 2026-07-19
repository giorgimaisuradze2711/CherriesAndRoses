using System;
using UnityEngine;

public class BabyBunnyAnimator : MonoBehaviour
{
    private const string ON_BALOON_TAKEN = "OnBaloonTaken";

    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem tearParticles;

    private void Start()
    {
        Player.OnAnyBaloonTaken += Player_OnBaloonTaken;
    }

    private void OnDestroy()
    {
        Player.OnAnyBaloonTaken -= Player_OnBaloonTaken;
    }

    private void Awake()
    {
        tearParticles.Stop();
    }

    private void Player_OnBaloonTaken(Player player)
    {
        animator.SetTrigger(ON_BALOON_TAKEN);
        tearParticles.Play();
    }
}
