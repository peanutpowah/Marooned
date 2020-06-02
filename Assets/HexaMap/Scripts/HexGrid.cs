﻿using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class HexGrid : MonoBehaviour
{
    public int cellCountX = 20, cellCountY = 15;

    public HexCell cellPrefab;
    public Texture2D noiseSource;
    public HexUnit unitPrefab;

    HexCell[] cells;

    List<HexUnit> units = new List<HexUnit>();

    public int seed;
    public Tilemap terrain;
    public TileBase oceanTile;
    public TileBase landTile;

    private void OnEnable()
    {
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexUnit.unitPrefab = unitPrefab;
        }
    }

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);

        HexUnit.unitPrefab = unitPrefab;

        CreateMap(cellCountX, cellCountY);
    }

    public bool CreateMap(int x, int y)
    {
        ClearUnits();

        cellCountX = x;
        cellCountY = y;
        CreateCells();

        //Debug unit
        HexUnit unit = Instantiate(unitPrefab);
        unit.transform.position = cells[0].Position;
        cells[0].Unit = unit;
        unit.Location = cells[0];

        return true;
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Y * cellCountX + coordinates.Y / 2;
        if (index >= 0 && cells.Length > index)
        {
            return cells[index];
        }
        else
        {
            return null;
        }
    }

    void CreateCells()
    {
        cells = new HexCell[cellCountY * cellCountX];

        for (int y = 0, i = 0; y < cellCountY; y++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, y, i++);
            }
        }
    }

    void CreateCell(int x, int y, int i)
    {
        Vector3 position;
        position.x = (x + y * 0.5f - y / 2) * (HexMetrics.innerRadius * 2f);
        position.z = 0f;
        position.y = y * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, y);

        //Connect hex neighbors to the west
        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (y > 0)
        {
            if ((y & 1) == 0) //If even row (with X = 0 to the leftmost position)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else //Un-even row (with X = 0 with incline into the row)
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }

        cell.transform.SetParent(this.transform);

        SetTerrainCellVisual(cell);
    }

    /// <summary>
    /// Paints terrain on the tilemap
    /// </summary>
    /// <param name="cell"></param>
    private void SetTerrainCellVisual(HexCell cell)
    {
        Vector3Int tilemapPosition = HexCoordinates.CoordinatesToTilemapCoordinates(cell.coordinates);

        TileBase tile;

        if (HexMetrics.landChance > HexMetrics.SampleHashGrid(cell.Position).a)
        {
            tile = landTile;
            cell.IsOcean = false;
        }
        else
        {
            tile = oceanTile;
            cell.IsOcean = true;
        }

        terrain.SetTile(tilemapPosition, tile);
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int y = coordinates.Y;
        int x = coordinates.X + y / 2;

        if (y < 0 || y >= cellCountY)
        {
            return null;
        }
        if (x < 0 || x >= cellCountX)
        {
            return null;
        }

        return cells[x + y * cellCountX];
    }

    public HexCell GetCell(Ray ray)
    {
        RaycastHit2D hit;
        hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if (hit.collider)
        {
            return GetCell(hit.point);
        }
        else
        {
            return null;
        }
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].ShowUI(visible);
        }
    }

    public void AddUnit(HexUnit unit, HexCell location, int orientation)
    {
        units.Add(unit);
        unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = (HexDirection)orientation;
    }

    public void RemoveUnit(HexUnit unit)
    {
        units.Remove(unit);
        unit.Die();
    }

    void ClearUnits()
    {
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Die();
        }
        units.Clear();
    }

    #region Save and Load
    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountY);

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }

        writer.Write(units.Count);
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader)
    {
        ClearUnits();

        int x = reader.ReadInt32();
        int z = reader.ReadInt32();

        if (cellCountX != x || cellCountY != z)
        {
            if (!CreateMap(x, z))
            {
                return;
            }
        }

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader);
        }

        int unitCount = reader.ReadInt32();
        for (int i = 0; i < unitCount; i++)
        {
            HexUnit.Load(reader, this);
        }
    }
    #endregion
}
