using UnityEngine;

public class CloudScroller : MonoBehaviour
{
    [SerializeField] private float _scrollSpeed = 0.5f;
    [SerializeField] private float _resetPosX;   // 왼쪽 리셋 지점 (화면 밖)
    [SerializeField] private float _startPosX;   // 오른쪽 시작 지점 (화면 밖)

    private Vector3 _initialPosition;

    private void Awake()
    {
        _initialPosition = transform.position;
    }

    public void ResetPosition()
    {
        transform.position = _initialPosition;
    }

    private void Update()
    {
        transform.Translate(Vector3.left * _scrollSpeed * Time.deltaTime);

        if (transform.position.x <= _resetPosX)
        {
            var pos = transform.position;
            pos.x = _startPosX;
            transform.position = pos;
        }
    }
}
