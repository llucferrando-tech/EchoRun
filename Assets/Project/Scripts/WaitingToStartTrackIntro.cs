using DG.Tweening;
using EchoRun.Core;
using UnityEngine;

namespace EchoRun.UI
{
    public sealed class WaitingToStartTrackIntro : MonoBehaviour
    {
        [Header("Roots To Toggle")]
        [SerializeField] private GameObject playerRoot;
        [SerializeField] private GameObject trackParentRoot;

        [Header("Individual Tracks")]
        [SerializeField] private Transform leftTrack;
        [SerializeField] private Transform centerTrack;
        [SerializeField] private Transform rightTrack;

        [Header("Movement")]
        [SerializeField] private Vector3 hiddenLocalOffset = new Vector3(0f, -4f, 0f);
        [SerializeField] private float introDuration = 0.55f;
        [SerializeField] private float outroDuration = 0.35f;
        [SerializeField] private float stagger = 0.08f;
        [SerializeField] private Ease introEase = Ease.OutBack;
        [SerializeField] private Ease outroEase = Ease.InBack;

        private Sequence _sequence;

        private Vector3 _leftShownLocalPosition;
        private Vector3 _centerShownLocalPosition;
        private Vector3 _rightShownLocalPosition;

        private bool _cached;
        private bool _isShown;

        private void Awake()
        {
            CacheShownPositions();
            HideImmediate();
        }

        private void OnEnable()
        {
            GameSignals.StateChanged += HandleStateChanged;
            GameSignals.BackToSongSelectAnimatedRequested += HandleBackToSongSelectAnimatedRequested;
        }

        private void OnDisable()
        {
            GameSignals.StateChanged -= HandleStateChanged;
            GameSignals.BackToSongSelectAnimatedRequested -= HandleBackToSongSelectAnimatedRequested;

            _sequence?.Kill();
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.WaitingToStart)
            {
                PlayIntro();
                return;
            }

            if (_isShown &&
                state != GameState.WaitingToStart &&
                state != GameState.Defeat &&
                state != GameState.Victory)
            {
                HideImmediate();
            }
        }

        public void PlayIntro()
        {
            CacheShownPositions();

            _sequence?.Kill();

            if (playerRoot != null)
                playerRoot.SetActive(true);

            if (trackParentRoot != null)
                trackParentRoot.SetActive(true);

            SetTracksHiddenImmediate();

            _sequence = DOTween.Sequence();

            AddTrackIntro(leftTrack, _leftShownLocalPosition, 0f);
            AddTrackIntro(centerTrack, _centerShownLocalPosition, stagger);
            AddTrackIntro(rightTrack, _rightShownLocalPosition, stagger * 2f);

            _sequence.OnComplete(() =>
            {
                _isShown = true;
            });
        }

        public void PlayOutro(System.Action onComplete = null)
        {
            CacheShownPositions();

            _sequence?.Kill();

            if (playerRoot != null)
                playerRoot.SetActive(true);

            if (trackParentRoot != null)
                trackParentRoot.SetActive(true);

            _sequence = DOTween.Sequence();

            AddTrackOutro(rightTrack, _rightShownLocalPosition + hiddenLocalOffset, 0f);
            AddTrackOutro(centerTrack, _centerShownLocalPosition + hiddenLocalOffset, stagger);
            AddTrackOutro(leftTrack, _leftShownLocalPosition + hiddenLocalOffset, stagger * 2f);

            _sequence.OnComplete(() =>
            {
                _isShown = false;

                if (playerRoot != null)
                    playerRoot.SetActive(false);

                if (trackParentRoot != null)
                    trackParentRoot.SetActive(false);

                onComplete?.Invoke();
            });
        }

        public void HideImmediate()
        {
            _sequence?.Kill();

            SetTracksHiddenImmediate();

            if (playerRoot != null)
                playerRoot.SetActive(false);

            if (trackParentRoot != null)
                trackParentRoot.SetActive(false);

            _isShown = false;
        }

        private void HandleBackToSongSelectAnimatedRequested()
        {
            PlayOutro(() =>
            {
                GameSignals.RaiseRunCleanupRequested();
                GameSignals.RaiseBackToSongSelectRequested();
            });
        }

        private void AddTrackIntro(Transform track, Vector3 shownLocalPosition, float delay)
        {
            if (track == null)
                return;

            _sequence.Insert(
                delay,
                track.DOLocalMove(shownLocalPosition, introDuration)
                    .SetEase(introEase)
            );
        }

        private void AddTrackOutro(Transform track, Vector3 hiddenLocalPosition, float delay)
        {
            if (track == null)
                return;

            _sequence.Insert(
                delay,
                track.DOLocalMove(hiddenLocalPosition, outroDuration)
                    .SetEase(outroEase)
            );
        }

        private void SetTracksHiddenImmediate()
        {
            CacheShownPositions();

            if (leftTrack != null)
                leftTrack.localPosition = _leftShownLocalPosition + hiddenLocalOffset;

            if (centerTrack != null)
                centerTrack.localPosition = _centerShownLocalPosition + hiddenLocalOffset;

            if (rightTrack != null)
                rightTrack.localPosition = _rightShownLocalPosition + hiddenLocalOffset;
        }

        private void CacheShownPositions()
        {
            if (_cached)
                return;

            if (leftTrack != null)
                _leftShownLocalPosition = leftTrack.localPosition;

            if (centerTrack != null)
                _centerShownLocalPosition = centerTrack.localPosition;

            if (rightTrack != null)
                _rightShownLocalPosition = rightTrack.localPosition;

            _cached = true;
        }
    }
}