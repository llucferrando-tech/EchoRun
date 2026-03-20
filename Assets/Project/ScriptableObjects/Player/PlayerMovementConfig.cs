using UnityEngine;

namespace EchoRun.Player
{
    [CreateAssetMenu(
        fileName = "PlayerMovementConfig",
        menuName = "EchoRun/Player/Movement Config")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [Header("Lanes")]
        [Min(0f)]
        [SerializeField] private float laneWidth = 2.5f;

        [Min(1f)]
        [SerializeField] private float laneChangeSpeed = 12f;

        [Header("Jump")]
        [Min(0f)]
        [SerializeField] private float jumpForce = 8f;

        [Tooltip("Gravity while going UP (lower = floatier jump start)")]
        [Min(0f)]
        [SerializeField] private float riseGravityMultiplier = 1.2f;

        [Tooltip("Gravity while falling down")]
        [Min(0f)]
        [SerializeField] private float fallGravityMultiplier = 2.5f;

        [Tooltip("Gravity when player swipes down mid-air")]
        [Min(0f)]
        [SerializeField] private float fastFallGravityMultiplier = 4.5f;

        [Tooltip("How much upward velocity is kept when cancelling jump (lower = snappier drop)")]
        [Range(0f, 2f)]
        [SerializeField] private float jumpCutMultiplier = 0.3f;

        [Header("Ground Check")]
        [Min(0f)]
        [SerializeField] private float groundCheckDistance = 0.3f;

        [SerializeField] private LayerMask groundLayers = ~0;

        public float LaneWidth => laneWidth;
        public float LaneChangeSpeed => laneChangeSpeed;
        public float JumpForce => jumpForce;

        public float RiseGravityMultiplier => riseGravityMultiplier;
        public float FallGravityMultiplier => fallGravityMultiplier;
        public float FastFallGravityMultiplier => fastFallGravityMultiplier;
        public float JumpCutMultiplier => jumpCutMultiplier;

        public float GroundCheckDistance => groundCheckDistance;
        public LayerMask GroundLayers => groundLayers;
    }
}