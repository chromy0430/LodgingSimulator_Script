using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    [Header("상호작용 시스템")]
    [SerializeField] private ObjectInteractionUI interactionUI;

    [Header("컴포넌트")]
    [SerializeField] private PlacementSystem placementSystem;
    [SerializeField] private Camera cam;
    [SerializeField] private Grid grid;
    [SerializeField] private ChangeFloorSystem changeFloorSystem;
    
    private Vector3 lastPosition;   // 마지막 좌표 변수
    private Vector3 uiShowPosition; // BuildUI가 보이는 위치
    private Vector3 uiHidePosition; // BuildUI가 숨겨진 위치
    private Tween   uiTween; // 현재 실행 중인 트윈 저장

    [Header("변수")]
    
    [SerializeField] private LayerMask placementLayermask;
    [SerializeField] private LayerMask batchedLayer;
    [SerializeField] private LayerMask objectLayer;
    public event Action OnClicked, OnExit;

    public GameObject BuildUI;
    public GameObject SettingUI;
    public GameObject SettingUI2;
    public GameObject targetObject;
    public GameObject QuestUI;
    public Button     SettingBtn;

    public RaycastHit   hit;
    public RaycastHit   hit2; 
    public bool         isBuildMode = false;
    public bool isDeleteMode = false;
    public bool IsPointerOverUI() => EventSystem.current.IsPointerOverGameObject();

    [Header("DOTween Settings")]
    public float animationDuration = 0.3f; // 애니메이션 지속 시간
    public Ease openEase = Ease.OutBack;    // 열릴 때 적용할 Ease
    public Ease closeEase = Ease.InBack;     // 닫힐 때 적용할 Ease

    private void Start()
    {
        InitialBuildUI();

        SettingBtn.onClick.AddListener(OnOffSettingUI);
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Time.timeScale = 0.1f;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Time.timeScale = 0.3f;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Time.timeScale = 0.5f;
        }

        // B키로 건설모드 시작
        if (Input.GetKeyDown(KeyCode.B) && !IsPointerOverUI())
        {
            ChangeBuildMode();
        }

        // Q키로 퀘스트 UI 출력
        if (Input.GetKeyDown(KeyCode.Q)) QuestUI.SetActive(!QuestUI.activeSelf);

        // ESC키로 설정 UI 출력
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            OnOffSettingUI();
        }

        // 마우스 우클릭으로 건설 중지
        if (Input.GetMouseButtonDown(1) && isBuildMode && !IsPointerOverUI())
        {
            OnExit?.Invoke();
        }

        // 마우스 좌클릭으로 건설
        if (Input.GetMouseButtonDown(0) && isDeleteMode)
        {
            OnClicked?.Invoke();
        }

        // ESC 키로 건설 상태 종료
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isBuildMode)
            {
                isBuildMode = false;
                placementSystem.ExitBuildMode();
                HideBuildUI();
            }
            OnExit?.Invoke();
        }

        // 삭제모드 시작
        if (Input.GetKeyDown(KeyCode.L) && isBuildMode)
        {
            ChangeDeleteMode();
        }

        // 일반 모드에서 오브젝트 클릭 처리
        if (Input.GetMouseButtonDown(0) && !isBuildMode && !isDeleteMode && !IsPointerOverUI())
        {
            HandleObjectSelection();
        }
    }

    private void ChangeDeleteMode()
    {
        if (isDeleteMode)
            placementSystem.StopDeleteMode();
        else
            placementSystem.StartDeleteMode();
    }

    private void ChangeBuildMode()
    {
        isBuildMode = !isBuildMode;
        if (isBuildMode)
        {
            ShowBuildUI(); // BuildUI 애니메이션 실행
        }
        else
        {
            HideBuildUI(); // BuildUI 애니메이션 실행
            placementSystem.ExitBuildMode();
            OnExit?.Invoke();
        }

        ChangeFloorForBuildMode();
    }

    private void OnOffSettingUI()
    {
        // UI가 비활성화 상태일 때 -> 열기
        if (!SettingUI.activeSelf)
        {
            // SettingUI2가 켜져 있다면 먼저 끈다.
            if (SettingUI2 != null && SettingUI2.activeSelf)
            {
                SettingUI2.SetActive(false);
            }

            // UI를 활성화하고, 시작 크기를 설정한 후 애니메이션 실행
            SettingUI.SetActive(true);
            SettingUI.transform.localScale = Vector3.one * 0.1f;
            SettingUI.transform.DOScale(1f, animationDuration).SetEase(openEase);
        }
        // UI가 활성화 상태일 때 -> 닫기
        else
        {
            // 크기 애니메이션을 먼저 실행하고, 애니메이션이 끝나면 비활성화
            SettingUI.transform.DOScale(0.1f, animationDuration)
                .SetEase(closeEase)
                .OnComplete(() =>
                {
                    SettingUI.SetActive(false);
                    SettingUI2.SetActive(false);
                });
        }

        //SettingUI.SetActive(!SettingUI.activeSelf);
        //if(SettingUI2.activeSelf) SettingUI2.SetActive(false);
    }

    private void HandleObjectSelection()
    {
        if (interactionUI is not null && interactionUI.IsUIActive())
        {
            return; // UI가 열려 있으면 다른 오브젝트 선택 무시
        }
        
        GameObject clickedObject = GetClickedObject();
        
        if (clickedObject != null)
        {
            Debug.Log($"{clickedObject.name}을 클릭");

            // ObjectPlacer null 체크 추가
            ObjectPlacer objectPlacer = FindFirstObjectByType<ObjectPlacer>();
            if (objectPlacer == null)
            {
                Debug.LogError("ObjectPlacer를 찾을 수 없습니다!");
                return;
            }

            // 설치된 오브젝트인지 확인
            int objectIndex = objectPlacer.GetObjectIndex(clickedObject);
            Debug.Log($"{clickedObject.name}의 objectIndex는 {objectIndex}");
            if (objectIndex >= 0)
            {
                Vector3 objectPosition = clickedObject.transform.position;
                Debug.Log($"{clickedObject.name}는 {objectPosition}에 위치");

                // interactionUI null 체크 추가
                if (interactionUI != null)
                {
                    interactionUI.ShowInteractionUI(clickedObject, objectPosition);
                }
                else
                {
                    Debug.LogError("InteractionUI가 할당되지 않았습니다! Inspector에서 할당해주세요.");
                }
            }
        }
        else
        {
            // 빈 공간 클릭 시 UI 숨김
            //if (interactionUI != null && interactionUI.IsUIActive()) interactionUI.HideInteractionUI();
            
            if(clickedObject != null)  interactionUI.HideInteractionUI();
        }
    }

    private void InitialBuildUI()
    {
        // BuildUI의 초기 위치 설정
        if (BuildUI is not null)
        {
            // BuildUI의 RectTransform 사용
            RectTransform uiRect = BuildUI.GetComponent<RectTransform>();
            if (uiRect is not null)
            {
                // 현재 위치를 보이는 위치로 설정
                uiShowPosition = uiRect.anchoredPosition;

                // 숨겨진 위치는 Y축을 아래로 이동 (화면 아래로)
                uiHidePosition = uiShowPosition + new Vector3(0, -Screen.height, 0); // 화면 높이만큼 아래로

                // 초기 상태: BuildUI 숨김
                uiRect.anchoredPosition = uiHidePosition;
                BuildUI.SetActive(false); // 비활성화 대신 위치로 제어
            }
            else
            {
                Debug.LogError("BuildUI에 RectTransform이 없습니다!");
            }
        }
        else
        {
            Debug.LogError("BuildUI가 할당되지 않았습니다!");
        }

        if(SettingUI is not null) SettingUI.SetActive(false);
    }

    private void ActiveInputHelper()
    {
        if (targetObject == null)
        {
            Debug.LogError("Target Object가 할당되지 않았습니다!");
            return;
        }

        if (Input.GetKey(KeyCode.F1))
        {
            // 타겟 게임 오브젝트를 활성화시킵니다.
            targetObject.SetActive(true);
        }
        // F1 키에서 손을 떼는 순간 true를 반환합니다.
        else if (Input.GetKeyUp(KeyCode.F1))
        {
            // 타겟 게임 오브젝트를 비활성화시킵니다.
            targetObject.SetActive(false);
        }
    }

    /// <summary>
    /// BuildUI를 위로 올리는 Dotween 애니메이션 코드
    /// </summary>
    private void ShowBuildUI()
    {
        if (BuildUI is null) return;

        // 기존 트윈이 있으면 종료
        if (uiTween is not null)
        {
            uiTween.Kill();
        }

        RectTransform uiRect = BuildUI.GetComponent<RectTransform>();
        if (uiRect is not null)
        {
            placementSystem.EnterBuildMode();
            // DOTween으로 Y축 이동 애니메이션
            uiTween = uiRect.DOAnchorPosY(uiShowPosition.y, 0.5f) // 0.5초 동안 이동
                .SetEase(Ease.OutQuad) // 부드러운 이징
                .SetUpdate(UpdateType.Normal, true) // 타임스케일 영향 X
                .OnComplete(() => uiTween = null); // 완료 시 트윈 변수 초기화
        }
    }

    /// <summary>
    /// BuildUI를 아래로 내리는 Dotween 애니메이션 코드
    /// </summary>
    private void HideBuildUI()
    {
        if (BuildUI is null) return;

        // 기존 트윈이 있으면 종료
        if (uiTween is not null)
        {
            uiTween.Kill();
        }

        RectTransform uiRect = BuildUI.GetComponent<RectTransform>();
        if (uiRect is not null)
        {
            // DOTween으로 Y축 이동 애니메이션
            uiTween = uiRect.DOAnchorPosY(uiHidePosition.y, 0.5f) // 0.5초 동안 이동
                .SetEase(Ease.InQuad) // 부드러운 이징
                .SetUpdate(UpdateType.Normal, true) // 타임스케일 영향 X
                .OnComplete(() =>
                {
                    uiTween = null;
                    //placementSystem.ExitBuildMode();
                }); // 완료 시 트윈 변수 초기화
        }
    }

    /// <summary>
    /// 마우스를 통해 실시간으로 좌표를 반환한다.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetSelectedMapPosition()
    {
        if (cam is null)
        {
            Debug.LogError("Camera가 할당되지 않았습니다!");
            return Vector3.zero;
        }
        
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = cam.nearClipPlane;
        
        Ray ray = cam.ScreenPointToRay(mousePos);
        if (Physics.Raycast(ray, out hit, 90, placementLayermask))
        {
            lastPosition = hit.point;
        }
        
        return lastPosition;
    }

    public GameObject GetClickedObject()
    {
        if (cam is null)
        {
            Debug.LogError("Camera가 할당되지 않았습니다!");
            return null;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit2, 100f, objectLayer))
        {
            // 클릭한 오브젝트의 루트 오브젝트 반환
            GameObject clickedObject = hit2.collider.gameObject;
            //Debug.Log($"선택된 오브젝트 : {clickedObject}");
            return clickedObject.transform.root.gameObject;
        }

        return null;
    }

    private void ChangeFloorForBuildMode()
    {
        if(placementSystem.GetFloorLock()) changeFloorSystem.OnBuildModeChanged();
    }    
}
