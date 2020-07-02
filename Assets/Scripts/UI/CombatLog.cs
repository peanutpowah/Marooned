﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CombatLog : MonoBehaviour
{
    [SerializeField] int combatLogLimit = 6;
    [SerializeField] MouseHoverImage prefab = null;
    Queue<MouseHoverImage> combatLog = new Queue<MouseHoverImage>();

    public void NewLog(string logMessage, Character attacker)
    {
        MouseHoverImage message = Instantiate(prefab, transform);

        if (combatLog.Count > combatLogLimit)
        {
            DestroyImmediate(combatLog.Dequeue().gameObject);
        }
        combatLog.Enqueue(message);
        message.UpdateUI(logMessage, attacker.characterData.portrait);
    }
}
