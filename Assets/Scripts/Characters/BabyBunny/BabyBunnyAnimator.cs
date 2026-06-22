using System;
using UnityEngine;

public class BabyBunnyAnimator : MonoBehaviour
{
    private const string ON_BALOON_TAKEN = "OnBaloonTaken";

    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem tearParticles;
    [SerializeField] private Player player;


    private void Start()
    {
        player.OnBaloonTaken += Player_OnBaloonTaken;
    }

    private void Awake()
    {
        tearParticles.Stop();
    }

    private void Player_OnBaloonTaken(object sender, EventArgs e)
    {
        animator.SetTrigger(ON_BALOON_TAKEN);
        tearParticles.Play();
    }
}
