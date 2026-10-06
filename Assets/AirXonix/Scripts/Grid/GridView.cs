using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AirXonix
{
    /// <summary>
    /// Builds the whole field as one blocky "voxel" mesh: each cell is a flat-topped
    /// block at a height determined by its state (water/land/trail), with vertical
    /// cliff walls generated wherever a cell borders a lower neighbor (or the outer
    /// edge of the field). Owns the cell &lt;-&gt; world mapping every entity uses.
    /// </summary>
    public class GridView : MonoBehaviour
    {
        public Color emptyColor = new Color(0.05f, 0.35f, 0.62f);   // water
        public Color filledColor = new Color(0.86f, 0.73f, 0.45f);  // claimed land / border
        public Color trailColor = new Color(1f, 0.86f, 0.12f);      // active trail
        public Color cliffColor = new Color(0.32f, 0.24f, 0.15f);   // cliff walls

        public float waterHeight = -0.3f;
        public float trailHeight = 0.1f;
        public float filledHeight = 0.35f;

        public float CellSize { get; private set; } = 1f;
        public Vector3 Origin { get; private set; }
        public float MinHeight => waterHeight;
        public float MaxHeight => filledHeight;

        GridModel _grid;
        Mesh _mesh;
        bool _dirty;

        readonly List<Vector3> _verts = new List<Vector3>(4096);
        // submesh 0 = Empty top, 1 = Filled top, 2 = Trail top, 3 = cliff walls
        readonly List<int>[] _tris = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };

        public void Init(GridModel grid)
        {
            _grid = grid;

            var mf = gameObject.AddComponent<MeshFilter>();
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.materials = new[]
            {
                MakeMaterial(emptyColor),
                MakeMaterial(filledColor),
                MakeMaterial(trailColor),
                MakeMaterial(cliffColor),
            };

            _mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            _mesh.MarkDynamic();
            mf.sharedMesh = _mesh;

            transform.position = Vector3.zero;
            Origin = new Vector3(-grid.Cols * CellSize * 0.5f, 0f, -grid.Rows * CellSize * 0.5f);

            Redraw();
        }

        static Material MakeMaterial(Color c) => new Material(Shader.Find("Standard")) { color = c };

        float HeightForCell(Cell c)
        {
            switch (c)
            {
                case Cell.Filled: return filledHeight;
                case Cell.Trail: return trailHeight;
                default: return waterHeight;
            }
        }

        /// <summary>World position of the centre of cell (x, y), at that cell's surface height.</summary>
        public Vector3 CellToWorld(float x, float y) => CellSpaceToWorld(new Vector2(x + 0.5f, y + 0.5f));

        /// <summary>World position for a continuous cell-space point (integer part = cell index), at that cell's surface height.</summary>
        public Vector3 CellSpaceToWorld(Vector2 p)
        {
            int cx = Mathf.Clamp(Mathf.FloorToInt(p.x), 0, _grid.Cols - 1);
            int cy = Mathf.Clamp(Mathf.FloorToInt(p.y), 0, _grid.Rows - 1);
            float h = HeightForCell(_grid.Get(cx, cy));
            return Origin + new Vector3(p.x * CellSize, h, p.y * CellSize);
        }

        public void SetDirty() => _dirty = true;

        /// <summary>Hook for a future chunked-rebuild optimization; today every dirty mark rebuilds the whole field.</summary>
        public void SetDirty(int x, int y) => _dirty = true;

        void LateUpdate()
        {
            if (!_dirty) return;
            _dirty = false;
            Redraw();
        }

        void Redraw()
        {
            _verts.Clear();
            for (int i = 0; i < _tris.Length; i++) _tris[i].Clear();

            int cols = _grid.Cols, rows = _grid.Rows;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    var cell = _grid.Get(x, y);
                    float h = HeightForCell(cell);
                    float x0 = Origin.x + x * CellSize, x1 = x0 + CellSize;
                    float z0 = Origin.z + y * CellSize, z1 = z0 + CellSize;

                    AddQuad(_tris[(int)cell],
                        new Vector3(x0, h, z0), new Vector3(x0, h, z1),
                        new Vector3(x1, h, z1), new Vector3(x1, h, z0));

                    // internal boundary to the right, or the field's right edge
                    if (x + 1 < cols)
                    {
                        float hR = HeightForCell(_grid.Get(x + 1, y));
                        if (!Mathf.Approximately(h, hR))
                            AddWallX(x1, z0, z1, Mathf.Max(h, hR), Mathf.Min(h, hR), h > hR);
                    }
                    else if (h > waterHeight)
                    {
                        AddWallX(x1, z0, z1, h, waterHeight, true);
                    }

                    // field's left edge
                    if (x == 0 && h > waterHeight)
                        AddWallX(x0, z0, z1, h, waterHeight, false);

                    // internal boundary above, or the field's top edge
                    if (y + 1 < rows)
                    {
                        float hT = HeightForCell(_grid.Get(x, y + 1));
                        if (!Mathf.Approximately(h, hT))
                            AddWallZ(z1, x0, x1, Mathf.Max(h, hT), Mathf.Min(h, hT), h > hT);
                    }
                    else if (h > waterHeight)
                    {
                        AddWallZ(z1, x0, x1, h, waterHeight, true);
                    }

                    // field's bottom edge
                    if (y == 0 && h > waterHeight)
                        AddWallZ(z0, x0, x1, h, waterHeight, false);
                }
            }

            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.subMeshCount = 4;
            for (int i = 0; i < 4; i++) _mesh.SetTriangles(_tris[i], i);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        void AddQuad(List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = _verts.Count;
            _verts.Add(a); _verts.Add(b); _verts.Add(c); _verts.Add(d);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        /// <summary>Vertical wall in the X plane (fixed x, spanning z0..z1), from yBottom up to yTop.</summary>
        void AddWallX(float x, float z0, float z1, float yTop, float yBottom, bool normalPositiveX)
        {
            if (normalPositiveX)
                AddQuad(_tris[3],
                    new Vector3(x, yBottom, z0), new Vector3(x, yTop, z0),
                    new Vector3(x, yTop, z1), new Vector3(x, yBottom, z1));
            else
                AddQuad(_tris[3],
                    new Vector3(x, yBottom, z0), new Vector3(x, yBottom, z1),
                    new Vector3(x, yTop, z1), new Vector3(x, yTop, z0));
        }

        /// <summary>Vertical wall in the Z plane (fixed z, spanning x0..x1), from yBottom up to yTop.</summary>
        void AddWallZ(float z, float x0, float x1, float yTop, float yBottom, bool normalPositiveZ)
        {
            if (normalPositiveZ)
                AddQuad(_tris[3],
                    new Vector3(x0, yBottom, z), new Vector3(x1, yBottom, z),
                    new Vector3(x1, yTop, z), new Vector3(x0, yTop, z));
            else
                AddQuad(_tris[3],
                    new Vector3(x0, yBottom, z), new Vector3(x0, yTop, z),
                    new Vector3(x1, yTop, z), new Vector3(x1, yBottom, z));
        }
    }
}
