using UnityEngine;

namespace EchoRun.UI
{
    public sealed class BeatPopupPool : MonoBehaviour
    {
        [SerializeField] private BeatPopup popupPrefab;
        [SerializeField] private int poolSize = 5;

        private BeatPopup[] _pool;
        private int _index;

        private void Awake()
        {
            _pool = new BeatPopup[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                _pool[i] = Instantiate(popupPrefab, transform);
                _pool[i].gameObject.SetActive(false);
            }
        }

        public void Show(string message, Color color)
        {
            BeatPopup popup = _pool[_index];
            _index = (_index + 1) % _pool.Length;

            popup.Play(message, color);
        }
    }
}