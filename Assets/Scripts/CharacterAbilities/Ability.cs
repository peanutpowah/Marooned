﻿using System.Collections.Generic;
using UnityEngine;

public abstract class Ability
{
    public static Dictionary<int, Ability> abilityDictionary = new Dictionary<int, Ability>()
    {
        {0, new ChainWhip(0)},
        {1, new GrabAndPull(1)},
        {2, new Punch(2)},
        {3, new WarCry(3) },
        {100, new Slice(100)}
    };

    public string abilityName;
    public string abilityDescription;
    public Sprite AbilitySprite
    {
        private set;
        get;
    }

    public int cost;
    public bool RequireSkillCheck
    {
        protected set;
        get;
    }
    public CharacterStatType AttackerSkillcheck
    {
        protected set;
        get;
    }
    public CharacterStatType TargetSkillcheck
    {
        protected set;
        get;
    }

    protected List<Effect> effects = new List<Effect>();
    public TargetType targeting;

    const string path = "AbilitySprites/";
    protected Ability(int abilityIndex)
    {
        abilityName = ToString();
        AbilitySprite = Resources.Load<Sprite>(path + "AbilityIcon" + abilityIndex);
    }

    //No decided outcomes required (autohits)
    public virtual void Use(Character attacker, List<Character> targets)
    {
        List<SkillcheckSystem.CombatOutcome> outcomes = new List<SkillcheckSystem.CombatOutcome>();
        for (int i = 0; i < targets.Count; i++)
        {
            outcomes.Add(SkillcheckSystem.CombatOutcome.NormalHit);
        }
        Use(attacker, targets, outcomes);
    }

    public virtual void Use(Character attacker, List<Character> targets, List<SkillcheckSystem.CombatOutcome> outcomes)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (outcomes[i] == SkillcheckSystem.CombatOutcome.Miss)
            {
                continue;
            }
            foreach (var item in effects)
            {
                item.ApplyEffect(attacker, targets[i], outcomes[i]);
            }
        }
    }
    protected void SetDescriptionFromEffects()
    {
        for (int i = 0; i < effects.Count; i++)
        {
            abilityDescription += effects[i].Description;
            if (i == effects.Count - 1)
            {
                abilityDescription += "\n";
            }
        }
    }
    public string CreateCombatLogMessage(Character attacker, List<Character> targets)
    {
        string targetsToString = "";

        // Billy, John, Robert and Andrew
        if (targets.Count > 0)
        {
            if (targets.Count > 1)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    if (i == targets.Count - 1)
                    {
                        targetsToString += $"and {targets[i].characterData.characterName}.";
                    }
                    else if (i == targets.Count - 2)
                    {
                        targetsToString += $"{targets[i].characterData.characterName} ";
                    }
                    else
                    {
                        targetsToString += $"{targets[i].characterData.characterName}, ";
                    }
                }
            }
            else
            {
                targetsToString = $"{targets[0].characterData.characterName}.";
            }
        }
        else
        {
            return $"Used {abilityName} but completely failed at aiming.";
        }
        return $"Used {abilityName} on {targetsToString}";
    }
}

public abstract class TargetType
{
    public abstract List<HexCell> GetValidTargets(HexCell fromCell);
    public abstract List<HexCell> GetAffectedCells(HexCell fromCell, HexCell targetCell);
    public List<Character> GetAffectedCharacters(HexCell fromCell, HexCell targetCell)
    {
        List<Character> affectedCharacters = new List<Character>();
        foreach (var cell in GetAffectedCells(fromCell, targetCell))
        {
            Character character = cell.Unit as Character;
            if (character)
            {
                affectedCharacters.Add(character);
            }
        }
        return affectedCharacters;
    }
}

public class SingleTargetAdjacent : TargetType
{
    public override List<HexCell> GetValidTargets(HexCell fromCell)
    {
        return CellFinder.GetAllAdjacent(fromCell, true, true);
    }
    public override List<HexCell> GetAffectedCells(HexCell fromCell, HexCell targetCell)
    {
        List<HexCell> affectedCells = new List<HexCell>();
        affectedCells.Add(targetCell);
        return affectedCells;
    }
}

public class SwipeAdjacent : TargetType
{
    public override List<HexCell> GetValidTargets(HexCell fromCell)
    {
        return CellFinder.GetAllAdjacent(fromCell, true, false);
    }
    public override List<HexCell> GetAffectedCells(HexCell fromCell, HexCell targetCell)
    {
        List<HexCell> affectedCells = new List<HexCell>();
        affectedCells.Add(targetCell);

        //Sides
        HexDirection dirToSelected = HexDirectionExtension.GetDirectionToNeighbor(fromCell, targetCell);
        HexCell previousCell = fromCell.GetNeighbor(dirToSelected.Previous(), true, false, false, false, false);
        if (previousCell)
        {
            affectedCells.Add(previousCell);
        }
        HexCell nextCell = fromCell.GetNeighbor(dirToSelected.Next(), true, false, false, false, false);
        if (nextCell)
        {
            affectedCells.Add(nextCell);
        }
        return affectedCells;
    }
}

public class AnySingleTarget : TargetType
{
    public override List<HexCell> GetValidTargets(HexCell fromCell)
    {
        return CellFinder.GetAllCells(fromCell.myGrid, true, true);
    }

    public override List<HexCell> GetAffectedCells(HexCell fromCell, HexCell targetCell)
    {
        List<HexCell> affectedCells = new List<HexCell>();
        affectedCells.Add(targetCell);
        return affectedCells;
    }
}

public class SingleTargetRanged : TargetType
{
    int range;
    public SingleTargetRanged(int range)
    {
        this.range = range;
    }

    public override List<HexCell> GetValidTargets(HexCell fromCell)
    {
        return CellFinder.GetAllCells(fromCell.myGrid, true, true);
    }

    public override List<HexCell> GetAffectedCells(HexCell fromCell, HexCell targetCell)
    {
        List<HexCell> affectedCells = new List<HexCell>();
        affectedCells.Add(targetCell);
        return affectedCells;
    }
}

public class SingleTargetRangeLine : TargetType
{
    int range;
    bool blockedByFirstUnit; //Can only hit one target in a line, the first detected is the only possible to hit
    public SingleTargetRangeLine(int range, bool blockedByFirstUnit)
    {
        this.range = range;
        this.blockedByFirstUnit = blockedByFirstUnit;
    }

    public override List<HexCell> GetValidTargets(HexCell fromCell)
    {
        return CellFinder.GetInLine(fromCell, true, true, range, blockedByFirstUnit);
    }

    public override List<HexCell> GetAffectedCells(HexCell fromCell, HexCell targetCell)
    {
        List<HexCell> affectedCells = new List<HexCell>();
        affectedCells.Add(targetCell);
        return affectedCells;
    }
}

public class SelfAOE : TargetType
{
    int range;

    public SelfAOE(int range)
    {
        this.range = range;
    }

    public override List<HexCell> GetValidTargets(HexCell fromCell)
    {
        List<HexCell> validCells = new List<HexCell>();
        validCells.Add(fromCell);
        return validCells;
    }

    //FromCell and TargetCell Should be equal
    public override List<HexCell> GetAffectedCells(HexCell fromCell, HexCell targetCell)
    {
        List<HexCell> affectedCells = new List<HexCell>();
        affectedCells.AddRange(CellFinder.GetAOE(targetCell, range, true));
        affectedCells.Add(fromCell);
        return affectedCells;
    }
}