using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using BansheeGz.BGSpline.Curve;
using BansheeGz.BGSpline.Components;

public class CharacterController : MonoBehaviour
{
    struct RaycastOrigins
    {
        public Vector3 topLeft, topRight;
        public Vector3 bottomLeft, bottomRight;
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool climbingSlope;
        public float slopeAngle, slopeAngleOld;

        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0f;
        }
    }

    // [SerializeField] GameObject anchor;
    // BGCurve _curve;
    BGCcCursor _cursor;

    [Header("Movement Stats")]
    [SerializeField] protected float speed;
    protected bool facingLeft;

    [SerializeField] LayerMask _collisionMask;

    const float _skinWidth = .015f;
    [SerializeField] int _horizontalRayCount = 4;
    [SerializeField] int _verticalRayCount = 4;

    [SerializeField] float _maxClimbAngle = 80f;
    [SerializeField] float _maxDescendAngle = 75f;

    float _horizontalRaySpacing;
    float _verticalRaySpacing;

    BoxCollider _collider;
    Transform _characterMesh;
    RaycastOrigins _raycastOrigins;
    protected CollisionInfo collisions;

    private void Awake()
    {
        GameObject path = GameObject.Find("Path");
        // _curve = path.GetComponent<BGCurve>();

        _cursor = path.AddComponent<BGCcCursor>();

        // // Create a TRS for the character on the path
        // BGCcTrs trs = path.AddComponent<BGCcTrs>();

        // // Create an anchor on the TRS and change some of the fields on it
        // trs.ObjectToManipulate = Instantiate(anchor.transform);
        // trs.RotateObject = true;
        // trs.OverflowControl = BGCcTrs.OverflowControlEnum.Stop;
        // trs.Speed = 0;
    }

    protected virtual void Start()
    {
        _collider = GetComponentInChildren<BoxCollider>();
        _characterMesh = transform.GetChild(1);
        CalculateRaySpacing();
    }

    public void Move(Vector2 velocity)
    {
        Physics.SyncTransforms();

        UpdateRaycastOrigins();
        collisions.Reset();

        if (velocity.x != 0)
            HorizontalCollisions(ref velocity);
        if (velocity.y != 0)
            VerticalCollisions(ref velocity);

        _cursor.Distance += velocity.x;
        Vector3 position = _cursor.CalculatePosition();
        
        transform.position = new Vector3(position.x, transform.position.y + velocity.y, position.z);

        // Change the rotation of the character
        Vector3 tangent = _cursor.CalculateTangent();

        transform.rotation = Quaternion.LookRotation(new Vector3(tangent.x, 0, tangent.z), Vector3.up);
        if (facingLeft) _characterMesh.rotation = Quaternion.LookRotation(new Vector3(-tangent.x, 0, -tangent.z), Vector3.up);
        else _characterMesh.rotation = transform.rotation;
    }

    void HorizontalCollisions(ref Vector2 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        float rayLength = Mathf.Abs(velocity.x) + _skinWidth;
        
        for (int i = 0; i < _horizontalRayCount; i++) 
        {
            Vector3 rayOrigin = (directionX == -1) ? _raycastOrigins.bottomLeft : _raycastOrigins.bottomRight;
            rayOrigin += transform.up * (_horizontalRaySpacing * i);
            RaycastHit hit;

            Debug.DrawRay(rayOrigin, transform.forward * directionX * rayLength, Color.red);
            
            if (Physics.Raycast(rayOrigin, transform.forward * directionX, out hit, rayLength, _collisionMask))
            {
                _cursor.Distance += velocity.x + (_collider.transform.localScale.x * .5f * directionX - _skinWidth);
                Vector3 position = _cursor.CalculatePosition();
                Vector3 tangent = _cursor.CalculateTangent() * directionX;
                _cursor.Distance -= velocity.x + (_collider.transform.localScale.x * .5f * directionX - _skinWidth);

                RaycastHit slopeHit;
                Physics.Raycast(new Vector3(position.x, _raycastOrigins.bottomRight.y, position.z) - (tangent * .1f), tangent, out slopeHit, 1f);

                float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);

                if (i == 0 && slopeAngle <= _maxClimbAngle)
                {
                    float distanceToSlopeStart = 0f;
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - _skinWidth;
                        velocity.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref velocity, slopeAngle);
                    velocity.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > _maxClimbAngle)
                {
                    velocity.x = (hit.distance - _skinWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope)
                    {
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }
        }
    }

    void VerticalCollisions(ref Vector2 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + _skinWidth;
        
        for (int i = 0; i < _verticalRayCount; i++) 
        {
            Vector3 rayOrigin = (directionY == -1) ? _raycastOrigins.bottomLeft : _raycastOrigins.topLeft;
            rayOrigin += transform.forward * (_verticalRaySpacing * i + velocity.x);
            RaycastHit hit;

            Debug.DrawRay(rayOrigin, transform.up * directionY * rayLength, Color.red);

            if (Physics.Raycast(rayOrigin, transform.up * directionY * rayLength, out hit, rayLength, _collisionMask))
            {
                velocity.y = (hit.distance - _skinWidth) * directionY;
                rayLength = hit.distance;

                if (collisions.climbingSlope)
                {
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }
    }

    void ClimbSlope(ref Vector2 velocity, float slopeAngle)
    {
        float moveDistance = Mathf.Abs(velocity.x);
        float climbVelocity = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if (velocity.y <= climbVelocity)
        {
            velocity.y = climbVelocity;

            if (slopeAngle == 0 && collisions.slopeAngleOld != 0)
            {
                velocity.y += .05f;
            }

            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    // *This function assumes characters have an exact same x and z scale
    void UpdateRaycastOrigins()
    {
        // Create a forward vector that doesn't include character skinWidth
        Vector3 forwardVector = (transform.forward * (_collider.transform.localScale.x - 2 * _skinWidth) / _collider.transform.localScale.x) * .5f;

        // Create an up float that doesn't include character skin
        float upFloat = (transform.up * _collider.transform.localScale.y * .5f).y - _skinWidth;

        _raycastOrigins.bottomLeft = new Vector3(transform.position.x - forwardVector.x, _collider.transform.position.y - upFloat, transform.position.z - forwardVector.z);
        _raycastOrigins.bottomRight = new Vector3(transform.position.x + forwardVector.x, _collider.transform.position.y - upFloat, transform.position.z + forwardVector.z);
        _raycastOrigins.topLeft = new Vector3(transform.position.x - forwardVector.x, _collider.transform.position.y + upFloat, transform.position.z - forwardVector.z);
        _raycastOrigins.topRight = new Vector3(transform.position.x + forwardVector.x, _collider.transform.position.y + upFloat, transform.position.z + forwardVector.z);
    }

    void CalculateRaySpacing()
    {
        Bounds bounds = _collider.bounds;
        bounds.Expand(_skinWidth * -2);

        _horizontalRayCount = Mathf.Clamp(_horizontalRayCount, 2, int.MaxValue);
        _verticalRayCount = Mathf.Clamp(_verticalRayCount, 2, int.MaxValue);

        _horizontalRaySpacing = bounds.size.y / (_horizontalRayCount - 1);
        _verticalRaySpacing = bounds.size.z / (_verticalRayCount - 1);
    }

    // public void MoveLeft()
    // {
    //     if (_cursor.DistanceRatio > 0f) 
    //     {
    //         // Move the cursor
    //         _cursor.Distance -= speed * Time.deltaTime;

    //         // Change the rotation of the character
    //         Vector3 tangent = _cursor.CalculateTangent();
    //         transform.rotation = Quaternion.LookRotation(new Vector3(-tangent.x, 0, -tangent.z), Vector3.up);

    //         // Change the position of the character
    //         Vector3 position = _cursor.CalculatePosition();
    //         transform.position = new Vector3(position.x, 0, position.z);
    //     }
    // }
    
    // public void MoveRight()
    // {
    //     if (_cursor.DistanceRatio < 1f) 
    //     {
    //         // Move the cursor
    //         _cursor.Distance += speed * Time.deltaTime;

    //         // Change the rotation of the character
    //         Vector3 tangent = _cursor.CalculateTangent();
    //         transform.rotation = Quaternion.LookRotation(new Vector3(tangent.x, 0, tangent.z), Vector3.up);
            
    //         // Change the position of the character
    //         Vector3 position = _cursor.CalculatePosition();
    //         transform.position = new Vector3(position.x, 0, position.z);
    //     }
    // }

    // private void OnValidate()
    // {
    //     CalculateRaySpacing();
    // }

    // private void OnDrawGizmos()
    // {
    //     Gizmos.DrawSphere(_raycastOrigins.bottomLeft, .05f);
    //     Gizmos.DrawSphere(_raycastOrigins.bottomRight, .05f);
    //     Gizmos.DrawSphere(_raycastOrigins.topLeft, .05f);
    //     Gizmos.DrawSphere(_raycastOrigins.topRight, .05f);
    // }
}
