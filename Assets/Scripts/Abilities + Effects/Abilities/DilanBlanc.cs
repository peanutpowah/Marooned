﻿using System.Collections.Generic;

public class Cleave : Ability
{
    int abilityCost = 10;
    int damage = 5;

    public Cleave(int abilityIndex) : base(abilityIndex)
    {
        cost = abilityCost;

        FriendlyHitChanceSkillcheck = CharacterStatType.Strength;
        HostileHitChanceSkillcheck = CharacterStatType.Strength;

        effects.Add(new Damage(damage, true, true));
        targeting = new SwipeAdjacent();
        base.SetDescriptionFromEffects();
    }
}

public class CarefulIncision : Ability
{
    int abilityCost = 10;
    int heal = 5;

    public CarefulIncision(int abilityIndex) : base(abilityIndex)
    {
        cost = abilityCost;

        FriendlyHitChanceSkillcheck = CharacterStatType.NONE;
        HostileHitChanceSkillcheck = CharacterStatType.NONE;

        effects.Add(new Heal(heal, true, true));
        targeting = new SingleTargetAdjacent();
        base.SetDescriptionFromEffects();
    }
}

public class BadMedicine : Ability
{
    int abilityCost = 30;
    int poison = 1;
    int duration = 2;

    public BadMedicine(int abilityIndex) : base(abilityIndex)
    {
        cost = abilityCost;

        FriendlyHitChanceSkillcheck = CharacterStatType.Accuracy;
        HostileHitChanceSkillcheck = CharacterStatType.Accuracy;

        effects.Add(new Poison(poison, duration, true, true));
        targeting = new SingleTargetAdjacent();
        base.SetDescriptionFromEffects();
    }
}

public class TheGoodStuff : Ability
{
    int abilityCost = 100;

    CreateObject specialEffect;
    ActiveHexObject spawnedObject;
    int healPerTurn = 5;

    public TheGoodStuff(int abilityIndex) : base(abilityIndex)
    {
        cost = abilityCost;

        FriendlyHitChanceSkillcheck = CharacterStatType.NONE;
        HostileHitChanceSkillcheck = CharacterStatType.NONE;

        specialEffect = new CreateObject(0);
        targeting = new SingleTargetAdjacent(false);
        base.SetDescriptionFromEffects();
    }

    public override void Use(Character attacker, List<Character> hostileTargets, List<Character> friendlyTargets, List<HexCell> affectedCells)
    {
        if (spawnedObject)
        {
            spawnedObject.Despawn();
        }
        spawnedObject = specialEffect.SpawnActiveObject(affectedCells[0], healPerTurn, attacker);
    }
}