﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrewJobDisplay : MouseHoverImage
{
    public Character character;
    public void OnClick()
    {
        HexGridController.SelectedCell = character.Location;
    }
}
