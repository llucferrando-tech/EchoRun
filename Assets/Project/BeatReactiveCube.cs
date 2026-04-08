using UnityEngine;

namespace EchoRun.Level
{
    [RequireComponent(typeof(Renderer))]
    public sealed class BeatReactiveCube : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;

        [Header("Pulse")]
        [SerializeField] private float beatPulseAmount = 1f;
        [SerializeField] private float pulseDecaySpeed = 6f;
        [SerializeField] private bool stackBeats = false;

        [Header("Optional variation")]
        [SerializeField] private float pulseMultiplier = 1f;
        [SerializeField] private bool randomizeInitialPulse = false;
        [SerializeField] private Vector2 randomPulseRange = new Vector2(0f, 0.2f);

        private MaterialPropertyBlock _propertyBlock;
        private float _pulse;

        private static readonly int PulseId = Shader.PropertyToID("_Pulse");

        private void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();

            _propertyBlock = new MaterialPropertyBlock();

            if (randomizeInitialPulse)
                _pulse = Random.Range(randomPulseRange.x, randomPulseRange.y);

            Apply();
        }

        private void OnEnable()
        {
            BeatPulseManager.BeatTriggered += HandleBeatTriggered;
        }

        private void OnDisable()
        {
            BeatPulseManager.BeatTriggered -= HandleBeatTriggered;
        }

        private void Update()
        {
            _pulse = Mathf.MoveTowards(_pulse, 0f, pulseDecaySpeed * Time.deltaTime);
            Apply();
        }

        private void HandleBeatTriggered()
        {
            float beatValue = Mathf.Clamp01(beatPulseAmount * pulseMultiplier);

            if (stackBeats)
                _pulse = Mathf.Clamp01(_pulse + beatValue);
            else
                _pulse = beatValue;

            Apply();
        }

        private void Apply()
        {
            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(PulseId, _pulse);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}