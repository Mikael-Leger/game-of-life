using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class LifeGameManager : MonoBehaviour
{
    public float GameSpeed = 0.1f;
    public Tilemap Tilemap;
    public Tilemap TilemapTemp;
    public TileBase TileAlive;
    public Button PauseButton;
    public Button NextButton;
    public GameObject GameSpeedGroup;

    private BoundsInt tilemapBounds;
    private HashSet<Vector3Int> changedTiles = new();
    private int width = 36;
    private int height = 20;
    private bool gameResume = true;
    private const double X1 = 0.009;
    private const double Y1 = 10;
    private const double X2 = 0.5;
    private const double Y2 = 1;

    void Start()
    {
        GameSpeedGroup.GetComponentInChildren<Slider>().onValueChanged.AddListener(OnGameSpeedChange);
        tilemapBounds = Tilemap.cellBounds;
        OnPauseButtonClick();
        StartCoroutine(StartGame());
    }

    void Update()
    {
        if (!gameResume && Input.GetMouseButtonDown(0))
        {
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPosition.z = 0;
            Vector3Int tilePosition = Tilemap.WorldToCell(worldPosition);

            TileBase tile = Tilemap.GetTile(tilePosition);

            if (tile != null)
            {
                Tilemap.SetTile(tilePosition, null);
            }
            else
            {
                Tilemap.SetTile(tilePosition, TileAlive);
            }
            changedTiles.Add(tilePosition);

            UpdateTilemapBounds();
        }
    }

    public void OnPauseButtonClick()
    {
        gameResume = !gameResume;
        PauseButton.GetComponentInChildren<TextMeshProUGUI>().text = gameResume ? "pause" : "play";
        NextButton.interactable = !gameResume;
        NextButton.gameObject.SetActive(!gameResume);
    }

    public void OnNextButtonClick()
    {
        StartCoroutine(WaitForNextgeneration());
    }

    public void OnMenuButtonClick()
    {
        SceneManager.LoadScene("Menu");
    }

    public void OnResetButtonClick()
    {
        Tilemap.ClearAllTiles();
        OnPauseButtonClick();
    }

    IEnumerator WaitForNextgeneration()
    {
        gameResume = true;
        yield return new WaitForSeconds(GameSpeed);
        gameResume = false;
    }

    public static double GetGameSpeedByValue(double input)
    {
        if (input < X1 || input > X2)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "x is out of the interpolation range.");
        }

        double y = Y1 + (input - X1) * (Y2 - Y1) / (X2 - X1);
        return y;
    }

    public void OnGameSpeedChange(float value)
    {
        GameSpeed = value;
        GameSpeedGroup.GetComponentInChildren<TextMeshProUGUI>().text = $"GAME SPEED: {GetGameSpeedByValue(value):0.0}x";
        EventSystem.current.SetSelectedGameObject(null);
    }

    IEnumerator StartGame()
    {
        InitializeChangedTiles();
        while (true)
        {
            if (gameResume)
            {
                CopyTilemapInto(Tilemap, TilemapTemp);
                LoopThroughChangedTiles();
                CopyTilemapInto(TilemapTemp, Tilemap);
                UpdateTilemapBounds();
            }
            yield return new WaitForSeconds(GameSpeed);
        }
    }

    void InitializeChangedTiles()
    {
        LoopInTheTilemap((x, y) =>
        {
            Vector3Int tilePosition = new(x, y, 0);
            if (Tilemap.GetTile(tilePosition) != null)
            {
                changedTiles.Add(tilePosition);
            }
        });
    }

    void UpdateTilemapBounds()
    {
        if (changedTiles.Count == 0) return;

        Vector3Int min = tilemapBounds.min;
        Vector3Int max = tilemapBounds.max;

        foreach (var position in changedTiles)
        {
            min = Vector3Int.Min(min, position);
            max = Vector3Int.Max(max, position);
        }

        tilemapBounds = new BoundsInt(min, max - min + Vector3Int.one);
        width = tilemapBounds.size.x;
        height = tilemapBounds.size.y;
    }

    void CopyTilemapInto(Tilemap tilemapSource, Tilemap tilemapTarget)
    {
        foreach (var position in changedTiles)
        {
            tilemapTarget.SetTile(position, tilemapSource.GetTile(position));
        }
    }

    void UseGameRules(HashSet<Vector3Int> newChangedTiles, Vector3Int position)
    {
        int numberOfAliveAdjacents = GetNumberOfAliveAdjacents(position);
        TileBase currentTile = Tilemap.GetTile(position);

        if (currentTile == TileAlive && (numberOfAliveAdjacents < 2 || numberOfAliveAdjacents > 3))
        {
            TilemapTemp.SetTile(position, null);
            newChangedTiles.Add(position);
        }
        else if (currentTile == null && numberOfAliveAdjacents == 3)
        {
            TilemapTemp.SetTile(position, TileAlive);
            newChangedTiles.Add(position);
        }
    }

    void LoopThroughChangedTiles()
    {
        HashSet<Vector3Int> newChangedTiles = new();

        foreach (Vector3Int position in changedTiles)
        {
            UseGameRules(newChangedTiles, position);
            List<Vector3Int> adjacentPositionTiles = GetAdjacentPositions(position);
            foreach (Vector3Int adjacentPositionTile in adjacentPositionTiles)
            {
                UseGameRules(newChangedTiles, adjacentPositionTile);
            }
        }

        changedTiles = newChangedTiles;
    }

    int GetNumberOfAliveAdjacents(Vector3Int tilePosition)
    {
        int numberOfAlives = 0;
        List<TileBase> adjacentTiles = GetAdjacentTiles(tilePosition);
        foreach (TileBase tile in adjacentTiles)
        {
            if (tile == TileAlive)
            {
                numberOfAlives++;
            }
        }
        return numberOfAlives;
    }

    List<Vector3Int> GetAdjacentPositions(Vector3Int tilePosition)
    {
        return new()
        {
            tilePosition + new Vector3Int(-1, 0, 0),
            tilePosition + new Vector3Int(-1, -1, 0),
            tilePosition + new Vector3Int(0, -1, 0),
            tilePosition + new Vector3Int(1, -1, 0),
            tilePosition + new Vector3Int(1, 0, 0),
            tilePosition + new Vector3Int(1, 1, 0),
            tilePosition + new Vector3Int(0, 1, 0),
            tilePosition + new Vector3Int(-1, 1, 0)
        };
    }

    List<TileBase> GetAdjacentTiles(Vector3Int tilePosition)
    {
        List<TileBase> listTileBases = new();
        foreach (Vector3Int tile in GetAdjacentPositions(tilePosition))
        {
            listTileBases.Add(Tilemap.GetTile(tile));
        }
        return listTileBases;
    }

    void LoopInTheTilemap(Action<int, int> callback)
    {
        for (int x = -width; x <= width; x++)
        {
            for (int y = -height; y <= height; y++)
            {
                callback(x, y);
            }
        }
    }
}
