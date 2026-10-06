using UnityEngine;

namespace AirXonix
{
    /// <summary>
    /// "Shoreline" enemy (appears from level 2). Wall-follows the boundary between
    /// claimed land and open water. Touching it, or letting it reach your trail,
    /// costs a life.
    /// </summary>
    public class PatrolEnemy : MonoBehaviour
    {
        const float CylinderLocalHalfHeight = 1f; // Unity's primitive cylinder is 2 units tall

        GameManager _gm;
        GridModel _g;
        GridView _view;
        float _hoverLift;

        Vector2Int _cell;
        Vector2Int _facing;
        float _interval;
        float _timer;
        bool _suppress;

        public void Init(GameManager gm, GridModel g, GridView view, float speed)
        {
            _gm = gm;
            _g = g;
            _view = view;
            _interval = 1f / Mathf.Max(1f, speed);

            var mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = PrimMesh.Get(PrimitiveType.Cylinder);
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Standard")) { color = new Color(1f, 0.32f, 0.85f) };

            // flattened "puck" shape: wide and short, not the default tall cylinder
            transform.localScale = new Vector3(1.8f, 0.3f, 1.8f) * _view.CellSize;
            _hoverLift = transform.localScale.y * CylinderLocalHalfHeight;

            _cell = FindStart();
            _facing = Vector2Int.right;
            Sync();
        }

        /// <summary>Walk a few steps at spawn so multiple patrols don't stack.</summary>
        public void Advance(int steps)
        {
            _suppress = true;
            for (int i = 0; i < steps && i < 4000; i++) Step();
            _suppress = false;
        }

        void Update()
        {
            if (_gm.State != GameState.Playing) return;

            _timer += Time.deltaTime;
            while (_timer >= _interval)
            {
                _timer -= _interval;
                Step();
            }
        }

        void Step()
        {
            // keep claimed land on the right: try right, forward, left, then back
            Vector2Int[] order = { RotCW(_facing), _facing, RotCCW(_facing), -_facing };
            foreach (var d in order)
            {
                var n = _cell + d;
                if (!_g.InBounds(n.x, n.y)) continue;

                var c = _g.Get(n.x, n.y);
                bool ok = (c == Cell.Empty && TouchesLand(n)) || c == Cell.Trail;
                if (!ok) continue;

                _cell = n;
                _facing = d;
                break;
            }

            if (_suppress) { Sync(); return; }

            if (_g.Get(_cell.x, _cell.y) == Cell.Trail) { _gm.OnPlayerHit(); return; }
            if (_gm.Player != null && _gm.Player.Pos == _cell) { _gm.OnPlayerHit(); return; }

            Sync();
        }

        bool TouchesLand(Vector2Int p) =>
            IsLand(p.x + 1, p.y) || IsLand(p.x - 1, p.y) ||
            IsLand(p.x, p.y + 1) || IsLand(p.x, p.y - 1);

        bool IsLand(int x, int y) => _g.InBounds(x, y) && _g.Get(x, y) == Cell.Filled;

        Vector2Int FindStart()
        {
            for (int y = _g.BorderThickness; y < _g.Rows - _g.BorderThickness; y++)
                for (int x = _g.BorderThickness; x < _g.Cols - _g.BorderThickness; x++)
                {
                    var p = new Vector2Int(x, y);
                    if (_g.Get(x, y) == Cell.Empty && TouchesLand(p)) return p;
                }
            return new Vector2Int(_g.BorderThickness, _g.BorderThickness);
        }

        void Sync()
        {
            transform.position = _view.CellToWorld(_cell.x, _cell.y) + Vector3.up * _hoverLift;
            transform.rotation = Quaternion.LookRotation(new Vector3(_facing.x, 0f, _facing.y), Vector3.up);
        }

        static Vector2Int RotCW(Vector2Int d) => new Vector2Int(d.y, -d.x);
        static Vector2Int RotCCW(Vector2Int d) => new Vector2Int(-d.y, d.x);
    }
}
