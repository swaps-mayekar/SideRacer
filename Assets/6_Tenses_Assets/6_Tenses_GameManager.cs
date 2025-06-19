using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
// using System.Linq;
using TMPro;

namespace Six_Tenses
{
    /// <summary>
    /// Manages the main game logic for the Six Tenses game, including question handling,
    /// lane changes, scoring, and UI updates.
    /// </summary>
    internal class Six_Tenses_GameManager : MonoBehaviour
    {
        [Header("Game Settings")]

        [SerializeField]
        float m_scrollSpeed = 1f; // Speed at which the road and answers scroll

        [SerializeField]
        Vector2 m_scrollDirection = Vector2.left; // Direction of scrolling

        [SerializeField]
        bool m_autoStart = false; // Should the game start automatically?

        [SerializeField]
        int m_questionsLimitForClassroom = 10; // Number of questions in Classroom mode

        Material m_roadMaterial;
        Vector2 m_roadMatOffset = Vector2.zero;
        bool m_isScrolling = false, m_isAnswered = false, m_isQuestionShown = false, m_isAnsweredCorrect = false;
        bool m_isTopAnswerCorrect = false, m_isMiddleAnswerCorrect = false, m_isBottomAnswerCorrect = false;
        Vector2 m_currentAnswerPos, m_defaultAnswerPos, m_newPosition;
        float m_movement = 0;
        int l_randomIndex, m_currentQuestionNum = 1; // Current question number

        Lane m_currentLane = Lane.Middle; // Current lane of the car
        Lane CurrentLane
        {
            get => m_currentLane;
            set
            {
                m_currentLane = value;

                // Move the car to the correct lane position
                switch (m_currentLane)
                {
                    case Lane.Top:
                        m_carRT.anchoredPosition = m_carPosTop;
                        break;

                    case Lane.Middle:
                        m_carRT.anchoredPosition = m_carPosMid;
                        break;

                    case Lane.Bottom:
                        m_carRT.anchoredPosition = m_carPosBottom;
                        break;

                    default:
                        Debug.LogError("Tenses_GameManager::ChangeLaneUp::Unhandled lane::" + m_currentLane.ToString());
                        break;
                }
            }
        }
        GameDifficuly m_currentDifficulty = GameDifficuly.Easy; // Current difficulty
        GameMode m_currentGameMode = GameMode.Unlimited; // Current game mode

        enum Lane
        {
            Top,
            Middle,
            Bottom
        }

        Six_Tenses_Question[] m_availableQuestions; // All available questions for the selected difficulty
        List<int> m_shownQuestions = new(); // Indices of questions already shown

        [Header("References")]

        [SerializeField]
        Image m_roadImage = null; // Reference to the road image

        [SerializeField]
        GameObject m_gamePanel, m_welcomePanel; // UI panels

        [SerializeField]
        RectTransform m_carRT, m_topAnswerRT, m_middleAnswerRT, m_bottomAnswerRT, m_answrParentRT; // UI elements for car and answers

        [SerializeField]
        TextMeshProUGUI m_questionText, m_topAnswerText, m_middleAnswerText, m_bottomAnswerText, m_scoringText; // UI text fields

        [SerializeField]
        Image m_topPotholeImg, m_middlePotholeImg, m_bottomPotholeImg; // Pothole images for wrong answers

        [SerializeField]
        WaitForSeconds m_waitInitial = new WaitForSeconds(2f), m_waitAfterAnswer = new WaitForSeconds(2f); // Wait times for coroutines

        [SerializeField]
        TextAsset m_dataJSON; // JSON file containing questions

        [SerializeField]
        Color m_colorDefault = Color.white, m_colorCorrect = Color.green, m_colorWrong = Color.red; // Answer colors

        [SerializeField]
        Vector2 m_carPosTop = new Vector2(200, 150), m_carPosMid = new Vector2(200, -100), m_carPosBottom = new Vector2(200, -350); // Car positions for each lane

        void Awake()
        {
            // Show welcome panel if auto start is disabled
            m_welcomePanel.SetActive(!m_autoStart);
        }

        IEnumerator Start()
        {
            // TODO: Set difficulty here
            m_currentDifficulty = GameDifficuly.Easy;

            // TODO: set game mode here
            m_currentGameMode = GameMode.Classroom;

            // Get the material from the road image
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
            PopulateQuestions(); // Load and filter questions

            yield return m_waitInitial;

            if (m_autoStart)
            {
                StartGame();
            }
        }

        /// <summary>
        /// Loads and filters questions from the JSON file based on the current difficulty.
        /// </summary>
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
            // Only scroll and check for answers if the game is running and not answered
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
            if (RectTransformUtility.RectangleContainsScreenPoint(m_topAnswerRT, m_carRT.position))
            {
                OnAnswerSelected(m_isTopAnswerCorrect, m_topAnswerText, m_topPotholeImg);
            }
            // Check collision with middle answer
            else if (RectTransformUtility.RectangleContainsScreenPoint(m_middleAnswerRT, m_carRT.position))
            {
                OnAnswerSelected(m_isMiddleAnswerCorrect, m_middleAnswerText, m_middlePotholeImg);
            }
            // Check collision with bottom answer
            else if (RectTransformUtility.RectangleContainsScreenPoint(m_bottomAnswerRT, m_carRT.position))
            {
                OnAnswerSelected(m_isBottomAnswerCorrect, m_bottomAnswerText, m_bottomPotholeImg);
            }
        }

        /// <summary>
        /// Handles logic when an answer is selected (car collides with an answer).
        /// </summary>
        void OnAnswerSelected(bool a_isAnswerCorrect, TextMeshProUGUI a_answerText, Image a_potholeImg)
        {
            if (a_answerText == null || a_potholeImg == null)
            {
                Debug.LogError("Tenses_GameManager::OnAnswerSelected::Invalid UI references!");
                return;
            }

            m_isAnswered = true;
            if (a_isAnswerCorrect)
            {
                m_isAnsweredCorrect = true;
                a_answerText.color = m_colorCorrect;
            }
            else
            {
                a_answerText.color = m_colorWrong;
                a_potholeImg.enabled = true;
            }

            // Safely manage coroutine
            if (m_scoringCheck != null)
            {
                StopCoroutine(m_scoringCheck);
                m_scoringCheck = null;
            }
            m_scoringCheck = StartCoroutine(IE_CheckScoring());
        }

        Coroutine m_scoringCheck;

        /// <summary>
        /// Coroutine to handle scoring and progression after an answer is selected.
        /// </summary>
        IEnumerator IE_CheckScoring()
        {
            yield return m_waitAfterAnswer;

            if (m_currentGameMode == GameMode.Classroom)
            {
                m_currentQuestionNum++;
                if (m_isAnsweredCorrect && m_currentQuestionNum < m_questionsLimitForClassroom)
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

        /// <summary>
        /// Called by UI button to start the game.
        /// </summary>
        public void OnClick_StartGame()
        {
            // Debug.Log("Tenses_GameManager::OnClick_StartGame");
            StartGame();
        }

        /// <summary>
        /// Initializes and starts the game session.
        /// </summary>
        void StartGame()
        {
            m_currentQuestionNum = 1;
            m_carRT.anchoredPosition = m_carPosMid;
            m_scoringText.text = string.Empty;
            m_welcomePanel.SetActive(false);
            m_gamePanel.SetActive(true);
            ResetAnswers();
        }

        /// <summary>
        /// Called by UI button to end the game.
        /// </summary>
        public void OnClick_EndGame()
        {
            // Debug.Log("Tenses_GameManager::OnClick_EndGame");
            EndGame();
        }

        /// <summary>
        /// Ends the game and returns to the welcome panel.
        /// </summary>
        void EndGame()
        {
            m_isScrolling = m_isAnswered = false;
            m_gamePanel.SetActive(false);
            m_welcomePanel.SetActive(true);
            // TODO: Return to main scene
        }

        /// <summary>
        /// Resets answer UI and shows the next question.
        /// </summary>
        void ResetAnswers()
        {
            // Validate UI elements
            if (m_answrParentRT == null || m_topAnswerText == null || m_middleAnswerText == null || 
                m_bottomAnswerText == null || m_topPotholeImg == null || m_middlePotholeImg == null || 
                m_bottomPotholeImg == null)
            {
                Debug.LogError("Tenses_GameManager::ResetAnswers::Missing UI references!");
                return;
            }

            m_answrParentRT.anchoredPosition = m_defaultAnswerPos;
            m_topAnswerText.color = m_middleAnswerText.color = m_bottomAnswerText.color = m_colorDefault;
            m_topPotholeImg.enabled = m_middlePotholeImg.enabled = m_bottomPotholeImg.enabled = false;
            m_isAnswered = m_isAnsweredCorrect = false;
            ShowQuestion();
        }

        /// <summary>
        /// Called by UI button to change the car's lane.
        /// </summary>
        public void OnClick_ChangeLane(bool a_isGoingUp)
        {
            // Debug.Log("Tenses_GameManager::OnClick_ChangeLane::Going up:" + a_isGoingUp);
            if (a_isGoingUp)
            {
                ChangeLaneUp();
            }
            else
            {
                ChangeLaneDown();
            }
        }

        /// <summary>
        /// Moves the car up one lane if possible.
        /// </summary>
        void ChangeLaneUp()
        {
            switch (CurrentLane)
            {
                case Lane.Top:
                    // Do nothing as already in top lane
                    break;

                case Lane.Middle:
                    CurrentLane = Lane.Top;
                    break;

                case Lane.Bottom:
                    CurrentLane = Lane.Middle;
                    break;

                default:
                    Debug.LogError("Tenses_GameManager::ChangeLaneUp::Unhandled lane::" + CurrentLane.ToString());
                    break;
            }
        }

        /// <summary>
        /// Moves the car down one lane if possible.
        /// </summary>
        void ChangeLaneDown()
        {
            switch (CurrentLane)
            {
                case Lane.Top:
                    CurrentLane = Lane.Middle;
                    break;

                case Lane.Middle:
                    CurrentLane = Lane.Bottom;
                    break;

                case Lane.Bottom:
                    // Do nothing as already in bottom lane
                    break;

                default:
                    Debug.LogError("Tenses_GameManager::ChangeLaneUp::Unhandled lane::" + CurrentLane.ToString());
                    break;
            }
        }

        /// <summary>
        /// Randomly selects and displays a new question and its answers.
        /// </summary>
        void ShowQuestion()
        {
            if (m_availableQuestions == null || m_availableQuestions.Length == 0)
            {
                Debug.LogError("Tenses_GameManager::ShowQuestion::No questions available!");
                return;
            }

            m_newPosition = m_answrParentRT.anchoredPosition;
            
            // If we've shown all questions, reset the shown questions list
            if (m_shownQuestions.Count >= m_availableQuestions.Length)
            {
                m_shownQuestions.Clear();
            }

            // Safety check to prevent infinite loop
            int maxAttempts = m_availableQuestions.Length;
            int attempts = 0;
            
            do
            {
                l_randomIndex = Random.Range(0, m_availableQuestions.Length);
                m_isQuestionShown = m_shownQuestions.Contains(l_randomIndex);
                attempts++;
                
                // Break if we've tried too many times
                if (attempts >= maxAttempts)
                {
                    Debug.LogWarning("Tenses_GameManager::ShowQuestion::Could not find unshown question after " + maxAttempts + " attempts");
                    m_shownQuestions.Clear();
                    break;
                }
            } while (m_isQuestionShown);

            m_shownQuestions.Add(l_randomIndex);
            Six_Tenses_Question m_question = m_availableQuestions[l_randomIndex];
            
            // Validate question data
            if (m_question == null || m_question.Options == null || m_question.Options.Length < 3)
            {
                Debug.LogError("Tenses_GameManager::ShowQuestion::Invalid question data!");
                return;
            }

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

        /// <summary>
        /// Hides the current question and resets answers.
        /// </summary>
        void HideQuestion()
        {
            ResetAnswers();
        }

        void OnDestroy()
        {
            // Clean up coroutine when object is destroyed
            if (m_scoringCheck != null)
            {
                StopCoroutine(m_scoringCheck);
                m_scoringCheck = null;
            }
        }
    }

    // Data structure for a single question
    [System.Serializable]
    public class Six_Tenses_Question
    {
        public string QuestionText;
        public Six_Tenses_Answer[] Options;
        public string Difficulty;
    }

    // Data structure for a single answer
    [System.Serializable]
    public class Six_Tenses_Answer
    {
        public string AnswerText;
        public bool IsCorrect;
    }

    // Enum for lane positions
    internal enum Lane
    {
        Top,
        Middle,
        Bottom
    }

    // Enum for game difficulty
    internal enum GameDifficuly
    {
        Easy,
        Medium,
        Hard
    }

    // Enum for game mode
    internal enum GameMode
    {
        Classroom,
        Unlimited
    }

    // Wrapper for deserializing questions from JSON
    [System.Serializable]
    public class QuestionsWrapper
    {
        public Six_Tenses_Question[] questions;
    }
}