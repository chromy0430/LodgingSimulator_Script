using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // UI 클릭 감지를 위해 추가

public class NewTutorialGuide : MonoBehaviour
{
    [Header("튜토리얼 설정")]
    [SerializeField] private List<TutorialStep> tutorialSteps;
    [SerializeField] private TutorialUIManager uiManager;
    [SerializeField] private TutorialAnimationManager animationManager;
    [SerializeField] private InputManager inputManager; // BuildUI 상태 확인용
    [SerializeField] private ObjectPlacer objectPlacer; // 오브젝트 건설 확인용
    [SerializeField] private CameraCon cameraCon;

    [SerializeField] private int currentStepIndex = -1;
    private TutorialStep currentStep;
    private bool isWaitingForAction = false;
    private int initialObjectCount = 0; // 건설 감지를 위한 초기 오브젝트 수

    public bool isTutorialFinish = false;
    public static NewTutorialGuide Instance { get; private set; }

    // wait 함수 캐싱 
    WaitForSeconds waitSeconds = new WaitForSeconds(.001f);
    WaitForEndOfFrame waitFrame = new WaitForEndOfFrame();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    private void Start()
    {
        StartCoroutine(StartTutorialSequence());
    }

    private IEnumerator StartTutorialSequence()
    {
        yield return waitSeconds;
        if (!isTutorialFinish)
        {
            yield return waitFrame;
            Time.timeScale = 0f;
            ShowNextStep();
        }
        else
        {
            cameraCon.StartingMoveCamera();
            Destroy(this);
        }
    }

    private void Update()
    {
        if (!isWaitingForAction) return;
        CheckCurrentStepCompletion();
    }

    private void ShowNextStep()
    {
        isWaitingForAction = false;
        animationManager.HideAllGuides();

        currentStepIndex++;
        if (currentStepIndex >= tutorialSteps.Count)
        {
            EndTutorial();
            return;
        }

        currentStep = tutorialSteps[currentStepIndex];
        uiManager.ShowTutorialMessage(currentStep.dialogue);
        //uiManager.ShowTutorialMessage(currentStep.dialogue.GetLocalizedString());

        if (!string.IsNullOrEmpty(currentStep.guideAnimationName))
        {
            animationManager.ShowGuideAnimation(currentStep.guideAnimationName);
        }

        if (currentStep.triggerType == TutorialTriggerType.PlaceObject)
        {
            initialObjectCount = objectPlacer.placedGameObjects.Count;
        }

        isWaitingForAction = true;
    }    

    private void CheckCurrentStepCompletion()
    {
        if (currentStep == null) return;

        bool conditionMet = false;

        switch (currentStep.triggerType)
        {
            case TutorialTriggerType.Input:
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return))
                {
                    conditionMet = true;
                }
                break;
            case TutorialTriggerType.KeyPress:
                if (Input.GetKeyDown(currentStep.requiredKey))
                {
                    conditionMet = true;
                }
                break;
            case TutorialTriggerType.BuildUIOpen:
                if (inputManager.BuildUI.activeInHierarchy)
                {
                    conditionMet = true;
                }
                break;
            case TutorialTriggerType.BuildButtonClick:
                // 마우스 클릭 시, 클릭된 UI 오브젝트의 이름 확인
                if (Input.GetMouseButtonDown(0))
                {
                    GameObject clickedObject = EventSystem.current.currentSelectedGameObject;
                    if (clickedObject != null && clickedObject.name == currentStep.requiredButtonName)
                    {
                        conditionMet = true;
                    }
                }
                break;
            case TutorialTriggerType.PlaceObject:
                // 건설된 오브젝트 수가 증가했는지 확인
                if (objectPlacer.placedGameObjects.Count > initialObjectCount)
                {
                    // 마지막으로 추가된 오브젝트가 우리가 원하는 오브젝트인지 확인
                    // 이 방식은 완벽하지 않지만, PlacementSystem 수정 없이 가능합니다.
                    // 더 정확하게 하려면 ObjectPlacer에서 마지막에 배치된 오브젝트 정보를 가져와야 합니다.
                    conditionMet = true;
                }
                break;
        }

        if (conditionMet)
        {
            if (currentStep.triggerCameraMoveOnComplete)
            {
                if (cameraCon != null)
                {
                    cameraCon.StartingMoveCamera();
                }
                else
                {
                    Debug.LogError("NewTutorialGuide에 CameraCon이 연결되지 않았습니다!");
                }
            }

            ShowNextStep();
        }
    }

    private void EndTutorial()
    {
        isTutorialFinish = true;
        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame(); // 튜토리얼 끝난 후 강제 저장
        isWaitingForAction = false;
        uiManager.HideTutorial();
        animationManager.HideAllGuides();
        Time.timeScale = 1f;

        if (transform.parent != null && transform.parent.name == "TutorialUI")
        {
            Destroy(transform.parent.gameObject);
        }
        else
        {
            // 안전장치: TutorialUI 오브젝트가 없다면 이 스크립트가 포함된 오브젝트만 파괴
            Destroy(gameObject);
        }

        Debug.Log("튜토리얼 종료!");
        //gameObject.SetActive(false);
    }
}