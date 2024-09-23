using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;    // 이동 속도
    public float camSpeed = 5f;
    public float jumpPower = 200f;
    public LayerMask groundLayer;
    float _pitch = 0f;
    float _yaw = 0f;
    public PlayerInput _inputCompo;
    private Rigidbody _rb;           // Rigidbody 컴포넌트
    Transform _camTrm;
    private void Awake()
    {
        _camTrm = transform.Find("Main Camera");
        _rb = GetComponent<Rigidbody>();
        _inputCompo.OnAim += Aim;
        _inputCompo.OnJump += Jump;
    }
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    void Aim(Vector2 pos)
    {
        _yaw += camSpeed * pos.x; // 마우스X값을 지속적으로 받을 변수
        _pitch += camSpeed * pos.y; // 마우스y값을 지속적으로 받을 변수

        // Mathf.Clamp(x, 최소값, 최댓값) - x값을 최소,최대값 사이에서만 변하게 해줌
        _pitch = Mathf.Clamp(_pitch, -90f, 90f); // pitch값을 한정시켜줌

        _camTrm.eulerAngles = new Vector3(-_pitch, transform.eulerAngles.y, transform.eulerAngles.z); // 앵글각에 만들어놓은 값을 넣어줌
        transform.eulerAngles = new Vector3(0, _yaw, 0);
    }
    private void Update()
    {
        Move(_inputCompo.Movement);
    }

    void Move(Vector2 input)
    {
        // 현재 Rigidbody의 velocity 값을 가져옴
        Vector3 velocity = _rb.velocity;

        // 현재 객체의 로컬 좌표계에서 XZ 평면에서 움직임을 설정
        Vector3 localMovement = new Vector3(input.x * moveSpeed, 0, input.y * moveSpeed);

        // 로컬 좌표계에서 월드 좌표계로 변환
        Vector3 worldMovement = transform.TransformDirection(localMovement);

        // Y축은 로컬 좌표계로 유지하면서 움직임 적용
        velocity.x = worldMovement.x;
        velocity.z = worldMovement.z;

        // Rigidbody의 velocity에 새로운 값을 설정
        _rb.velocity = velocity;
    }

    void Jump()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(ray, 1.05f, groundLayer))
        {
            _rb.velocity = new Vector3(_rb.velocity.x, jumpPower, _rb.velocity.z);
        }
    }
}
