using System.Collections.Generic;
using UnityEngine;

namespace AirXonix
{
    /// <summary>
    /// Grid-locked marker. Moves continuously in the current direction; carves a
    /// trail while in open water and triggers a capture when it returns to land.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public float stepInterval = 0.045f; // seconds per cell

        public Vector2Int Pos { get; private set; }
        public bool IsCarving { get; private set; }
        public List<Vector2Int> TrailCells { get; } = new List<Vector2Int>();

        GameManager _gm;
        GridModel _g;
        GridView _view;
        float _hoverLift;

        Vector2Int _dir;
        Vector2Int _queuedDir;
        float _timer;

        public void Init(GameManager gm, GridModel g, GridView view, Vector2Int start)
        {
            _gm = gm;
            _g = g;
            _view = view;

            var mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = PrimMesh.Get(PrimitiveType.Cube);
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Standard")) { color = new Color(1f, 0.28f, 0.2f) };

            transform.localScale = Vector3.one * (_view.CellSize * 1.7f);
            _hoverLift = transform.localScale.y * 0.5f; // cube's local half-height is 0.5

            PlaceAt(start);
        }

        public void PlaceAt(Vector2Int c)
        {
            Pos = c;
            transform.position = _view.CellToWorld(c.x, c.y) + Vector3.up * _hoverLift;
        }

        public void SetDirection(Vector2Int d)
        {
            if (d == Vector2Int.zero) return;
            if (IsCarving && d == -_dir) return; // can't reverse into our own trail
            _queuedDir = d;
        }

        public void ResetAfterDeath(Vector2Int start)
        {
            ClearTrail(revert: true);
            IsCarving = false;
            _dir = Vector2Int.zero;
            _queuedDir = Vector2Int.zero;
            _timer = 0f;
            PlaceAt(start);
        }

        public void ClearTrail(bool revert)
        {
            if (revert)
            {
                foreach (var c in TrailCells)
                    if (_g.Get(c.x, c.y) == Cell.Trail)
                        _g.Set(c.x, c.y, Cell.Empty);
            }
            TrailCells.Clear();
        }

        void Update()
        {
            if (_gm.State != GameState.Playing) return;

            _timer += Time.deltaTime;
            if (_timer < stepInterval) return;
            _timer -= stepInterval;
            Step();
        }

        void Step()
        {
            if (_queuedDir != Vector2Int.zero && !(IsCarving && _queuedDir == -_dir))
            {
                var t = Pos + _queuedDir;
                if (_g.InBounds(t.x, t.y)) _dir = _queuedDir;
            }

            if (_dir == Vector2Int.zero) return;

            var next = Pos + _dir;
            if (!_g.InBounds(next.x, next.y))
            {
                _dir = Vector2Int.zero; // stop at the edge; wait for new input
                return;
            }

            var target = _g.Get(next.x, next.y);
            if (target == Cell.Trail)
            {
                _gm.OnPlayerHit(); // ran into our own line
                return;
            }

            Pos = next;
            transform.position = _view.CellToWorld(Pos.x, Pos.y) + Vector3.up * _hoverLift;
            transform.rotation = Quaternion.LookRotation(new Vector3(_dir.x, 0f, _dir.y), Vector3.up);

            if (target == Cell.Empty)
            {
                IsCarving = true;
                _g.Set(Pos.x, Pos.y, Cell.Trail);
                TrailCells.Add(Pos);
                _view.SetDirty(Pos.x, Pos.y);
            }
            else if (target == Cell.Filled && IsCarving)
            {
                IsCarving = false;
                _dir = Vector2Int.zero;
                _queuedDir = Vector2Int.zero;
                _gm.OnTrailClosed();
            }
        }
    }
}
