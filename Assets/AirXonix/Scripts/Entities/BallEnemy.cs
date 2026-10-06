using UnityEngine;

namespace AirXonix
{
    /// <summary>
    /// Bounces around the open water. Kills the player on contact with the trail
    /// or with the player while carving. Its position also decides which regions
    /// stay open when a trail is closed.
    /// </summary>
    public class BallEnemy : MonoBehaviour
    {
        GameManager _gm;
        GridModel _g;
        GridView _view;
        float _hoverLift;

        Vector2 _pos;   // continuous cell space
        Vector2 _vel;   // cells / second

        public void Init(GameManager gm, GridModel g, GridView view, Vector2 startCell, Vector2 dir, float speed)
        {
            _gm = gm;
            _g = g;
            _view = view;

            var mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = PrimMesh.Get(PrimitiveType.Sphere);
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Standard")) { color = new Color(0.96f, 0.98f, 1f) };

            transform.localScale = Vector3.one * (_view.CellSize * 1.55f);
            _hoverLift = transform.localScale.y * 0.5f; // sphere's local half-height is 0.5

            _pos = startCell;
            if (dir.sqrMagnitude < 0.0001f) dir = new Vector2(1f, 1f);
            _vel = dir.normalized * speed;
            Sync();
        }

        public Vector2Int CellPos => new Vector2Int(Mathf.FloorToInt(_pos.x), Mathf.FloorToInt(_pos.y));

        public void SetSpeed(float s)
        {
            if (_vel.sqrMagnitude > 0.0001f) _vel = _vel.normalized * s;
        }

        public void NudgeToCenter()
        {
            var c = new Vector2(_g.Cols * 0.5f, _g.Rows * 0.5f);
            _pos = Vector2.Lerp(_pos, c, 0.4f);
            var cp = CellPos;
            if (!_g.InBounds(cp.x, cp.y) || _g.Get(cp.x, cp.y) != Cell.Empty) _pos = c;
            Sync();
        }

        void FixedUpdate()
        {
            if (_gm.State != GameState.Playing) return;

            float dt = Time.fixedDeltaTime;
            StepAxis(_vel.x * dt, true);
            StepAxis(_vel.y * dt, false);

            var c = CellPos;
            if (_g.InBounds(c.x, c.y) && _g.Get(c.x, c.y) == Cell.Trail)
            {
                _gm.OnPlayerHit();
                return;
            }
            if (_gm.Player != null && _gm.Player.IsCarving && _gm.Player.Pos == c)
            {
                _gm.OnPlayerHit();
                return;
            }

            Sync();
        }

        void StepAxis(float delta, bool axisX)
        {
            var np = _pos;
            if (axisX) np.x += delta; else np.y += delta;

            int cx = Mathf.FloorToInt(np.x);
            int cy = Mathf.FloorToInt(np.y);

            bool blocked = !_g.InBounds(cx, cy) || _g.Get(cx, cy) == Cell.Filled;
            if (blocked)
            {
                if (axisX) _vel.x = -_vel.x; else _vel.y = -_vel.y;
            }
            else
            {
                _pos = np;
            }
        }

        void Sync() => transform.position = _view.CellSpaceToWorld(_pos) + Vector3.up * _hoverLift;
    }
}
