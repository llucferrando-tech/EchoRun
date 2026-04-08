using TMPro;
using UnityEngine;
using DG.Tweening;

namespace EchoRun.UI
{
    public sealed class BeatPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private float moveY = 100f;
        [SerializeField] private float duration = 0.6f;

        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private Tween _tween;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Play(string message, Color color)
        {
            gameObject.SetActive(true);

            text.text = message;
            text.color = color;

            _tween?.Kill();

            _rect.anchoredPosition = Vector2.zero;
            _canvasGroup.alpha = 1f;

            Sequence seq = DOTween.Sequence();

            seq.Join(_rect.DOAnchorPosY(moveY, duration).SetEase(Ease.OutQuad));
            seq.Join(_canvasGroup.DOFade(0f, duration));
            _rect.DOScale(1.2f, 0.1f).From(0.8f);

            seq.OnComplete(() =>
            {
                gameObject.SetActive(false);
            });

            _tween = seq;
        }
    }
}