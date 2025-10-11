using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutorialUIManager : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private GameObject tutorialPanel; // 튜토리얼 전체 패널
    [SerializeField] private TextMeshProUGUI tutorialText; // 텍스트를 표시할 TextMeshPro UI
    [SerializeField] private Image tutorialCharacter;

    [Header("애니메이션 설정")]
    [SerializeField] private float typingSpeed = 0.05f; // 한 글자가 나타나는 시간 (초)

    private Coroutine typingCoroutine; // 현재 실행 중인 타이핑 코루틴

    public bool IsTyping { get; private set; }

    private void Awake()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // 시작할 때 튜토리얼 패널을 비활성화합니다.
        if (tutorialPanel != null)
        {
            tutorialCharacter.gameObject.SetActive(false);
            tutorialPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 튜토리얼 메시지를 화면에 표시합니다.
    /// </summary>
    /// <param name="message">화면에 출력할 메시지 내용</param>
    public void ShowTutorialMessage(string message)
    {
        if (tutorialPanel == null || tutorialText == null)
        {
            Debug.LogError("튜토리얼 UI가 설정되지 않았습니다!");
            return;
        }
     
        tutorialPanel.SetActive(true);
        tutorialCharacter.gameObject.SetActive(true);

        StartTypingAnimation(message);
    }

    /// <summary>
    /// 현재 표시 중인 튜토리얼을 숨깁니다.
    /// </summary>
    public void HideTutorial()
    {
        if (tutorialPanel == null) return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        IsTyping = false;
        tutorialPanel.SetActive(false);
        tutorialCharacter.gameObject.SetActive(false);
    }

    /// <summary>
    /// 텍스트 타이핑 애니메이션을 시작합니다.
    /// </summary>
    private void StartTypingAnimation(string text)
    {
        // ▼▼▼ [수정] 코루틴 중지 로직 단순화 ▼▼▼
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeTextAnimation(text));
    }

    /// <summary>
    /// 한 글자씩 텍스트를 출력하는 코루틴입니다. (maxVisibleCharacters 사용)
    /// </summary>
    private IEnumerator TypeTextAnimation(string text)
    {
        IsTyping = true;

        tutorialText.text = text;
        tutorialText.maxVisibleCharacters = 0;

        while (tutorialText.maxVisibleCharacters < text.Length)
        {
            tutorialText.maxVisibleCharacters++;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        typingCoroutine = null;
        IsTyping = false;
    }
}