using UnityEngine;
using UnityEngine.InputSystem;
namespace AdrianWez
{
    namespace Controller
    {
        public class RTS : MonoBehaviour
        {
            // schemes map
            private CameraActions _cameraActions;
            // actions
            private InputAction _movement;
            private InputAction _rotation;
            private InputAction _acceleration;
            private InputAction _zoom;
            private InputAction _selection;
            
            private Camera _mainCamera;

            [Header("Movement")]
            [SerializeField] private float _baseSpeed = 1;
            [SerializeField] private float _fastSpeed = 5;
            private float _moveSpeed;
            [Tooltip("Values to clamp camera movement on the horizontal axis (min),(max)")]
            [SerializeField] private Vector2 _xClamp = new();
            [Tooltip("Values to clamp camera movement on the vertical axis (min),(max)")]
            [SerializeField] private Vector2 _zClamp = new();

            [Header("Zoom")]
            [SerializeField] private float _zoomStep = 5;
            [SerializeField] private float _zoomLimit = 20;
            
            [Header("Smoothness")]
            [SerializeField] private float _motionSpeed = 5;

            //value set in various functions 
            //used to update the position of the rig.
            private Vector3 _newPosition;
            private Quaternion _newRotation;
            private Vector3 _newZoom;


            // ai bots
            [Header("Clicking")]
            [SerializeField] private float _clickRayDistance = 500f;
            private AI.AICompanion _selected;

            private void Awake()
            {
                _cameraActions = new CameraActions();
                _mainCamera = GetComponentInChildren<Camera>();
            }

            private void OnEnable()
            {

                _movement = _cameraActions.RTS.Move;
                _rotation = _cameraActions.RTS.Rotate;
                _zoom = _cameraActions.RTS.Zoom;
                _selection = _cameraActions.RTS.Select;
                _acceleration = _cameraActions.RTS.Acceleration;
                //
                _cameraActions.RTS.Select.performed += Select;
                //
                _cameraActions.RTS.Enable();
            }

            private void OnDisable()
            {
                _cameraActions.RTS.Select.performed -= Select;
                _cameraActions.RTS.Disable();
            }

            private void Start()
            {
                _newPosition = transform.position;
                _newRotation = transform.rotation;
                _newZoom = _mainCamera.transform.localPosition;
            }

            private void Update()
            {
                // move speed
                _moveSpeed = _acceleration.IsInProgress()? _fastSpeed : _baseSpeed;
                
                // movement
                _newPosition += _moveSpeed * (transform.right * _movement.ReadValue<Vector2>().x + transform.forward * _movement.ReadValue<Vector2>().y);
                _newPosition.x = Mathf.Clamp(_newPosition.x, _xClamp.x, _xClamp.y);
                _newPosition.z = Mathf.Clamp(_newPosition.z, _zClamp.x, _zClamp.y);
                transform.position = Vector3.Lerp(transform.position, _newPosition, Time.deltaTime * _motionSpeed);

                // rotation
                _newRotation *= Quaternion.Euler(Vector3.up * _rotation.ReadValue<float>());
                transform.rotation = Quaternion.Lerp(transform.rotation, _newRotation, Time.deltaTime * _motionSpeed);
                
                // zoom
                _newZoom += new Vector3(0,-_zoomStep, _zoomStep) * _zoom.ReadValue<float>();
                _newZoom.y = Mathf.Clamp(_newZoom.y, 0, _zoomLimit);
                _newZoom.z = Mathf.Clamp(_newZoom.z, -_zoomLimit, 0);
                _mainCamera.transform.localPosition = Vector3.Lerp(_mainCamera.transform.localPosition, _newZoom, Time.deltaTime * _motionSpeed);
            }

            // entity selection
            private void Select(InputAction.CallbackContext _context)
            {
                Ray _ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if(Physics.Raycast(_ray, out RaycastHit _hit, _clickRayDistance))
                {
                    // check entity
                    Entity _entity = _hit.collider.GetComponent<Entity>();
                    if(_entity == null) UI_Entity._instance.DisableUI();
                    else UI_Entity._instance.DisplayEntity(_entity._entityProps);

                    // check AI
                    if(_hit.collider.GetComponent<AI.AICompanion>())
                    {
                        _selected = _hit.collider.GetComponent<AI.AICompanion>();
                        UI_AI._instance.DisplayAI(_selected);
                    }
                }else
                    UI_Entity._instance.DisableUI();
            }
            
            // controlling AIs
            private void DeselectAI(InputAction.CallbackContext _context)
            {
                _selected = null;
            }
            private void MoveCharacter(InputAction.CallbackContext _context)
            {
                if(_selected == null) return;
                else
                {
                    Ray _ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                    if(Physics.Raycast(_ray, out RaycastHit _hit, _clickRayDistance))
                    {
                        _selected.Move(_hit.point);
                    }
                }
            }
        }
    }
}