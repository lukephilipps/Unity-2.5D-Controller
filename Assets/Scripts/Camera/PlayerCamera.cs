using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerCamera : MonoBehaviour
{
    Transform _playerCameraRoot;
    CinemachineVirtualCamera _camera;
    Cinemachine3rdPersonFollow _cameraFollow;
    public bool onLeftSide {private get; set;}
    [SerializeField] float _horizontalOffset;
    [SerializeField] float _horizontalBuffering;
    [SerializeField] float _jumpPositionOffset;
    [SerializeField, Range(0f, 30f)] float _jumpAngleOffset;
    [SerializeField, Range(0f, 1f)] float _jumpOffsetRequirement;
    [SerializeField] float _jumpSmoothing;
    public bool playerMoving {private get; set;}
    public bool playerGrounded {private get; set;}
    public float playerYPosition {private get; set;}
    public float playerJumpHeight {private get; set;}
    public bool playerFalling {private get; set;}
    float _yOffset;
    float _zOffset;
    float _cameraAnchorPos;
    float _jumpOffsetPercentage;
    float _startingShoulderYOffset;
    bool _canAngleCameraDown;

    private void Start()
    {
        _camera = GetComponent<CinemachineVirtualCamera>();
        _playerCameraRoot = GameObject.Find("Player").transform.GetChild(2).transform;
        _camera.m_Follow = _playerCameraRoot;
        _yOffset = _playerCameraRoot.localPosition.y;
        _cameraFollow = _camera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        _startingShoulderYOffset = _cameraFollow.ShoulderOffset.y;
    }

    private void LateUpdate()
    {
        // If the player is grounded, anchor camera here
        if (playerGrounded)
        {
            _cameraAnchorPos = playerYPosition;
        }
        
        // If the player is below current anchor, move it down
        if (playerYPosition <= _cameraAnchorPos)
        {
            _cameraAnchorPos = playerYPosition;
            if (_jumpOffsetPercentage > 0f) _jumpOffsetPercentage -= _jumpSmoothing * Time.deltaTime;
            if (_jumpOffsetPercentage < 0f)
            {
                _jumpOffsetPercentage = 0f;
            }
        }
        // else move camera slightly towards player
        else
        {
            float goalPercentage = (playerYPosition - _cameraAnchorPos) / playerJumpHeight;
            if (_canAngleCameraDown && playerFalling)
            {
                if (_jumpOffsetPercentage > 0f) _jumpOffsetPercentage -= _jumpSmoothing * Time.deltaTime * (1 - goalPercentage);
                if (_jumpOffsetPercentage < 0f)
                {
                    _jumpOffsetPercentage = 0f;
                }
            }
            else if (goalPercentage > _jumpOffsetRequirement)
            {
                if (playerFalling) _canAngleCameraDown = true;
                _jumpOffsetPercentage += _jumpSmoothing * Time.deltaTime * (1 - goalPercentage);
                if (_jumpOffsetPercentage > goalPercentage) _jumpOffsetPercentage = goalPercentage;
            }
        }

        // Handle player moving left and right
        if (!onLeftSide && playerMoving)
        {
            if (_zOffset > -_horizontalOffset)
            {
                _zOffset -= _horizontalBuffering * Time.deltaTime;
                if (_zOffset < -_horizontalOffset)
                {
                    _zOffset = -_horizontalOffset;
                }
            }
        }
        else if (playerMoving)
        {
            if (_zOffset < _horizontalOffset)
            {
                _zOffset += _horizontalBuffering * Time.deltaTime;
                if (_zOffset > _horizontalOffset)
                {
                    _zOffset = _horizontalOffset;
                }
            }
        }

        _cameraFollow.ShoulderOffset = new Vector3(_cameraFollow.ShoulderOffset.x, _startingShoulderYOffset + (_jumpOffsetPercentage * _jumpPositionOffset), _cameraFollow.ShoulderOffset.z); 

        // Set local position of _playerCameraRoot z pos
        _playerCameraRoot.localPosition = new Vector3(_playerCameraRoot.localPosition.x, _playerCameraRoot.localPosition.y, _zOffset);

        // Set world position of _playerCameraRoot y pos
        _playerCameraRoot.position = new Vector3(_playerCameraRoot.position.x, _cameraAnchorPos + _yOffset, _playerCameraRoot.position.z);

        _playerCameraRoot.rotation = Quaternion.Euler(-(_jumpOffsetPercentage * _jumpAngleOffset), _playerCameraRoot.rotation.eulerAngles.y, _playerCameraRoot.rotation.eulerAngles.z);
    }
}