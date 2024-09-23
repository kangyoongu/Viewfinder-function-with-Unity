using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[CreateAssetMenu(menuName = "SO/InputTest")]
public class PlayerInput : ScriptableObject
{
    private Controls _inputAction;
    public Controls InputAction => _inputAction;

    public event Action<Vector2> OnMovement;
    public event Action<Vector2> OnAim;
    public event Action OnJump;
    public event Action ClickEsc;
    public Vector2 Movement { get; private set; }

    private void OnEnable()
    {
        _inputAction = new Controls();

        _inputAction.Enable();
        _inputAction.Control.Mouse.performed += Aim_performed;
        _inputAction.Control.Move.performed += Movement_performed;
        _inputAction.Control.Jump.performed += Jump_performed;
        _inputAction.Control.Move.canceled += Movement_performed;
    }
    private void Movement_performed(InputAction.CallbackContext obj)
    {
        Movement = obj.ReadValue<Vector2>();
    }

    private void Aim_performed(InputAction.CallbackContext obj)
    {
        OnAim?.Invoke(obj.ReadValue<Vector2>());
    }
    private void Jump_performed(InputAction.CallbackContext obj)
    {
        OnJump?.Invoke();
    }
}
