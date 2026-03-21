using System.Collections;
using DG.Tweening;
using EchoRun.Core;
using TMPro;
using UnityEngine;

namespace EchoRun.UI
{
    public sealed class CountdownView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text countdownText;

        [Header("Animation")]
        [SerializeField] private float scaleDuration = 0.25f;
        [SerializeField] private float startScale = 1.8f;
        [SerializeField] private float endScale = 1f;
        [SerializeField] private float goVisibleDuration = 0.4f;

        private Tween _scaleTween;
        private Coroutine _hideRoutine;

        private void OnEnable()
        {
           // Debug.Log("[CountdownView] OnEnable");
            GameSignals.CountdownStarted += HandleCountdownStarted;
            GameSignals.CountdownTicked += HandleCountdownTicked;
            GameSignals.CountdownGo += HandleCountdownGo;
            GameSignals.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            //Debug.Log("[CountdownView] OnDisable");
            GameSignals.CountdownStarted -= HandleCountdownStarted;
            GameSignals.CountdownTicked -= HandleCountdownTicked;
            GameSignals.CountdownGo -= HandleCountdownGo;
            GameSignals.RunEnded -= HandleRunEnded;

            _scaleTween?.Kill();

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }
        }

        private void Start()
        {
            //Debug.Log("[CountdownView] Start");
            HideImmediate();
        }

        private void HandleCountdownStarted()
        {
            //Debug.Log("[CountdownView] CountdownStarted received");
            Show();
            SetText(string.Empty);
        }

        private void HandleCountdownTicked(int value)
        {
            //Debug.Log($"[CountdownView] CountdownTicked received: {value}");
            AnimateText(value.ToString());
        }

        private void HandleCountdownGo()
        {
            //Debug.Log("[CountdownView] CountdownGo received");
            AnimateText("GO");

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
            }

            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private void HandleRunEnded()
        {
            //Debug.Log("[CountdownView] RunEnded received");

            _scaleTween?.Kill();

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
                _hideRoutine = null;
            }

            HideImmediate();
        }

        private IEnumerator HideAfterDelay()
        {
            //Debug.Log("[CountdownView] HideAfterDelay started");
            yield return new WaitForSeconds(goVisibleDuration);
            HideImmediate();
            _hideRoutine = null;
            //Debug.Log("[CountdownView] Hidden after GO");
        }

        private void AnimateText(string value)
        {
            if (countdownText == null)
            {
                Debug.LogWarning("[CountdownView] countdownText is null");
                return;
            }

           // Debug.Log($"[CountdownView] AnimateText: {value}");

            _scaleTween?.Kill();

            countdownText.text = value;
            countdownText.transform.localScale = Vector3.one * startScale;

            _scaleTween = countdownText.transform
                .DOScale(endScale, scaleDuration)
                .SetEase(Ease.OutBack);
        }

        private void SetText(string value)
        {
            if (countdownText != null)
            {
                countdownText.text = value;
            }
            else
            {
                Debug.LogWarning("[CountdownView] SetText failed, countdownText is null");
            }
        }

        private void Show()
        {
            if (root != null)
            {
                root.SetActive(true);
               // Debug.Log("[CountdownView] Show");
            }
            else
            {
                //Debug.LogWarning("[CountdownView] root is null");
            }
        }

        private void HideImmediate()
        {
            if (root != null)
            {
                root.SetActive(false);
                //Debug.Log("[CountdownView] HideImmediate");
            }
            else
            {
                //Debug.LogWarning("[CountdownView] root is null");
            }
        }
    }
}