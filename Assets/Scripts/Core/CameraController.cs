using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour, IGameStateListener<InGameState>
{
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    public CinemachineCamera CinemachineCamera => _cinemachineCamera;

    [SerializeField] private Camera _uiCamera;
    public Camera UICamera => _uiCamera;

    private Camera _mainCamera;
    public Camera MainCamera => _mainCamera;

    private CameraContentFitter _contentFitter;

    public float CameraWidth => CameraHeight * CameraAspect;
    public float CameraHeight => _mainCamera.orthographicSize * 2f;
    public float CameraAspect => (float)Screen.width / (float)Screen.height;

    private CinemachineImpulseSource _impulseSource;

    private void OnDestroy()
    {

    }

    public void Initialize(Transform target)
    {
        _mainCamera = GetComponent<Camera>();

        _contentFitter = GetComponent<CameraContentFitter>();
        _contentFitter.Initialize(this);

        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }
    
    public void OnStateChanged(InGameState prevState, InGameState currState)
    {
        SetAllPriority(10);
    }

    private void SetAllPriority(int priority)
    {
        
    }

    public void SetOrthographicSize(float size)
    {
        var lens = _cinemachineCamera.Lens;
        lens.OrthographicSize = size;
        _cinemachineCamera.Lens = lens;
        _uiCamera.orthographicSize = size;
    }
}
