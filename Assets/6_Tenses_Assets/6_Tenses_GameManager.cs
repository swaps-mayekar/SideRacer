using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
// using System.Linq;
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

        [SerializeField]
        int m_questionsLimitForClassroom = 10;

        Material m_roadMaterial;
        Vector2 m_roadMatOffset = Vector2.zero;
        bool m_isScrolling = false, m_isAnswered = false, l_isQuestionShown = false, l_isAnsweredCorrect = false;
        bool m_isTopAnswerCorrect = false, m_isMiddleAnswerCorrect = false, m_isBottomAnswerCorrect = false;
        Vector2 m_currentAnswerPos, m_defaultAnswerPos, m_newPosition;
        float m_movement = 0;
        int l_randomIndex, m_currentQuestionNum = 1;

        GameDifficuly m_currentDifficulty = GameDifficuly.Easy;
        GameMode m_currentGameMode = GameMode.Classroom;

        enum Lane
        {
            Top,
            Middle,
            Bottom
        }

        Lane m_currentLane = Lane.Top;
        Six_Tenses_Question[] m_availableQuestions;
        List<int> m_shownQuestions = new();


        [Header("References")]

        [SerializeField]
        Image m_roadImage = null;

        [SerializeField]
        GameObject m_gamePanel, m_welcomePanel;

        [SerializeField]
        RectTransform m_playerRT, m_topAnswerRT, m_middleAnswerRT, m_bottomAnswerRT, m_answrParentRT;

        [SerializeField]
        TextMeshProUGUI m_questionText, m_topAnswerText, m_middleAnswerText, m_bottomAnswerText, m_scoringText;

        [SerializeField]
        Image m_topPotholeImg, m_middlePotholeImg, m_bottomPotholeImg;

        [SerializeField]
        WaitForSeconds m_waitInitial = new WaitForSeconds(2f), m_waitAfterAnswer = new WaitForSeconds(2f);

        [SerializeField]
        TextAsset m_dataJSON;

        [SerializeField]
        Color m_colorDefault = Color.white, m_colorCorrect = Color.green, m_colorWrong = Color.red;



        void Awake()
        {
            m_welcomePanel.SetActive(!m_autoStart);
        }

        IEnumerator Start()
        {
            // TODO: Set difficulty here
            m_currentDifficulty = GameDifficuly.Easy;

            // TODO: set game mode here
            m_currentGameMode = GameMode.Classroom;

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
            PopulateQuestions();

            yield return m_waitInitial;

            if (m_autoStart)
            {
                StartGame();
            }
        }

        void PopulateQuestions()
        {
            if (m_dataJSON != null)
            {
                QuestionsWrapper l_qWrapper = JsonUtility.FromJson<QuestionsWrapper>(m_dataJSON.text);
                if (l_qWrapper == null)
                {
                    Debug.LogError("Tenses_GameManager::PopulateQuestions::Failed to parse JSON data!");
                    return;
                }

                m_availableQuestions = l_qWrapper.questions;
                if (m_availableQuestions == null)
                {
                    Debug.LogError("Tenses_GameManager::PopulateQuestions::No questions array found in JSON!");
                    return;
                }

                // Debug.Log($"Tenses_GameManager::PopulateQuestions::Total questions before filtering: {m_availableQuestions.Length}");
                // Debug.Log($"Tenses_GameManager::PopulateQuestions::Current difficulty: {m_currentDifficulty}");

                // Filter questions based on difficulty
                List<Six_Tenses_Question> l_filteredQuestions = new List<Six_Tenses_Question>();
                string l_currentDifficultyLower = m_currentDifficulty.ToString().ToLower();
                foreach (var l_question in m_availableQuestions)
                {
                    if (l_question != null && l_question.Difficulty != null &&
                        l_question.Difficulty.ToLower() == l_currentDifficultyLower)
                    {
                        l_filteredQuestions.Add(l_question);
                    }
                }
                m_availableQuestions = l_filteredQuestions.ToArray();
                m_shownQuestions.Clear();

                if (m_availableQuestions.Length == 0)
                {
                    Debug.LogError($"Tenses_GameManager::PopulateQuestions::No questions found for difficulty: {m_currentDifficulty}");
                }
                else
                {
                    Debug.Log("Tenses_GameManager::PopulateQuestions::Found " + m_availableQuestions.Length + " questions.");
                }
            }
            else
            {
                Debug.LogError("Tenses_GameManager::PopulateQuestions::No JSON data found!");
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

            m_movement = m_scrollDirection.x * m_scrollSpeed * Time.deltaTime;
            m_currentAnswerPos.x += m_movement;
            m_newPosition.x -= m_movement * 900f;
            m_answrParentRT.anchoredPosition = m_newPosition;

            // Check collision with top answer
            if (RectTransformUtility.RectangleContainsScreenPoint(m_topAnswerRT, m_playerRT.position))
            {
                OnAnswerSelected(m_isTopAnswerCorrect, m_topAnswerText, m_topPotholeImg);
            }
            // Check collision with middle answer
            else if (RectTransformUtility.RectangleContainsScreenPoint(m_middleAnswerRT, m_playerRT.position))
            {
                OnAnswerSelected(m_isMiddleAnswerCorrect, m_middleAnswerText, m_middlePotholeImg);
            }
            // Check collision with bottom answer
            else if (RectTransformUtility.RectangleContainsScreenPoint(m_bottomAnswerRT, m_playerRT.position))
            {
                OnAnswerSelected(m_isBottomAnswerCorrect, m_bottomAnswerText, m_bottomPotholeImg);
            }
        }

        void OnAnswerSelected(bool a_isAnswerCorrect, TextMeshProUGUI a_answerText, Image a_potholeImg)
        {
            m_isAnswered = true;
            if (a_isAnswerCorrect)
            {
                l_isAnsweredCorrect = true;
                a_answerText.color = m_colorCorrect;
            }
            else
            {
                a_answerText.color = m_colorWrong;
                a_potholeImg.enabled = true;
            }

            if (m_scoringCheck != null)
            {
                StopCoroutine(m_scoringCheck);
            }
            m_scoringCheck = StartCoroutine(IE_CheckScoring());
        }

        Coroutine m_scoringCheck;

        IEnumerator IE_CheckScoring()
        {
            yield return m_waitAfterAnswer;

            if (m_currentGameMode == GameMode.Classroom)
            {
                m_currentQuestionNum++;
                if (l_isAnsweredCorrect || m_currentQuestionNum < m_questionsLimitForClassroom)
                {
                    ResetAnswers();
                }
                else
                {
                    yield return m_waitAfterAnswer;
                    EndGame();
                }
            }
            else
            {
                ResetAnswers();
            }
            m_scoringCheck = null;
        }

        public void OnClick_StartGame()
        {
            StartGame();
        }

        void StartGame()
        {
            m_scoringText.text = string.Empty;
            m_welcomePanel.SetActive(false);
            m_gamePanel.SetActive(true);
            ResetAnswers();
        }

        public void OnClick_EndGame()
        {
            EndGame();
        }

        void EndGame()
        {
            m_isScrolling = m_isAnswered = false;
            m_gamePanel.SetActive(false);
            m_welcomePanel.SetActive(true);
            // TODO: Return to main scene
        }

        void ResetAnswers()
        {
            m_answrParentRT.anchoredPosition = m_defaultAnswerPos;
            m_topAnswerText.color = m_middleAnswerText.color = m_bottomAnswerText.color = m_colorDefault;
            m_topPotholeImg.enabled = m_middlePotholeImg.enabled = m_bottomPotholeImg.enabled = false;
            m_isAnswered = false;
            ShowQuestion();
        }

        void ShowQuestion()
        {
            m_newPosition = m_answrParentRT.anchoredPosition;
            // If we've shown all questions, reset the shown questions list
            if (m_shownQuestions.Count >= m_availableQuestions.Length)
            {
                m_shownQuestions.Clear();
            }

            do
            {
                l_randomIndex = Random.Range(0, m_availableQuestions.Length);
                l_isQuestionShown = m_shownQuestions.Contains(l_randomIndex);
            } while (l_isQuestionShown);

            m_shownQuestions.Add(l_randomIndex);
            Six_Tenses_Question m_question = m_availableQuestions[l_randomIndex];
            m_questionText.text = m_question.QuestionText;
            m_topAnswerText.text = m_question.Options[0].AnswerText;
            m_isTopAnswerCorrect = m_question.Options[0].IsCorrect;
            m_middleAnswerText.text = m_question.Options[1].AnswerText;
            m_isMiddleAnswerCorrect = m_question.Options[1].IsCorrect;
            m_bottomAnswerText.text = m_question.Options[2].AnswerText;
            m_isBottomAnswerCorrect = m_question.Options[2].IsCorrect;
            m_isScrolling = true;
            if (m_currentGameMode == GameMode.Classroom)
            {
                m_scoringText.text = "Question " + m_currentQuestionNum + "/" + m_questionsLimitForClassroom;
            }
        }

        void HideQuestion()
        {
            ResetAnswers();
        }
    }

    [System.Serializable]
    public class Six_Tenses_Question
    {
        public string QuestionText;
        public Six_Tenses_Answer[] Options;
        public string Difficulty;
    }

    [System.Serializable]
    public class Six_Tenses_Answer
    {
        public string AnswerText;
        public bool IsCorrect;
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

    [System.Serializable]
    public class QuestionsWrapper
    {
        public Six_Tenses_Question[] questions;
    }
}