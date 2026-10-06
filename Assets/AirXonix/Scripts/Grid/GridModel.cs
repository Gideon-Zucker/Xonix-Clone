namespace AirXonix
{
    public enum Cell : byte
    {
        Empty = 0,   // open "water" the player carves through
        Filled = 1,  // safe claimed land (also the outer border)
        Trail = 2    // the line the player is currently drawing
    }

    /// <summary>Pure data model of the play field. No Unity types, easy to test.</summary>
    public class GridModel
    {
        public readonly int Cols;
        public readonly int Rows;
        public int BorderThickness { get; private set; }
        public int InitialEmptyCount { get; private set; }

        readonly Cell[] _cells;

        public GridModel(int cols, int rows, int border)
        {
            Cols = cols;
            Rows = rows;
            BorderThickness = border;
            _cells = new Cell[cols * rows];
            Reset();
        }

        public Cell[] Raw => _cells;

        public void Reset()
        {
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                    _cells[Idx(x, y)] = IsBorder(x, y) ? Cell.Filled : Cell.Empty;

            InitialEmptyCount = CountEmpty();
        }

        public int Idx(int x, int y) => y * Cols + x;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Cols && y < Rows;

        public bool IsBorder(int x, int y) =>
            x < BorderThickness || y < BorderThickness ||
            x >= Cols - BorderThickness || y >= Rows - BorderThickness;

        public Cell Get(int x, int y) => _cells[Idx(x, y)];

        public void Set(int x, int y, Cell c) => _cells[Idx(x, y)] = c;

        public int CountEmpty()
        {
            int n = 0;
            for (int i = 0; i < _cells.Length; i++)
                if (_cells[i] == Cell.Empty) n++;
            return n;
        }

        /// <summary>0..1 fraction of the originally-open area that is now claimed.</summary>
        public float CapturedFraction()
        {
            if (InitialEmptyCount == 0) return 1f;
            return (InitialEmptyCount - CountEmpty()) / (float)InitialEmptyCount;
        }
    }
}
