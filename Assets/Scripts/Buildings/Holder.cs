using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class Holder : NetworkBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Basket basket;

    [SerializeField] private PlayerDetector climbUpInteractionArea;
    [SerializeField] private PlayerDetector climbDownInteractionArea;

    [SerializeField] private Vector3 climbUpPosition = new Vector3(0f, 1f, -1.5f);
    [SerializeField] private Vector3 climbDownPosition = new Vector3(0f, 10f, -1.5f);
    [SerializeField] private Vector3 RoofPosition = new Vector3(0f, 10.5f, 0f);

    private bool playerAtTop = false;
    private bool playerAtBottom = false;

    void Awake()
    {
        inputManager.OnInteractPerformed += InputManager_OnInteractPerformed;

        climbUpInteractionArea.OnPlayerEnter += ClimbUpInteractionArea_OnPlayerEnter;
        climbUpInteractionArea.OnPlayerExit += ClimbUpInteractionArea_OnPlayerExit;

        climbDownInteractionArea.OnPlayerEnter += ClimbDownInteractionArea_OnPlayerEnter;
        climbDownInteractionArea.OnPlayerExit += ClimbDownInteractionArea_OnPlayerExit;
    }

    private void InputManager_OnInteractPerformed(object sender, System.EventArgs e)
    {
        Player player = Player.LocalPlayer;
        if (player == null) return;

        if (playerAtTop && !player.IsOnRope())
        {
            MovePlayerTo(player, climbDownPosition, player.IsOnRope());
            playerAtTop = false;
            player.transform.eulerAngles = new Vector3(player.transform.eulerAngles.x, 0f, player.transform.eulerAngles.z);
            climbDownInteractionArea.InvokePLayerExit(player);
            Debug.Log($"Player On Climb Down Position! {climbDownPosition}");
        }

        if (playerAtBottom && !player.IsOnRope())
        {
            MovePlayerTo(player, climbUpPosition, player.IsOnRope());
            playerAtBottom = false;
            player.transform.eulerAngles = new Vector3(player.transform.eulerAngles.x, 0f, player.transform.eulerAngles.z);
            climbUpInteractionArea.InvokePLayerExit(player);
            Debug.Log($"Player On Climb Up Position! {climbUpPosition}");
        }
    }

    private void ClimbDownInteractionArea_OnPlayerExit(object sender, Player player)
    {
        if (player != Player.LocalPlayer) return;

        playerAtTop = false;
        Debug.Log("Player Exited Climb Down Area!");
    }

    private void ClimbDownInteractionArea_OnPlayerEnter(object sender, Player player)
    {
        if (player != Player.LocalPlayer) return;

        playerAtTop = true;
        Debug.Log("Player Entered Climb Up Area!");

        if (player.IsOnRope())
        {
            MovePlayerTo(player, RoofPosition, !player.IsOnRope());
            climbDownInteractionArea.InvokePLayerExit(player);
            DepositInventory(player);
            Debug.Log("Player On Roof Top!");
        }
    }

    private void ClimbUpInteractionArea_OnPlayerExit(object sender, Player player)
    {
        if (player != Player.LocalPlayer) return;

        playerAtBottom = false;
        Debug.Log("Player Exited Climb Up Area!");
    }

    private void ClimbUpInteractionArea_OnPlayerEnter(object sender, Player player)
    {
        if (player != Player.LocalPlayer) return;

        playerAtBottom = true;
        Debug.Log("Player Entered Climb Down Area!");

        if (player.IsOnRope())
        {
            MovePlayerTo(player, climbUpPosition, !player.IsOnRope());
            climbUpInteractionArea.InvokePLayerExit(player);
            Debug.Log("Player On CLimb Up Top!");
        }
    }

    private void DepositInventory(Player player)
    {
        InventoryManager inventoryManager = player.GetComponent<InventoryManager>();
        int scoreToAdd = inventoryManager.ExtractInventoryScore();
        RequestAddScoreServerRpc(scoreToAdd);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestAddScoreServerRpc(int scoreToAdd)
    {
        ScoreManager.Instance.AddScore(scoreToAdd);
    }

    private void MovePlayerTo(Player player, Vector3 position, bool isOnRope)
    {
        Vector3 worldPosition = transform.TransformPoint(position);

        CharacterController characterController = player.GetComponent<CharacterController>();

        if (characterController != null)
        {
            player.SetOnRope(isOnRope);
            characterController.enabled = false;
            player.transform.position = worldPosition;
            characterController.enabled = true;
            Debug.Log($"Player Moved To Position {worldPosition}");
        }
    }
}
