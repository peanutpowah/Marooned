﻿using UnityEngine;
using UnityEngine.UI;

public class JobPosition : MonoBehaviour
{
    public PlayerInput input;
    public CrewSimulation.ShipJob job;
    public CrewSimulation crewSimulation;
    public Image portrait;
    public Sprite defaultPortrait = null;

    public Character characterOnJob;
    public bool HasCharacter
    {
        get => characterOnJob != null;
    }

    public void RemoveCharacter()
    {
        characterOnJob = null;
        portrait.sprite = defaultPortrait;
    }

    public void SetCharacterToJob(Character newCharacter)
    {
        characterOnJob = newCharacter;
        portrait.sprite = newCharacter.characterData.portrait;
    }

    //Input from the UI-system when a character is placed on a job
    public void ClickDetected()
    {
        Character selectedCharacter = input.GetActiveCharacter();
        if (selectedCharacter)
        {
            crewSimulation.SetCharacterJob(selectedCharacter, job);
        }
    }
}
