using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AirXonix
{
    public enum GameState { Menu, Playing, Paused, LifeLost, LevelComplete, GameOver }

    /// <summary>
    /// Owns the model, spawns the view / player / enemies / HUD, and drives the
    /// game loop. The whole game is built in code so the project runs on Play with
    /// nothing to wire up in the inspector.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Field")]
        public int cols = 112;
        public int rows = 72;
        public int border = 2;
        [Range(0.5f, 0.95f)] public float targetFraction = 0.75f;

        [Header("Progression")]
        public int startLives = 3;
        public int baseBalls = 2;
        public int ballsPerLevel = 1;
        public float baseBallSpeed = 14f;
        public float ballSpeedPerLevel = 2f;
        public float maxBallSpeed = 30f;
        public int patrolFromLevel = 2;

        [Header("Camera")]
        public float cameraTiltDegrees = 55f;
        public float cameraPanSlack = 1.10f;   // orthographic size beyond exact field fit, for pan room
        public float cameraFollowSpeed = 4f;   // higher = snappier tracking
        public float cameraDistance = 80f;

        public GameState State { get; private set; } = GameState.Menu;
        public PlayerController Player { get; private set; }
        public int Level { get; private set; }
        public int Score { get; private set; }
        public int Lives { get; private set; }

        GridModel _grid;
        GridView _view;
        GameHud _hud;
        Sfx _sfx;
        Camera _cam;

        readonly List<BallEnemy> _balls = new List<BallEnemy>();
        readonly List<PatrolEnemy> _patrols = new List<PatrolEnemy>();

        Vector2Int _startCell;
        int _lastW, _lastH;

        Vector3 _fieldCenter;
        Vector3 _camCenter;
        float _panRoomUp, _panRoomRight;

        void Start()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            _cam = Camera.main;
            if (_cam != null && _cam.GetComponent<AudioListener>() == null)
                _cam.gameObject.AddComponent<AudioListener>();

            _grid = new GridModel(cols, rows, border);

            _view = new GameObject("Grid").AddComponent<GridView>();
            _view.Init(_grid);

            _startCell = new Vector2Int(cols / 2, Mathf.Max(0, border - 1));

            Player = new GameObject("Player").AddComponent<PlayerController>();
            Player.Init(this, _grid, _view, _startCell);

            _sfx = gameObject.AddComponent<Sfx>();
            _sfx.Init();

            _hud = new GameObject("HUD").AddComponent<GameHud>();
            _hud.Init(this);

            gameObject.AddComponent<TouchInput>().Init(this);

            FitCamera();
            _camCenter = _fieldCenter;
            TrackCamera();
            _hud.ShowMenu();
        }

        void Update()
        {
            if (Screen.width != _lastW || Screen.height != _lastH) FitCamera();
            TrackCamera();

            if (State == GameState.Playing)
            {
                float frac = _grid.CapturedFraction();
                _hud.SetStats(Level, Score, Lives, frac, targetFraction);
                if (frac >= targetFraction) StartCoroutine(LevelCompleteRoutine());
            }

            if (Input.GetKeyDown(KeyCode.Escape) &&
                (State == GameState.Playing || State == GameState.Paused))
                TogglePause();
        }

        // ---------------------------------------------------------------- flow

        public void StartGame()
        {
            Level = 1;
            Score = 0;
            Lives = startLives;
            BuildLevel();
        }

        public void RestartToMenu()
        {
            StopAllCoroutines();
            ClearEnemies();
            _grid.Reset();
            Player.ResetAfterDeath(_startCell);
            _view.SetDirty();
            State = GameState.Menu;
            _hud.ShowMenu();
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) { State = GameState.Paused; _hud.ShowPause(); }
            else if (State == GameState.Paused) { State = GameState.Playing; _hud.ShowPlaying(); }
        }

        void BuildLevel()
        {
            StopAllCoroutines();
            _grid.Reset();
            ClearEnemies();
            Player.ResetAfterDeath(_startCell);

            int ballCount = baseBalls + (Level - 1) * ballsPerLevel;
            float speed = Mathf.Min(maxBallSpeed, baseBallSpeed + (Level - 1) * ballSpeedPerLevel);
            for (int i = 0; i < ballCount; i++) SpawnBall(speed);

            if (Level >= patrolFromLevel)
            {
                int patrols = 1 + (Level - patrolFromLevel) / 2;
                for (int i = 0; i < patrols; i++) SpawnPatrol(speed * 0.55f, 30 + i * 50);
            }

            _view.SetDirty();
            _hud.SetStats(Level, Score, Lives, 0f, targetFraction);
            _hud.ShowPlaying();
            _hud.FlashMessage("LEVEL " + Level, 1.1f);
            State = GameState.Playing;
        }

        // ------------------------------------------------------------- entities

        void SpawnBall(float speed)
        {
            var b = new GameObject("Ball").AddComponent<BallEnemy>();
            var cell = RandomInnerCell(6);
            var dir = new Vector2(Random.value < 0.5f ? -1f : 1f, Random.value < 0.5f ? -1f : 1f);
            b.Init(this, _grid, _view, cell, dir, speed);
            _balls.Add(b);
        }

        void SpawnPatrol(float speed, int advance)
        {
            var p = new GameObject("Patrol").AddComponent<PatrolEnemy>();
            p.Init(this, _grid, _view, speed);
            p.Advance(advance);
            _patrols.Add(p);
        }

        Vector2 RandomInnerCell(int margin)
        {
            int x = Random.Range(border + margin, cols - border - margin);
            int y = Random.Range(border + margin, rows - border - margin);
            return new Vector2(x + 0.5f, y + 0.5f);
        }

        void ClearEnemies()
        {
            foreach (var b in _balls) if (b != null) Destroy(b.gameObject);
            foreach (var p in _patrols) if (p != null) Destroy(p.gameObject);
            _balls.Clear();
            _patrols.Clear();
        }

        // -------------------------------------------------- events from entities

        public void OnDirectionInput(Vector2Int dir)
        {
            if (State == GameState.Playing) Player.SetDirection(dir);
        }

        public void OnTrailClosed()
        {
            var cells = new List<Vector2Int>();
            foreach (var b in _balls) if (b != null) cells.Add(b.CellPos);

            int filled = CaptureSolver.Resolve(_grid, cells);
            Player.ClearTrail(false);
            _view.SetDirty();

            if (filled > 0)
            {
                Score += filled * 5 * Level;
                _sfx.Capture();
            }
            _hud.SetStats(Level, Score, Lives, _grid.CapturedFraction(), targetFraction);
        }

        public void OnPlayerHit()
        {
            if (State != GameState.Playing) return;

            Lives--;
            _sfx.Death();
            _hud.SetStats(Level, Score, Lives, _grid.CapturedFraction(), targetFraction);

            if (Lives <= 0)
            {
                State = GameState.GameOver;
                ClearEnemies();
                _hud.ShowGameOver(Score);
                return;
            }

            State = GameState.LifeLost;
            StartCoroutine(LifeLostRoutine());
        }

        IEnumerator LifeLostRoutine()
        {
            _hud.FlashMessage("LIFE LOST", 1.0f);
            yield return new WaitForSeconds(1.0f);

            Player.ResetAfterDeath(_startCell);
            foreach (var b in _balls) if (b != null) b.NudgeToCenter();
            _view.SetDirty();
            State = GameState.Playing;
        }

        IEnumerator LevelCompleteRoutine()
        {
            if (State != GameState.Playing) yield break;
            State = GameState.LevelComplete;

            int bonus = 250 * Level;
            Score += bonus;
            _sfx.Level();
            _hud.SetStats(Level, Score, Lives, _grid.CapturedFraction(), targetFraction);
            _hud.FlashMessage("LEVEL CLEAR   +" + bonus, 1.7f);

            yield return new WaitForSeconds(1.8f);

            Level++;
            BuildLevel();
        }

        // ---------------------------------------------------------------- camera

        void FitCamera()
        {
            _lastW = Screen.width;
            _lastH = Screen.height;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null || _view == null) return;

            _cam.orthographic = true;
            _cam.transform.rotation = Quaternion.Euler(cameraTiltDegrees, 0f, 0f);
            _cam.nearClipPlane = 0.3f;
            _cam.farClipPlane = 300f;

            float halfW = cols * _view.CellSize * 0.5f;
            float halfD = rows * _view.CellSize * 0.5f;
            float midH = (_view.MinHeight + _view.MaxHeight) * 0.5f;
            float halfH = (_view.MaxHeight - _view.MinHeight) * 0.5f;
            _fieldCenter = new Vector3(0f, midH, 0f);

            Vector3 right = _cam.transform.right;
            Vector3 up = _cam.transform.up;
            float aspect = Screen.width / Mathf.Max(1f, (float)Screen.height);

            // project every bounding-box corner onto the camera's basis to find the
            // exact orthographic size that fits the whole (now tilted) field
            float projRight = 0f, projUp = 0f;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        var corner = new Vector3(sx * halfW, sy * halfH, sz * halfD);
                        projRight = Mathf.Max(projRight, Mathf.Abs(Vector3.Dot(corner, right)));
                        projUp = Mathf.Max(projUp, Mathf.Abs(Vector3.Dot(corner, up)));
                    }

            float sizeForUp = projUp;
            float sizeForRight = projRight / Mathf.Max(0.0001f, aspect);
            _cam.orthographicSize = Mathf.Max(sizeForUp, sizeForRight) * cameraPanSlack;

            // leftover room (beyond the exact fit) the camera may pan toward the player
            _panRoomUp = Mathf.Max(0f, _cam.orthographicSize - projUp);
            _panRoomRight = Mathf.Max(0f, _cam.orthographicSize * aspect - projRight);
        }

        /// <summary>Softly centers the camera on the player, never panning far enough
        /// to lose more than the pan-slack margin of the field off either edge.</summary>
        void TrackCamera()
        {
            if (_cam == null || Player == null) return;

            Vector3 right = _cam.transform.right;
            Vector3 up = _cam.transform.up;
            Vector3 offset = Player.transform.position - _fieldCenter;

            float rightOffset = Mathf.Clamp(Vector3.Dot(offset, right), -_panRoomRight, _panRoomRight);
            float upOffset = Mathf.Clamp(Vector3.Dot(offset, up), -_panRoomUp, _panRoomUp);
            Vector3 targetCenter = _fieldCenter + right * rightOffset + up * upOffset;

            _camCenter = Vector3.Lerp(_camCenter, targetCenter, 1f - Mathf.Exp(-cameraFollowSpeed * Time.deltaTime));
            _cam.transform.position = _camCenter - _cam.transform.forward * cameraDistance;
        }
    }
}
