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

        [Min(0f)]
        [SerializeField] private float gravityMultiplier = 2f;

        [Header("Ground Check")]
        [Min(0f)]
        [SerializeField] private float groundCheckDistance = 0.3f;

        [SerializeField] private LayerMask groundLayers = ~0;

        public float LaneWidth => laneWidth;
        public float LaneChangeSpeed => laneChangeSpeed;
        public float JumpForce => jumpForce;
        public float GravityMultiplier => gravityMultiplier;
        public float GroundCheckDistance => groundCheckDistance;
        public LayerMask GroundLayers => groundLayers;
    }
}