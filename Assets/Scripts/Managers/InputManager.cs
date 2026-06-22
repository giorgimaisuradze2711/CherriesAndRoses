using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public event EventHandler OnInteractPerformed;

    private InputSystem_Actions inputSystemActions;

    private void Awake()
    {
        Instance = this;
        inputSystemActions = new InputSystem_Actions();
        inputSystemActions.Player.Interact.performed += Interact_performed;
    }

    private void Interact_performed(InputAction.CallbackContext obj)
    {
        OnInteractPerformed?.Invoke(this, EventArgs.Empty);
    }

    public void EnablePlayerInputs()
    {
        inputSystemActions.Player.Enable();
    }

    public void DisablePlayerInputs()
    {
        inputSystemActions.Player.Disable();
    }

    public Vector2 GetInputVectorNormalized()
    {
        Vector2 inputVector = inputSystemActions.Player.Move.ReadValue<Vector2>();
        inputVector = inputVector.normalized;
        return inputVector;
    }

    public bool IsRunHeld()
    {
        return inputSystemActions.Player.Sprint.IsPressed();
    }
}