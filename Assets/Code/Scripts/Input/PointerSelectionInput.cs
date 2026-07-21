using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Junkineering.Input
{
    public class PointerSelectionInput : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private LayerMask selectionMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField] private bool treatUiAsMiss = true;

        private InputAction _pressAction;
        private InputAction _positionAction;
        private bool _acceptInput = true;
        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>(8);
        private EventSystem _cachedEventSystem;
        private PointerEventData _pointerEventData;

        public event Action<RaycastHit, Vector2> WorldHit;
        public event Action<Vector2> Missed;

        private void Awake()
        {
            ResolveCamera();

            _pressAction = new InputAction("Select", InputActionType.Button, "<Pointer>/press");
            _positionAction = new InputAction("PointerPosition", InputActionType.PassThrough, "<Pointer>/position");
        }

        private void Reset()
        {
            ResolveCamera();
        }

        private void OnEnable()
        {
            _pressAction.performed += OnPressPerformed;
            _positionAction.Enable();
            _pressAction.Enable();
        }

        private void OnDisable()
        {
            _pressAction.Disable();
            _positionAction.Disable();
            _pressAction.performed -= OnPressPerformed;
        }

        private void OnDestroy()
        {
            _pressAction?.Dispose();
            _positionAction?.Dispose();
        }

        public void SetInputEnabled(bool isEnabled)
        {
            _acceptInput = isEnabled;
        }

        private void OnPressPerformed(InputAction.CallbackContext context)
        {
            if (!_acceptInput)
            {
                return;
            }

            Vector2 screenPosition = _positionAction.ReadValue<Vector2>();
            if (treatUiAsMiss && IsPointerOverUi(screenPosition))
            {
                Missed?.Invoke(screenPosition);
                return;
            }

            ResolveCamera();
            if (worldCamera == null)
            {
                Debug.LogError("PointerSelectionInput could not find a camera.", this);
                Missed?.Invoke(screenPosition);
                return;
            }

            Ray ray = worldCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, selectionMask, triggerInteraction))
            {
                WorldHit?.Invoke(hit, screenPosition);
            }
            else
            {
                Missed?.Invoke(screenPosition);
            }
        }

        private void ResolveCamera()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        private bool IsPointerOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (_pointerEventData == null || _cachedEventSystem != eventSystem)
            {
                _cachedEventSystem = eventSystem;
                _pointerEventData = new PointerEventData(eventSystem);
            }

            _pointerEventData.position = screenPosition;
            _uiRaycastResults.Clear();
            eventSystem.RaycastAll(_pointerEventData, _uiRaycastResults);
            return _uiRaycastResults.Count > 0;
        }
    }
}
