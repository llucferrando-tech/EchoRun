using TMPro;
using UnityEngine;
using DG.Tweening;

namespace EchoRun.UI
{
    public sealed class ScoreUIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private float countDuration = 0.2f;

        private int _displayedScore;
        private Tween _scoreTween;

        private void Start()
        {
            UpdateText(0);
        }

        public void SetScore(int newScore)
        {
            _scoreTween?.Kill();

            _scoreTween = DOTween.To(
                () => _displayedScore,
                value =>
                {
                    _displayedScore = value;
                    UpdateText(_displayedScore);
                },
                newScore,
                countDuration
            ).SetEase(Ease.OutQuad);
        }

        private void UpdateText(int value)
        {
            scoreText.text = value.ToString();
        }
    }
}