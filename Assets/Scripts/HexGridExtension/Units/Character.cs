﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : HexUnit
{
    public CharacterData characterData;

    public GameObject animatedArrow;
    public CharacterView overHeadUI;

    public delegate void CharacterStatHandler();
    public static CharacterStatHandler OnCharacterDowned;

    public bool isDead = false;
    public bool isDowned;
    int downCounter = 3;

    #region Visuals
    public Sprite portrait = null;
    #endregion

    //Used for storing locations on the player ship
    HexCell savedShipLocation;
    public HexCell SavedShipLocation
    {
        get => savedShipLocation;
        set
        {
            if (savedShipLocation)
            {
                savedShipLocation.Unit = null;
            }
            savedShipLocation = value;
            value.Unit = this;
        }
    }

    private void Awake()
    {
        //Setup abilities
        foreach (var item in abilityID)
        {
            if (Ability.abilityDictionary.TryGetValue(item, out Ability foundAbility))
            {
                Abilities.Add(foundAbility);
            }
            else
            {
                Debug.LogError("Ability ID " + item + " not found in dictionary");
            }
        }
        characterData.Setup();
        characterData.Vitality.OnResourceChanged += CheckCharacterDown;
        overHeadUI.SetCharacter(this);
    }

    public void CombatSetup() => characterData.SendValuesToRequesters();

    #region Effects and abilities
    public bool IsStunned
    {
        get => HasCondition(Condition.Stunned);
    }
    public bool HasCondition(Condition condition)
    {
        switch (condition)
        {
            case Condition.Stunned:
                foreach (var item in characterData.activeEffects)
                {
                    if (item is Stun)
                    {
                        return true;
                    }
                }
                break;
            case Condition.Bleeding:
                foreach (var item in characterData.activeEffects)
                {
                    if (item is Bleed)
                    {
                        return true;
                    }
                }
                break;
            case Condition.Poisoned:
                foreach (var item in characterData.activeEffects)
                {
                    if (item is Poison)
                    {
                        return true;
                    }
                }
                break;
        }
        return false;
    }
    public int NumberOfConditions(Condition condition)
    {
        int foundConditions = 0;
        switch (condition)
        {
            case Condition.Stunned:
                foreach (var item in characterData.activeEffects)
                {
                    if (item is Stun)
                    {
                        foundConditions++;
                    }
                }
                break;
            case Condition.Bleeding:
                foreach (var item in characterData.activeEffects)
                {
                    if (item is Bleed)
                    {
                        foundConditions++;
                    }
                }
                break;
            case Condition.Poisoned:
                foreach (var item in characterData.activeEffects)
                {
                    if (item is Poison)
                    {
                        foundConditions++;
                    }
                }
                break;
        }
        return foundConditions;
    }
    public List<Character> tauntedBy = new List<Character>();

    public List<Ability> Abilities { get; set; } = new List<Ability>();
    public List<int> abilityID = new List<int>();

    private void EffectTickUpdate()
    {
        if (characterData.activeEffects.Count > 0)
        {
            for (int i = 0; i < characterData.activeEffects.Count; i++)
            {
                characterData.activeEffects[i].EffectTick(this);
            }
        }
    }

    public Ability SelectAbility(int abilityIndex, out List<HexCell> possibleTargets)
    {
        possibleTargets = Abilities[abilityIndex].targeting.GetValidTargetCells(Location, true);
        return Abilities[abilityIndex];
    }

    public Ability SelectAbility(Ability ability, out List<HexCell> possibleTargets)
    {
        if (!Abilities.Contains(ability))
        {
            throw new System.ArgumentException("Selected ability not part of characters abilities. In " + characterData.CharacterName + " - " + ability.ToString());
        }
        possibleTargets = ability.targeting.GetValidTargetCells(Location, true);
        return ability;
    }
    #endregion

    public bool isFriendlyTo(Character otherCharacter)
    {
        //If berserk : return false;
        //if othercharacter is berserk : return false;
        return playerControlled == otherCharacter.playerControlled;
    }

    public override bool CanEnter(HexCell cell)
    {
        if (!cell.IsFree)
        {
            return false;
        }
        if (!cell.Traversable)
        {
            return false;
        }
        return true;
    }
    public bool CanMove()
    {
        if (isDowned) return false;
        if (isDead) return false;
        if (IsStunned) return false;
        return true;
    }

    public override void MakeUnitActive()
    {
        switch (HexGridController.CurrentMode)
        {
            case HexGridController.GridMode.Map:
                break;
            case HexGridController.GridMode.Combat:
                if (CanMove())
                {
                    remainingMovementPoints = defaultMovementPoints;
                    characterData.Energy.CurrentValue += CharacterData.DEFAULTENERGYREGEN;
                }
                else
                {
                    remainingMovementPoints = 0;
                }
                break;
            case HexGridController.GridMode.Management:
                remainingMovementPoints = int.MaxValue;
                break;
        }
        base.MakeUnitActive();
    }

    public override void ShowUnitActive(bool status)
    {
        base.ShowUnitActive(status);
        animatedArrow.SetActive(status);
    }

    public void TurnEnded()
    {
        EffectTickUpdate();
        if (isDowned)
        {
            CharacterDownTick();
            IsCharacterDead();
        }
    }

    void CheckCharacterDown(int notUsed) => IsCharacterDown();
    public bool IsCharacterDown()
    {
        if (characterData.Vitality.CurrentValue <= 0 && !IsCharacterDead())
        {
            OnCharacterDowned?.Invoke();
            isDowned = true;
            Debug.Log($"{characterData.CharacterName} is down");
        }
        else
        {
            isDowned = false;
        }
        return isDowned;
    }

    public void CharacterDownTick() => downCounter--;

    public bool IsCharacterDead()
    {
        if (downCounter <= 0)
        {
            isDead = true;
            // Remove character from  player session
            Debug.Log($"{characterData.CharacterName} died");
        }
        return isDead;
    }

    #region AI
    public void SetAI(AI ai) => aiController = ai;
    public void SetNextAction(ActionGroup actionGroup) => nextAction = actionGroup;
    AI aiController;
    ActionGroup nextAction;
    //HexCell target;
    public override IEnumerator PerformAutomaticTurn(int visionRange) //Visionrange is not used by characters
    {
        //Do turn
        CombatTurnSystem.OnTurnBeginning?.Invoke(this);

        if (!IsStunned)
        {
            yield return aiController.CalculateAvailableActions(this);
            if (nextAction != null)
            {
                if (nextAction.cellToEndTurnOn != Location)
                {
                    yield return MoveToTargetCell(nextAction.cellToEndTurnOn);
                }
                if (nextAction.abilityToUse != null)
                {
                    CombatSystem.instance.SelectAbility(nextAction.abilityToUse);
                    CombatSystem.instance.UseAbility(nextAction.cellAbilityTarget);
                }
                else
                {
                    CombatSystem.instance.EndActiveCharacterTurn();
                }
            }

            nextAction = null;
        }
        else
        {
            CombatSystem.instance.EndActiveCharacterTurn();
        }
    }

    IEnumerator MoveToTargetCell(HexCell targetCell)
    {
        Pathfinding.FindPath(Location, targetCell, this, playerControlled);
        List<HexCell> path = Pathfinding.GetWholePath();
        yield return Travel(path);
        Pathfinding.ClearPath();
    }
    #endregion
}
