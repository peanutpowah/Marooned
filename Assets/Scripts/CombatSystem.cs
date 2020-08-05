﻿using System.Collections.Generic;
using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    //Singleton included in Setup below

    [Header("References")]
    [SerializeField] HexGrid hexGrid = null;
    [SerializeField] Transform enemyCharacterParent = null;

    [SerializeField] CombatTurnSystem turnSystem = null;
    [SerializeField] HexGridController hexGridController = null;
    [SerializeField] SkillcheckSystem skillcheckSystem = null;

    [Header("Setup")]
    [HideInInspector]
    public BattleMap managementMap;
    public BattleMap[] battleMaps;
    public List<Character> debugEnemies = new List<Character>();

    public delegate void CombatHandler();
    public static CombatHandler OnCombatStart;
    public static CombatHandler OnCombatEnd;

    private Ability selectedAbility;
    List<HexCell> validTargetHexes;
    private List<HexCell> ValidTargetHexes
    {
        get => validTargetHexes;
        set
        {
            if (validTargetHexes != null)
            {
                foreach (var item in validTargetHexes)
                {
                    item.ShowHighlight(false, HexCell.HighlightType.ValidCombatInteraction);
                }
            }
            validTargetHexes = value;
            if (validTargetHexes != null)
            {
                foreach (var item in validTargetHexes)
                {
                    item.ShowHighlight(true, HexCell.HighlightType.ValidCombatInteraction);
                }
            }
        }
    }
    List<HexCell> abilityAffectedHexes;
    private List<HexCell> AbilityAffectedHexes
    {
        get => abilityAffectedHexes;
        set
        {
            if (abilityAffectedHexes != null)
            {
                foreach (var item in abilityAffectedHexes)
                {
                    item.ShowHighlight(false, HexCell.HighlightType.AbilityAffected);
                }
            }
            abilityAffectedHexes = value;
            if (abilityAffectedHexes != null)
            {
                foreach (var item in abilityAffectedHexes)
                {
                    item.ShowHighlight(true, HexCell.HighlightType.AbilityAffected);
                }
            }
        }
    }
    List<Character> hostileAbilityAffectedCharacters = new List<Character>();
    List<Character> friendlyAbilityAffectedCharacters = new List<Character>();
    List<Character> AllAffectedCharacters
    {
        get
        {
            List<Character> allCharacters = new List<Character>();
            allCharacters.AddRange(hostileAbilityAffectedCharacters);
            allCharacters.AddRange(friendlyAbilityAffectedCharacters);
            return allCharacters;
        }
    }

    bool awaitingSkillcheckSystem = false;

    #region Singleton
    public static CombatSystem instance;
    private void Awake()
    {
        if (instance != null)
        {
            Debug.Log("Another instance of : " + instance.ToString() + " was tried to be instanced, but was destroyed from gameobject: " + this.transform.name);
            GameObject.Destroy(this);
            return;
        }
        instance = this;
    }
    #endregion


    private void OnEnable()
    {
        CombatTurnSystem.OnTurnBegining += NewCharacterTurn;
        HexUnit.OnAnyUnitMoved += UnitMoved;
        HexCell.OnHexCellHoover += MarkCellsAndCharactersToBeAffected;
        HexGridController.OnCellSelected += UseAbility;
    }

    private void OnDisable()
    {
        CombatTurnSystem.OnTurnBegining -= NewCharacterTurn;
        HexUnit.OnAnyUnitMoved -= UnitMoved;
        HexCell.OnHexCellHoover -= MarkCellsAndCharactersToBeAffected;
        HexGridController.OnCellSelected -= UseAbility;
    }
    #if UNITY_EDITOR
    private void Update()
    {
        CheatInput();
    }
    #endif
    public void StartCombat(Player defender, Player attacker)
    {
        OnCombatStart?.Invoke();
        hexGridController.StartCombatMode();
        SetUpCombat(0, defender, attacker);
    }

    private void SetUpCombat(int size, Player defender, Player attacker)
    {
        hexGrid.Load(battleMaps[size], false);
        List<Character> allCharacters = new List<Character>();

        bool playerIsDefending = defender.IsHuman;

        if (playerIsDefending)
        {
            //Player characters
            allCharacters.AddRange(defender.Crew);
            //Enemy controlled characters
            AI aiController = new AI(attacker.Crew, defender.Crew);
            foreach (Character charactersToSpawn in attacker.Crew)
            {
                //Instantiate enemies
                Character spawnedCharacter = Instantiate(charactersToSpawn);
                spawnedCharacter.transform.SetParent(enemyCharacterParent);

                spawnedCharacter.myGrid = hexGrid;
                spawnedCharacter.SetAI(aiController);

                //Add character to grid
                HexCell validCell = hexGrid.Cells.ReturnRandomElementWithCondition((c) => c.IsFree == true, (c) => c.TypeOfSpawnPos == HexCell.SpawnType.AnyEnemy);
                hexGrid.AddUnit(spawnedCharacter, validCell, false);

                //Add character to list of all characters involved in combat
                allCharacters.Add(spawnedCharacter);
            }
        }
        else
        {
            //Player characters
            allCharacters.AddRange(attacker.Crew);
            //Enemy controlled characters
            AI aiController = new AI(defender.Crew, attacker.Crew);
            foreach (Character charactersToSpawn in defender.Crew)
            {
                //Instantiate enemies
                Character spawnedCharacter = Instantiate(charactersToSpawn);
                spawnedCharacter.transform.SetParent(enemyCharacterParent);

                spawnedCharacter.myGrid = hexGrid;
                spawnedCharacter.SetAI(aiController);

                //Add character to grid
                HexCell validCell = hexGrid.Cells.ReturnRandomElementWithCondition((c) => c.IsFree == true, (c) => c.TypeOfSpawnPos == HexCell.SpawnType.AnyEnemy);
                hexGrid.AddUnit(spawnedCharacter, validCell, false);

                //Add character to list of all characters involved in combat
                allCharacters.Add(spawnedCharacter);
            }
        }

        foreach (Character character in allCharacters)
        {
            character.CombatSetup();
        }

        turnSystem.SetupNewCombat(allCharacters);
        turnSystem.StartCombat();
    }

    public void EndCombat()
    {
        OnCombatEnd?.Invoke();
        hexGrid.Load(managementMap, false);
        HexGridController.CurrentMode = HexGridController.GridMode.Map;

        foreach (var item in HexGridController.player.Crew)
        {
            //Add move characters back to their saved location
            item.Location = hexGrid.GetCell(item.SavedShipLocation.coordinates);
            item.SavedShipLocation = item.Location;
        }
    }

    private void UnitMoved(HexUnit movedUnit)
    {
        if (HexGridController.ActiveCharacter == movedUnit)
        {
            ResetSelections();
        }
    }
    private void NewCharacterTurn(Character newTurnCharacter) => ResetSelections();

    private void ResetSelections()
    {
        ValidTargetHexes = null;
        AbilityAffectedHexes = null;
        selectedAbility = null;
    }

    private void MarkCellsAndCharactersToBeAffected(HexCell targetCell)
    {
        if (awaitingSkillcheckSystem)
        {
            return;
        }
        if (selectedAbility != null && HexGridController.ActiveCharacter != null && ValidTargetHexes.Contains(targetCell))
        {
            AbilityAffectedHexes = selectedAbility.targeting.GetAffectedCells(HexGridController.ActiveCharacter.Location, targetCell);
            selectedAbility.targeting.GetAffectedCharacters(HexGridController.ActiveCharacter, HexGridController.ActiveCharacter.Location, targetCell, out hostileAbilityAffectedCharacters, out friendlyAbilityAffectedCharacters);
        }
    }

    //Called from the UI when a player selects an ability
    public void SelectAbility(int selection)
    {
        if (awaitingSkillcheckSystem)
        {
            return;
        }
        if (HexGridController.ActiveCharacter.IsStunned)
        {
            return;
        }
        ResetSelections();
        selectedAbility = HexGridController.ActiveCharacter.SelectAbility(selection, out List<HexCell> abilityTargetHexes);
        Debug.Log("Selected ability " + selectedAbility.abilityName);
        ValidTargetHexes = abilityTargetHexes;
    }

    //Used by the AI when called from the character
    public void SelectAbility(Ability selection)
    {
        if (awaitingSkillcheckSystem)
        {
            return;
        }
        ResetSelections();
        selectedAbility = HexGridController.ActiveCharacter.SelectAbility(selection, out List<HexCell> abilityTargetHexes);
        Debug.Log("Selected ability " + selectedAbility.abilityName);
        ValidTargetHexes = abilityTargetHexes;
    }

    public void UseAbility(HexCell selectedCellForTarget)
    {
        if (awaitingSkillcheckSystem)
        {
            return;
        }
        Character abilityUser = HexGridController.ActiveCharacter;

        if (abilityUser == null)
            return;

        MarkCellsAndCharactersToBeAffected(selectedCellForTarget);
        if (selectedAbility != null && ValidTargetHexes != null && AbilityAffectedHexes != null && ValidTargetHexes.Contains(selectedCellForTarget)) //Have selected an ability and the target cell is a valid target
        {
            if (abilityUser.characterData.Energy.CurrentValue >= selectedAbility.cost) //Have enough energy
            {
                Debug.Log("Using ability " + selectedAbility.abilityName);

                //Autohits
                if (selectedAbility.AbilityuserHitSkillcheck == SkillcheckSystem.SkillcheckRequirement.None)
                {
                    selectedAbility.Use(abilityUser, hostileAbilityAffectedCharacters, friendlyAbilityAffectedCharacters, AbilityAffectedHexes);
                    PostAbilityCalculations();
                }
                else //Requires skillchecks
                {
                    awaitingSkillcheckSystem = true;
                    skillcheckSystem.OnCombatOutcomesDecided += ResolveAbilityOutcomes;
                    skillcheckSystem.StartContestedSkillcheck(abilityUser, hostileAbilityAffectedCharacters, friendlyAbilityAffectedCharacters,
                        selectedAbility.AbilityuserHitSkillcheck, selectedAbility.HostileDodgeSkillcheck, selectedAbility.FriendlyDodgeSkillcheck);
                }
            }
            else
            {
                Debug.Log("Not enough energy");
            }
        }
        else
        {
            Debug.Log("Action not possible. No action selected or clicked hex is not a valid hex");
        }
    }

    //Required outcomes
    private void ResolveAbilityOutcomes(List<SkillcheckSystem.CombatOutcome> hostileOutcomes, List<SkillcheckSystem.CombatOutcome> friendlyOutcomes)
    {
        skillcheckSystem.OnCombatOutcomesDecided -= ResolveAbilityOutcomes;
        selectedAbility.Use(HexGridController.ActiveCharacter, hostileAbilityAffectedCharacters, hostileOutcomes, friendlyAbilityAffectedCharacters, friendlyOutcomes, AbilityAffectedHexes);
        PostAbilityCalculations();
        awaitingSkillcheckSystem = false;
    }

    private void PostAbilityCalculations()
    {
        HexGridController.ActiveCharacter.characterData.Energy.CurrentValue -= selectedAbility.cost;
        HexGridController.ActiveCharacter.logMessage.AddLine(selectedAbility.CreateCombatLogMessage(HexGridController.ActiveCharacter, AllAffectedCharacters));
        HexGridController.ActiveCharacter.logMessage.SetAbilitySprite(selectedAbility.AbilitySprite);
        EndActiveCharacterTurn();
    }

    public void EndActiveCharacterTurn() => turnSystem.EndActiveCharacterTurn();

#region Cheats

    void CheatInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && Input.GetKey(KeyCode.LeftShift))
        {
            HexGridController.ActiveCharacter.characterData.Energy.CurrentValue = HexGridController.ActiveCharacter.characterData.Energy.MaxValue;
        }
        if (Input.GetKeyDown(KeyCode.M) && Input.GetKey(KeyCode.LeftShift))
        {
            HexGridController.ActiveCharacter.remainingMovementPoints = HexGridController.ActiveCharacter.defaultMovementPoints;
        }
    }
#endregion
}
