using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace Six_Tenses
{
    internal class Six_Tenses_GameManager : MonoBehaviour
    {
        [Header("Game Settings")]

        [SerializeField]
        float m_scrollSpeed = 1f;

        [SerializeField]
        Vector2 m_scrollDirection = Vector2.left;

        [SerializeField]
        bool m_autoStart = false;

        Material m_roadMaterial;
        Vector2 m_roadMatOffset = Vector2.zero;
        bool m_isScrolling = false, m_isAnswered = false;
        bool m_isTopAnswerCorrect = false, m_isMiddleAnswerCorrect = false, m_isBottomAnswerCorrect = false;
        Vector2 m_currentAnswerPos, m_defaultAnswerPos;

        GameDifficuly m_currentDifficulty = GameDifficuly.Hard;

        GameMode m_currentGameMode = GameMode.Classroom;

        Six_Tenses_Questions m_availableQuestions;


        [Header("References")]

        [SerializeField]
        Image m_roadImage = null;

        [SerializeField]
        GameObject m_gamePanel, m_welcomePanel;

        [SerializeField]
        RectTransform m_playerRT, m_topAnswerRT, m_middleAnswerRT, m_bottomAnswerRT, m_answrParentRT;

        [SerializeField]
        TextMeshProUGUI m_questionText, m_topAnswerText, m_middleAnswerText, m_bottomAnswerText;

        [SerializeField]
        WaitForSeconds m_initialWait = new WaitForSeconds(1.0f);

        [SerializeField]
        TextAsset m_dataJSON;







        void Awake()
        {
            m_welcomePanel.SetActive(!m_autoStart);
        }

        IEnumerator Start()
        {
            if (m_roadImage != null)
            {
                m_roadMaterial = m_roadImage.material;
            }
            else
            {
                Debug.LogError("Tenses_GameManager::Start::No Image component found on road!");
                yield break;
            }

            m_currentAnswerPos = m_defaultAnswerPos = m_answrParentRT.anchoredPosition;

            yield return m_initialWait;

            if (m_autoStart)
            {
                StartGame();
            }
        }

        void Update()
        {
            if (!m_isScrolling || m_isAnswered)
            {
                return;
            }

            // Update the offset based on time and speed
            m_roadMatOffset += m_scrollDirection * m_scrollSpeed * Time.deltaTime;

            // Keep the offset values between 0 and 1 for seamless looping
            m_roadMatOffset.x = Mathf.Repeat(m_roadMatOffset.x, 1f);
            // m_roadMatOffset.y = Mathf.Repeat(m_roadMatOffset.y, 1f);

            // Apply the offset to the material
            m_roadMaterial.mainTextureOffset = m_roadMatOffset;

            m_currentAnswerPos.x += m_scrollDirection.x * m_scrollSpeed * Time.deltaTime;
            m_answrParentRT.anchoredPosition = m_currentAnswerPos;
        }

        // Public methods to control the scrolling
        public void StartScrolling()
        {
            m_isScrolling = true;
        }

        public void StopScrolling()
        {
            m_isScrolling = false;
        }

        public void SetScrollSpeed(float a_scrollSpeed)
        {
            m_scrollSpeed = a_scrollSpeed;
        }

        public void SetScrollDirection(Vector2 a_scrollDirection)
        {
            m_scrollDirection = a_scrollDirection.normalized;
        }

        public void OnClick_StartGame()
        {
            ResetAnswers();
            StartGame();
        }

        void StartGame()
        {
            m_welcomePanel.SetActive(false);
            m_gamePanel.SetActive(true);
            m_isScrolling = true;
        }

        public void OnClick_EndGame()
        {
            EndGame();
        }

        void EndGame()
        {
            m_isScrolling = m_isAnswered = false;
            m_welcomePanel.SetActive(true);
            m_gamePanel.SetActive(false);
        }

        void ResetAnswers()
        {
            m_defaultAnswerPos = m_answrParentRT.anchoredPosition;

        }

    }

    internal class Six_Tenses_Questions
    {
        internal string QuestionText;
        internal Six_Tenses_Answer[] Answers = new Six_Tenses_Answer[3];
    }

    internal class Six_Tenses_Answer
    {
        internal string AnswerText;
        internal bool IsAnswerCorrect;
    }

    internal enum GameDifficuly
    {
        Easy,
        Medium,
        Hard
    }

    internal enum GameMode
    {
        Classroom,
        Unlimited
    }
}