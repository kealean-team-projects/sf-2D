using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class CharacterSquashOnClick : MonoBehaviour
{
    [Header("눌림 모양 (원래 크기에 곱하는 배율)")]
    [SerializeField] private Vector3 squashMultiplier = new(1.107f, 0.1f, 1f); // 가로로 퍼지고 세로로 눌림
    [SerializeField] private float squashTime = 0.1f;
    [SerializeField] private float recoverTime = 0.3f;

    [Header("입력 차단")]
    [Tooltip("시간이 멈춘 상태(일시정지 메뉴 등)에서는 반응하지 않음")]
    [SerializeField] private bool ignoreWhenPaused = true;
    [Tooltip("클릭 판정에 쓸 카메라 (비우면 Main Camera)")]
    [SerializeField] private Camera targetCamera;

    private Collider2D _collider;
    private Vector3 _baseScale;
    private Sequence _squashSequence;

    private static readonly List<RaycastResult> UiHits = new();
    private static PointerEventData _pointerData;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        if (_squashSequence.isAlive) _squashSequence.Stop();
        transform.localScale = _baseScale;
    }

    private void Update()
    {
        var pointer = Pointer.current; 
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;
        if (ignoreWhenPaused && Time.timeScale <= 0f) return;

        var screenPos = pointer.position.ReadValue();
        if (IsUiBlockingInput(screenPos)) return;
        if (!IsPointerOnMe(screenPos)) return;

        PlaySquash();
    }

    private static bool IsUiBlockingInput(Vector2 screenPos)
    {
        var es = EventSystem.current;
        if (es == null) return false;

        _pointerData ??= new PointerEventData(es);
        _pointerData.Reset();
        _pointerData.position = screenPos;

        UiHits.Clear();
        es.RaycastAll(_pointerData, UiHits);
        return UiHits.Count > 0;
    }

    private bool IsPointerOnMe(Vector2 screenPos)
    {
        var cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return false;

        var ray = cam.ScreenPointToRay(screenPos);
        var hits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            return hit.collider == _collider || hit.collider.transform.IsChildOf(transform);
        }
        return false;
    }

    private void PlaySquash()
    {
        if (_squashSequence.isAlive) _squashSequence.Stop();
        transform.localScale = _baseScale;

        var squashed = Vector3.Scale(_baseScale, squashMultiplier);
        _squashSequence = Sequence.Create()
            .Chain(Tween.Scale(transform, squashed, squashTime, Ease.OutBack))
            .Chain(Tween.Scale(transform, _baseScale, recoverTime, Ease.OutBounce));
    }
}