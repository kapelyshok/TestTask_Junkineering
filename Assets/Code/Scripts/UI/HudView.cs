using TMPro;
using UnityEngine;

namespace Junkineering.UI
{
    public class HudView : MonoBehaviour
    {
        private const string LOADING_MESSAGE = "Loading image...";
        private const string READY_MESSAGE = "Tap the object!";
        private const string SCORE_FORMAT = "Score: {0}";

        [Header("References")]
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text scoreText;

        private void Awake()
        {
            ResolveStatusText();
        }

        private void Reset()
        {
            ResolveStatusText();
        }

        public void ShowLoading()
        {
            SetStatus(LOADING_MESSAGE);
        }

        public void ShowReady()
        {
            SetStatus(READY_MESSAGE);
        }

        public void ShowError(string message)
        {
            SetStatus(string.IsNullOrWhiteSpace(message) ? "Loading failed." : message);
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        public void SetScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = string.Format(SCORE_FORMAT, score);
            }
        }

        private void ResolveStatusText()
        {
            if (statusText == null)
            {
                statusText = GetComponent<TMP_Text>();
            }

            if (statusText == null)
            {
                statusText = GetComponentInChildren<TMP_Text>(true);
            }
        }
    }
}
