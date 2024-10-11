using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : CharacterController
{
    Vector2 _velocity;
    
    [Header("Jump Variables")]
    [SerializeField] float _jumpHeight;
    [SerializeField] float _gravity = -20f; 
    [SerializeField] float _fallGravityMultiplier = 1.75f;
    [SerializeField] float _maxFallSpeed = -10f;
    bool _jumpReleased;
    bool _jumped;
    float _jumpTimer;
    float _jumpTimeout = .05f;
    Transform _playerCameraRoot;
    float _lastCameraYPos;
    PlayerCamera _playerCamera;

    protected override void Start()
    {
        base.Start();

        _lastCameraYPos = transform.position.y;
        _playerCamera = GameObject.FindObjectsOfType<PlayerCamera>()[0];
        _playerCamera.playerJumpHeight = _jumpHeight;
    }

    private void Update()
    {
        _playerCamera.playerGrounded = false;

        // Move left
        if (Input.GetKey("a"))
        {
            _velocity.x = -speed;
            facingLeft = true;
            _playerCamera.onLeftSide = false;
            _playerCamera.playerMoving = true;
        }
        // Move right
        else if (Input.GetKey("d"))
        {
            _velocity.x = speed;
            facingLeft = false;
            _playerCamera.onLeftSide = true;
            _playerCamera.playerMoving = true;
        }
        else 
        {
            _velocity.x = 0f;
            _playerCamera.playerMoving = false;
        }

        // Ceiling check
        if (collisions.above)
        {
            _velocity.y = 0f;
        }

        // Add to jump timer (used for coyote time)
        if (_jumpTimer < _jumpTimeout) _jumpTimer += Time.deltaTime;

        // Grounded check
        if (collisions.below)
        {
            _jumped = false;
            _jumpTimer = 0;
            _velocity.y = -3f;
        }

        // Jump check
        if (Input.GetKeyDown("space") && _jumpTimer < _jumpTimeout)
        {
            Jump();
        }
        // Jump release check
        else if (_jumped && Input.GetKeyUp("space") && _velocity.y > 0 && !_jumpReleased)
        {
            _jumpReleased = true;
            _velocity.y *= .5f;
        }

        // Apply gravity to y velocity. Apply more if falling
        if (_jumpReleased) _velocity.y += _gravity * _fallGravityMultiplier * Time.deltaTime;
        else _velocity.y += _gravity * Time.deltaTime;

        // Cap falling velocity
        if (_velocity.y < _maxFallSpeed) _velocity.y = _maxFallSpeed;

        // Move player
        Move(_velocity * Time.deltaTime);

        if (collisions.below) _playerCamera.playerGrounded = true;

        _playerCamera.playerYPosition = transform.position.y;
        _playerCamera.playerFalling = (Mathf.Sign(_velocity.y) == -1);
    }

    void Jump()
    {
        _jumpReleased = false;
        _jumped = true;
        _velocity.y = Mathf.Sqrt(_jumpHeight * -2 * _gravity);
    }
}
