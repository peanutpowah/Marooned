﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    [Header("References")]
    public HexGrid hexGrid;
    public Text selectedHexesCoordinateText;
    public Text newMapSize;

    public Dropdown spawnAbleDropdown;
    public Toggle traversableToggle;
    public Button overrideConnectionButton;
    public Text infoMessagePanel;

    [Header("Key commands")]
    public KeyCode toggleTraversable = KeyCode.T;
    public KeyCode selectMultiple = KeyCode.LeftShift;

    int xSize;
    int ySize;

    public List<HexCell> selectedHexes = new List<HexCell>();

    HexDirection selectedDirection = HexDirection.NE;

    private void Start()
    {
        xSize = 20;
        ySize = 15;
        CreateNewMap();
        ShowEditGrid(true);
        UpdateUI();
    }

    private void Update()
    {
        //if (!EventSystem.current.CompareTag("UI"))
        //{
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            HandleInput();
        }
        //}
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            UpdateSelectionOfHexes(Input.GetKey(selectMultiple));
        }
    }

    #region Selection
    void UpdateSelectionOfHexes(bool selectMultiple)
    {
        HexCell cell = hexGrid.GetCell();
        if (cell == null)
        {
            return;
        }

        if (overrideConnection)
        {
            OverrideConnection(cell);
        }
        else
        {
            if (!selectMultiple)
            {
                ClearSelectionList();
            }
            if (!selectedHexes.Contains(cell))
            {
                AddSelectionHex(cell);
            }
        }

        UpdateUI();
    }

    public void ClearSelectionList()
    {
        foreach (var item in selectedHexes)
        {
            item.ShowHighlight(false, HexCell.HighlightType.ActiveCell);
        }
        selectedHexes.Clear();
        UpdateUI();
    }

    private void AddSelectionHex(HexCell cellToAdd)
    {
        selectedHexes.Add(cellToAdd);
        cellToAdd.ShowHighlight(true, HexCell.HighlightType.ActiveCell);
    }
    #endregion

    #region CellOptions
    public void ChooseConnectionDirection(int newDir)
    {
        selectedDirection = (HexDirection)newDir;
    }

    public void ChangeSpawnType(int newSpawnType)
    {
        foreach (HexCell item in selectedHexes)
        {
            item.TypeOfSpawnPos = (HexCell.SpawnType)newSpawnType;
        }
    }

    public void ChangeTraversable(bool status)
    {
        foreach (var item in selectedHexes)
        {
            item.Traversable = status;
        }
    }

    bool overrideConnection = false;
    public void SelectForOverrideConnection()
    {
        overrideConnection = true;
        DisplayMessage("Overriding the connection with the next cell clicked on");
    }

    private void OverrideConnection(HexCell cellTo)
    {
        if (selectedHexes.Count == 1)
        {
            selectedHexes[0].SetNeighbor(selectedDirection, cellTo);
        }
        overrideConnection = false;
        ClearSelectionList();
        UpdateUI();
        DisplayMessage("New connection set up");
    }
    #endregion

    #region Grid and Gizmos
    public void ShowGameGrid(bool status)
    {
        hexGrid.ShowGameGrid(status);
    }

    public void ShowEditGrid(bool status)
    {
        hexGrid.ShowEditGrid(status);
    }

    public void ShowNeighborGizmos(bool status)
    {
        hexGrid.ShowNeighborGizmos(status);
    }
    #endregion

    #region New Map
    public void CreateNewMap()
    {
        ClearSelectionList();
        hexGrid.CreateMap(xSize, ySize, true, false, true);
        UpdateUI();
        ShowEditGrid(true);
    }

    public void SetXSizeByString(string stringInput)
    {
        if (int.TryParse(stringInput, out int number))
        {
            xSize = number;
            UpdateUI();
        }
    }

    public void SetYSizeByString(string stringInput)
    {
        if (int.TryParse(stringInput, out int number))
        {
            ySize = number;
            UpdateUI();
        }
    }
    #endregion

    private void DisplayMessage(string message)
    {
        infoMessagePanel.text = message;
    }

    public void UpdateUI()
    {
        string selectedText = "No hex clicked on";
        if (selectedHexes.Count == 1)
        {
            selectedText = selectedHexes[0].coordinates.ToString();
        }
        else if (selectedHexes.Count > 1)
        {
            selectedText = "Multiple hexes selected";
        }
        selectedHexesCoordinateText.text = selectedText;

        newMapSize.text = "X = " + xSize + "\nY = " + ySize;
        overrideConnectionButton.interactable = (selectedHexes.Count == 1);

        traversableToggle.SetIsOnWithoutNotify(false);
        foreach (var item in selectedHexes)
        {
            if (item.Traversable)
            {
                traversableToggle.SetIsOnWithoutNotify(true);
                break;
            }
        }

        spawnAbleDropdown.SetValueWithoutNotify((int)0);
        //Set the dropdown to the first selected hex type of spawnpos
        if (selectedHexes.Count > 0)
        {
            spawnAbleDropdown.SetValueWithoutNotify((int)selectedHexes[0].TypeOfSpawnPos);
        }
    }
}
