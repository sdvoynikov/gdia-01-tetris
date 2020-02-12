using System;
using UnityEngine;
using Random = UnityEngine.Random;

public enum Cell {
    Empty,
    Filled,
    Active
}

public class GameField : MonoBehaviour {
    private const int FieldWidth  = 10;
    private const int FieldHeight = 20;

    private readonly bool[][,] Figures = {
            new[,] { //L
                    {false, false, false, false},
                    {false, true, false, false},
                    {false, true, false, false},
                    {false, true, true, false},
            },
            new[,] { //I
                    {false, true, false, false},
                    {false, true, false, false},
                    {false, true, false, false},
                    {false, true, false, false}
            },
            new[,] { //O
                    {false, false, false, false},
                    {false, false, false, false},
                    {false, true, true, false},
                    {false, true, true, false},
            },
            new[,] { //N
                    {false, false, false, false},
                    {false, true, false, false},
                    {false, true, true, false},
                    {false, false, true, false},
            },
            new[,] { //T
                    {false, false, false, false},
                    {false, false, false, false},
                    {false, true, false, false},
                    {true, true, true, false},
            },
    };

    [SerializeField] private SpriteRenderer _cellPrefab;
    [SerializeField] private float          _moveDelay = .5f;

    private Cell[,]       _cells       = new Cell[FieldHeight, FieldWidth];
    private GameObject[,] _cellSprites = new GameObject[FieldHeight, FieldWidth];
    private float         _timeToMove;
    private bool          _gameIsOver;

    private void Start() {
        for (var y = 0; y < FieldHeight; y++) {
            for (var x = 0; x < FieldWidth; x++) {
                var cellSprite = _cellSprites[y, x] = Instantiate(_cellPrefab.gameObject);
                cellSprite.transform.position = new Vector3(x, y, 0);
                cellSprite.SetActive(true);
            }
        }

        SpawnFigure();
    }

    private void Update() {
        _timeToMove += Time.deltaTime;
        if (!_gameIsOver && (_timeToMove > _moveDelay)) {
            _timeToMove = 0;

            if (!CanMoveFigure(0, -1)) {
                StopFigure();
                RemoveFilledRows();
                _gameIsOver = !SpawnFigure();
            }
            else {
                MoveFigure(0, -1);
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            if (CanMoveFigure(-1, 0)) {
                MoveFigure(-1, 0);
            }
        }

        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            if (CanMoveFigure(1, 0)) {
                MoveFigure(1, 0);
            }
        }

        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            while (CanMoveFigure(0, -1)) {
                MoveFigure(0, -1);
            }
        }

        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            if (CanRotateFigure(out var rotatedFigure, out var pivot)) {
                RotateFigure(rotatedFigure, pivot);
            }
        }

        UpdateField();
    }

    private void RemoveFilledRows() {
        for (var y = 0; y < FieldHeight; y++) {
            var filled = true;
            for (var x = 0; x < FieldWidth; x++) {
                if (_cells[y, x] == Cell.Empty) {
                    filled = false;
                    break;
                }
            }

            if (filled) {
                for (var ny = y + 1; ny < FieldHeight; ny++) {
                    for (var x = 0; x < FieldWidth; x++) {
                        _cells[ny - 1, x] = _cells[ny, x];
                    }
                }

                y--;
            }
        }
    }

    private bool CanMoveFigure(int dx, int dy) {
        for (var y = 0; y < FieldHeight; y++) {
            for (var x = 0; x < FieldWidth; x++) {
                if (_cells[y, x] == Cell.Active) {
                    if ((y + dy < 0) || (x + dx < 0) || (x + dx >= FieldWidth) || (_cells[y + dy, x + dx] == Cell.Filled)) {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private void MoveFigure(int dx, int dy) {
        var startX = 0;
        var endX   = FieldWidth;
        var cx     = 1;
        if (dx >= 0) {
            startX = FieldWidth - 1;
            endX   = -1;
            cx     = -1;
        }

        for (var y = 0; y < FieldHeight; y++) {
            for (var x = startX; x != endX; x += cx) {
                if (_cells[y, x] == Cell.Active) {
                    _cells[y + dy, x + dx] = Cell.Active;
                    _cells[y, x]           = Cell.Empty;
                }
            }
        }
    }

    private bool CanRotateFigure(out bool[,] rotatedFigure, out Vector2Int pivot) {
        var min = new Vector2Int(FieldWidth, FieldHeight);
        var max = new Vector2Int(-1,         -1);

        for (var y = 0; y < FieldHeight; y++) {
            for (var x = 0; x < FieldWidth; x++) {
                if (_cells[y, x] == Cell.Active) {
                    min = new Vector2Int(Mathf.Min(min.x, x), Mathf.Min(min.y, y));
                    max = new Vector2Int(Mathf.Max(max.x, x), Mathf.Max(max.y, y));
                }
            }
        }

        var size = (max - min) + Vector2Int.one;
        rotatedFigure = new bool[size.x, size.y];
        pivot         = (min + max) / 2;

        for (var y = min.y; y <= max.y; y++) {
            for (var x = min.x; x <= max.x; x++) {
                if (_cells[y, x] == Cell.Active) {
                    var dx = x - min.x;
                    var dy = y - min.y;
                    rotatedFigure[dx, dy] = true;
                }
            }
        }

        for (var y = 0; y < rotatedFigure.GetLength(0); y++) {
            for (var x = 0; x < rotatedFigure.GetLength(1); x++) {
                if (rotatedFigure[y, x]) {
                    var dx = pivot.x + x;
                    var dy = pivot.y + y;

                    if ((dx < 0) || (dy < 0) || (dx >= FieldWidth) || (dy >= FieldHeight) || (_cells[dy, dx] == Cell.Filled)) {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private void RotateFigure(bool[,] rotatedFigure, Vector2Int pivot) {
        for (var y = 0; y < FieldHeight; y++) {
            for (var x = 0; x < FieldWidth; x++) {
                if (_cells[y, x] == Cell.Active) {
                    _cells[y, x] = Cell.Empty;
                }
            }
        }

        for (var y = 0; y < rotatedFigure.GetLength(0); y++) {
            for (var x = 0; x < rotatedFigure.GetLength(1); x++) {
                if (rotatedFigure[y, rotatedFigure.GetLength(1) - x - 1]) {
                    var dx = pivot.x + x;
                    var dy = pivot.y + y;
                    _cells[dy, dx] = Cell.Active;
                }
            }
        }
    }

    private void StopFigure() {
        for (var y = 0; y < FieldHeight; y++) {
            for (var x = 0; x < FieldWidth; x++) {
                if (_cells[y, x] == Cell.Active) {
                    _cells[y, x] = Cell.Filled;
                }
            }
        }
    }

    private void UpdateField() {
        for (var y = 0; y < FieldHeight; y++) {
            for (var x = 0; x < FieldWidth; x++) {
                var color = new Color(.2f, .2f, .2f, .2f);
                if (_cells[y, x] == Cell.Filled) {
                    color = new Color(1f, 1f, 1f, 1f);
                }
                else if (_cells[y, x] == Cell.Active) {
                    color = new Color(1f, 1f, 0.25f, 1f);
                }

                _cellSprites[y, x].GetComponent<SpriteRenderer>().color = color;
            }
        }
    }

    private bool SpawnFigure() {
        var figure = Figures[Random.Range(0, Figures.Length)];

        var px                  = FieldWidth / 2 - figure.GetLength(1) / 2;
        var py                  = FieldHeight    - figure.GetLength(0);
        var spawnedSuccessfully = true;

        for (var y = 0; y < figure.GetLength(0); y++) {
            for (var x = 0; x < figure.GetLength(1); x++) {
                if (figure[y, x]) {
                    if (_cells[y + py, x + px] == Cell.Filled) {
                        spawnedSuccessfully = false;
                    }

                    _cells[y + py, x + px] = Cell.Active;
                }
            }
        }

        if (spawnedSuccessfully) {
            var r = Random.Range(0, 4);
            while (r-- > 0) {
                if (CanRotateFigure(out var rotatedFigure, out var pivot)) {
                    RotateFigure(rotatedFigure, pivot);
                }
            }
        }

        return spawnedSuccessfully;
    }
}
