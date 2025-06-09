using UnityEngine;
using UnityEngine.UI;

namespace Six_Tenses
{
    public class Six_Tenses_GameManager : MonoBehaviour
    {
        [Header("Scrolling Settings")]

        [SerializeField]
        float m_scrollSpeed = 1f;

        [SerializeField]
        Vector2 m_scrollDirection = Vector2.left;

        [SerializeField]
        bool m_autoStart = false;

        [SerializeField]
        Image m_roadImage = null;

        Material m_roadMaterial;
        Vector2 m_roadMatOffset;
        bool m_isScrolling;

        void Start()
        {
            if (m_roadImage != null)
            {
                m_roadMaterial = m_roadImage.material;
                // m_roadMaterial = new Material(m_roadImage.material);
                // m_roadImage.material = m_roadMaterial;
            }
            else
            {
                Debug.LogError("Tenses_GameManager::Start::No Image component found on road!");
                return;
            }

            // Initialize offset
            m_roadMatOffset = Vector2.zero;
            m_isScrolling = m_autoStart;
        }

        void Update()
        {
            if (!m_isScrolling)
            {
                return;
            }

            // Update the offset based on time and speed
            m_roadMatOffset += m_scrollDirection * m_scrollSpeed * Time.deltaTime;

            // Keep the offset values between 0 and 1 for seamless looping
            m_roadMatOffset.x = Mathf.Repeat(m_roadMatOffset.x, 1f);
            m_roadMatOffset.y = Mathf.Repeat(m_roadMatOffset.y, 1f);

            // Apply the offset to the material
            m_roadMaterial.mainTextureOffset = m_roadMatOffset;
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
    }
}