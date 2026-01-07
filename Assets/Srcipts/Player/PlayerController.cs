using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 7f;
    public float rotationSpeed = 15f;
    
    [Header("Dash Settings")]
    public float dashForce = 20f;      // 대시 힘
    public float dashCooldown = 1.5f;  // 대시 쿨타임
    private float _lastDashTime = -999f; // 마지막 대시 시간 저장

    public bool CanAttack = true;
    public bool CanMove = true;
    public int ComboIndex = 0;
    
    [SerializeField] private ParticleSystem[] SwordTrails;
    [SerializeField] private Transform[] AttackEffectPos;
    
     private Transform SwordTrailTransform;
    private float bufferTimeout = 0.5f;
    private Queue<(InputType type,float time)> inputBuffer =new Queue<(InputType type,float time)>();
    private Rigidbody _rb;
    private Animator _animator;
    private Vector2 _inputVector;
    private Vector3 _moveDirection;
    private bool _isAttackPressed = false;
    // 상태 체크를 위한 변수
    public enum PlayerState { Idle, Run,Attack,Dash}
    private enum InputType{Attack,Dash}
    
    public PlayerState CurrentState = PlayerState.Idle;
    public enum AttackType {Normal,Ice,Fire}

    public AttackType attackType = AttackType.Normal;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _rb.freezeRotation = true;
        _rb.linearDamping = 2f;
    }
    
    private void FixedUpdate()
    {
        MoveCharacter();
        UpdateAnimation();
        
        if (_isAttackPressed)
        {
            // 공격 중이 아니고, 버퍼가 비어있다면 (다음 공격 가능 시점)
            if (CanAttack && inputBuffer.Count == 0)
            {
                AddToBuffer(InputType.Attack); // 버퍼에 공격 추가
            }
        }
        
        if (CanAttack && inputBuffer.Count > 0)
        {
            ProcessNextInput();
        }
    }

    #region Input함수

    public void OnMove(InputValue value)
    {
        _inputVector = value.Get<Vector2>();
    }
    
    public void OnAttack(InputValue value)
    {
        //AddToBuffer(InputType.Attack);
        // Digital 타입은 누를 때 true, 뗄 때 false를 반환합니다.
        _isAttackPressed = value.isPressed;
        // 누른 순간 즉시 첫 공격 실행
        if (_isAttackPressed && CanAttack)
        {
            AddToBuffer(InputType.Attack);
        }
    }

    public void OnDash()
    {
        
        if (Time.time >= _lastDashTime + dashCooldown&&CanMove)
        {

            inputBuffer.Clear();
            ComboIndex = 0;
            ExecuteDash();
        }
    }

    #endregion
    
    private void MoveCharacter()
    {
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = 0; right.y = 0;
        forward.Normalize(); right.Normalize();

        _moveDirection = (forward * _inputVector.y) + (right * _inputVector.x);
        if (!CanMove)
        {
            return;
        }
        if (_inputVector.sqrMagnitude > 0.01f)
        {
            Vector3 targetVelocity = _moveDirection * moveSpeed;
            targetVelocity.y = _rb.linearVelocity.y; 
            _rb.linearVelocity = targetVelocity;

            Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
            _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
        }
    }

    private void UpdateAnimation()
    {
        if(!CanMove) return;
        PlayerState newState;

        // 1. 입력값에 따라 목표 상태 결정
        float inputStrength = _inputVector.sqrMagnitude;
        
        if (inputStrength <= 0.01f)
            newState = PlayerState.Idle;
        else
            newState = PlayerState.Run;

        // 2. 상태가 변했을 때만 트리거 호출
        if (newState != CurrentState)
        {
            CurrentState = newState;

            switch (CurrentState)
            {
                case PlayerState.Idle:
                    _animator.SetTrigger("IDLE"); // 애니메이터에 Idle 트리거 필요
                    break;
                case PlayerState.Run:
                    _animator.SetTrigger("RUN");
                    break;
            }
        }
    }

    
    void AddToBuffer(InputType type)
    {
        inputBuffer.Enqueue((type, Time.time));
    }
    void ProcessNextInput()
    {
        while (inputBuffer.Count > 0)
        {
            var input = inputBuffer.Dequeue();

            // 유효 시간 체크
            if (Time.time - input.time <= bufferTimeout)
            {
                PerformInput(input.type);
                return;
            }
            // 시간이 지난 입력은 그냥 무시(Dequeue 되었으므로 자동 삭제)
        }
    }
    
    void PerformInput(InputType type)
    {
        //_rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
        if (type == InputType.Attack)
        {
            LookAtMouse();
            CurrentState = PlayerState.Attack;
            switch (ComboIndex)
            {
                case 0:
                    _animator.SetTrigger("ATTACK1");
                    break;
                case 1:
                    _animator.SetTrigger("ATTACK2");
                    break;
                case 2:
                    _animator.SetTrigger("ATTACK3");
                    break;
            }

            if (ComboIndex == 2)
            {
                ComboIndex--;
            }
            else
            {
                ComboIndex++;
            }
            
        }
        
    }


    #region 애니메이션 이벤트 함수

    public void CheckCombo()
    {
        CanAttack = true;
    }
    //애니메이션 이벤트함수
    public void stopDash()
    {
        _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
        
    }

    public void Attack1Effect()
    {
        SwordTrailTransform = AttackEffectPos[0];
        
        SwordTrails[(int)attackType].transform.position = SwordTrailTransform.position;
        SwordTrails[(int)attackType].transform.rotation = SwordTrailTransform.rotation;
        SwordTrails[(int)attackType].Play();
    }
    public void Attack2Effect()
    {
        SwordTrailTransform = AttackEffectPos[1];
        
        SwordTrails[(int)attackType].transform.position = SwordTrailTransform.position;
        SwordTrails[(int)attackType].transform.rotation = SwordTrailTransform.rotation;
        SwordTrails[(int)attackType].Play();
    }
    public void Attack3Effect()
    {
        SwordTrailTransform = AttackEffectPos[2];
        
        SwordTrails[(int)attackType].transform.position = SwordTrailTransform.position;
        SwordTrails[(int)attackType].transform.rotation = SwordTrailTransform.rotation;
        SwordTrails[(int)attackType].Play();
    }
    #endregion
    
    
    private void ExecuteDash()
    {
        CanMove = false;
        CanAttack = false;

        _lastDashTime = Time.time;
        _animator.SetTrigger("QUICK SHIFT F");

        Vector3 dashDir = _moveDirection.sqrMagnitude > 0.01f ? _moveDirection : transform.forward;//transform.forward;
        transform.forward = dashDir;
        _rb.linearVelocity = dashDir * dashForce;
        
       
        CurrentState =PlayerState.Dash;
    }
    
    private void LookAtMouse()
    {
        // 1. 현재 마우스의 스크린 좌표 가져오기
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        // 2. 카메라에서 마우스 지점을 통과하는 레이 생성
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPos);

        // 3. 캐릭터의 높이(Y축)를 기준으로 하는 수평 평면 생성
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

        // 4. 레이가 평면과 만나는 지점 계산
        if (groundPlane.Raycast(ray, out float hitDistance))
        {
            Vector3 targetPoint = ray.GetPoint(hitDistance);
        
            // 5. 캐릭터 위치에서 타겟 지점까지의 방향 계산 (Y축은 0으로 고정)
            Vector3 lookDir = (targetPoint - transform.position).normalized;
            lookDir.y = 0;

            if (lookDir != Vector3.zero)
            {
                // 즉시 회전
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    

}