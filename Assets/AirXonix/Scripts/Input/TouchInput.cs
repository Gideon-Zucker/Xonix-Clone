using UnityEngine;
using UnityEngine.EventSystems;

namespace AirXonix
{
    /// <summary>Swipe (mobile) + arrow/WASD (editor) steering. The on-screen D-pad
    /// in <see cref="GameHud"/> feeds the same <see cref="GameManager.OnDirectionInput"/>.</summary>
    public class TouchInput : MonoBehaviour
    {
        public float swipePixels = 45f;

        GameManager _gm;
        Vector2 _start;
        bool _tracking;
        bool _consumed;

        public void Init(GameManager gm) => _gm = gm;

        void Update()
        {
            if (_gm == null) return;

            HandleKeyboard();

            if (_gm.State != GameState.Playing) { _tracking = false; return; }
            HandlePointer();
        }

        void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) _gm.OnDirectionInput(Vector2Int.up);
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) _gm.OnDirectionInput(Vector2Int.down);
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) _gm.OnDirectionInput(Vector2Int.left);
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) _gm.OnDirectionInput(Vector2Int.right);
        }

        void HandlePointer()
        {
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                switch (t.phase)
                {
                    case TouchPhase.Began:
                        BeginTrack(t.position, t.fingerId);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (_tracking && !_consumed) TrySwipe(t.position);
                        break;
                    default:
                        if (_tracking && !_consumed) TrySwipe(t.position);
                        _tracking = false;
                        break;
                }
                return;
            }

            if (Input.GetMouseButtonDown(0)) BeginTrack(Input.mousePosition, -1);
            else if (Input.GetMouseButton(0) && _tracking && !_consumed) TrySwipe(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0)) _tracking = false;
        }

        void BeginTrack(Vector2 pos, int fingerId)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId))
            {
                _tracking = false; // let the D-pad / buttons handle it
                return;
            }
            _start = pos;
            _tracking = true;
            _consumed = false;
        }

        void TrySwipe(Vector2 pos)
        {
            var d = pos - _start;
            if (d.magnitude < swipePixels) return;

            if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
                _gm.OnDirectionInput(d.x > 0 ? Vector2Int.right : Vector2Int.left);
            else
                _gm.OnDirectionInput(d.y > 0 ? Vector2Int.up : Vector2Int.down);

            _consumed = true;
            _start = pos; // allow a follow-up flick without lifting
        }
    }
}
