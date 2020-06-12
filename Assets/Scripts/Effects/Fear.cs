﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fear : Effect
{
    int amount;
    public Fear(int amount)
    {
        description = $"Reduces loyalty by {amount}%";
        this.amount = amount;
    }
    public override void ApplyEffect(Character character) { }
    public override void EffectTick(Character character)
    {
        character.resources.Loyalty -= amount;
    }
    public override string GetData()
    {
        return "";
    }
}
