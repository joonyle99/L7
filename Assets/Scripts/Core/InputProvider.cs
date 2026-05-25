using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputProvider : IDisposable
{
    private const float DRAG_THRESHOLD = 10f;

    private readonly InputAction _pressAction;
    private readonly InputAction _positionAction;

    private bool _isDragStarted;
    private Vector2 _pressStartPos;

    private readonly Camera _camera;

    public InputProvider()
    {
        _camera = Camera.main;

        _pressAction = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
        _positionAction = new InputAction("Position", InputActionType.Value, "<Pointer>/position");

        _pressAction.Enable();
        _positionAction.Enable();
    }

    public Vector2 GetScreenPos => _positionAction?.ReadValue<Vector2>() ?? Vector2.zero;
    public Vector2 GetScreenPosDelta { get; private set; }
    public Vector2 GetWorldPos => ScreenToWorldPos(GetScreenPos);
    public Vector2 GetWorldPosDelta { get; private set; }

    public bool IsDragging { get; private set; }
    public bool JustPressed  { get; private set; }
    public bool JustReleased { get; private set; }

    private Vector2 _prevWorldPos;

    public void Tick()
    {
        var currPos = _positionAction.ReadValue<Vector2>();
        
        JustPressed = _pressAction.WasPressedThisFrame();
        JustReleased = _pressAction.WasReleasedThisFrame();

        if (JustPressed)
        {
            _pressStartPos = currPos;
            _prevWorldPos = ScreenToWorldPos(currPos);
            _isDragStarted = false;
        }

        var isPressed = _pressAction.IsPressed();

        if (isPressed && !_isDragStarted)
        {
            var sqrDistance = (currPos - _pressStartPos).sqrMagnitude;
            _isDragStarted = sqrDistance > DRAG_THRESHOLD * DRAG_THRESHOLD;
        }

        IsDragging = isPressed && _isDragStarted;

        var currWorldPos = ScreenToWorldPos(currPos);
        GetWorldPosDelta = IsDragging ? currWorldPos - _prevWorldPos : Vector2.zero;
        _prevWorldPos = currWorldPos;
    }

    private Vector2 ScreenToWorldPos(Vector2 screenPos) => _camera.ScreenToWorldPoint(screenPos);

    public void Dispose()
    {
        _pressAction.Disable();
        _positionAction.Disable();

        _pressAction.Dispose();
        _positionAction.Dispose();
    }
}
