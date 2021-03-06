﻿using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using System;
using System.Linq;
using Random = UnityEngine.Random;
using System.Text.RegularExpressions;

public static class Utility
{
    public static Vector3 GetMouseWorldPosition(Camera worldCamera, Vector3 mousePosition)
    {
        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0;
        return worldPosition;
    }

    public static Vector3 GetMouseWorldPosition()
    {
        return GetMouseWorldPosition(Camera.main, Input.mousePosition);
    }

    public static List<T> ShuffleList<T>(List<T> list)
    {
        int count = list.Count;
        for (var i = 0; i < count - 1; ++i)
        {
            int r = Random.Range(i, count);
            T tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
        return list;
    }

    public static T ReturnRandom<T>(T a, T b)
    {
        if (a != null && b != null)
        {
            if (Random.Range(0, 100) > 50)
            {
                return a;
            }
            else
            {
                return b;
            }
        }
        Debug.LogWarning("Objects not set");
        return default;
    }

    public static T ReturnRandom<T>(T[] array)
    {
        if (array != null && array.Length > 0)
        {
            return array[Random.Range(0, array.Length)];
        }
        Debug.LogWarning("Array empty");
        return default;
    }

    public static T ReturnRandom<T>(List<T> list)
    {
        if (list != null && list.Count > 0)
        {
            return list[Random.Range(0, list.Count)];
        }
        Debug.LogWarning("List empty");
        return default;
    }

    public static T ReturnRandom<T>(List<T> list, int seed)
    {
        Random.State previousState = Random.state;
        Random.InitState(seed);
        if (list != null && list.Count > 0)
        {
            T element = list[Random.Range(0, list.Count)];
            Random.state = previousState;
            return element;
        }
        Random.state = previousState;
        Debug.LogWarning("List empty");
        return default;
    }

    public static T TestVariableAgainstConditions<T>(T variable, params Func<T, bool>[] conditions)
    {
        if (variable == null)
        {
            return default(T);
        }
        bool flag = true;
        foreach (var con in conditions)
        {
            if (!con.Invoke(variable))
            {
                flag = false;
                break;
            }
        }
        if (flag)
        {
            return variable;
        }
        return default(T);
    }

    public static IList<T> PopulateListWithMatchingConditions<T>(this IList<T> outList, IList<T> inList, params Func<T, bool>[] conditions)
    {
        foreach (var item in inList)
        {
            if (item == null)
            {
                continue;
            }
            bool flag = true;
            foreach (var con in conditions)
            {
                if (!con.Invoke(item))
                {
                    flag = false;
                    break;
                }
            }
            if (flag)
            {
                outList.Add(item);
            }
        }
        return outList;
    }

    public static T ReturnRandomElementWithCondition<T>(this IList<T> inList, params Func<T, bool>[] conditions)
    {
        IList<T> passedItems = new List<T>().PopulateListWithMatchingConditions(inList, conditions);
        return passedItems[Random.Range(0, passedItems.Count)];
    }

    public static T ReturnElementWithCondition<T>(this IList<T> inList, Func<IList<T>, int> index, params Func<T, bool>[] conditions)
    {
        IList<T> passedItems = new List<T>().PopulateListWithMatchingConditions(inList, conditions);
        if (passedItems.Count > 0)
        {
            return passedItems[index.Invoke(passedItems)];
        }
        else
        {
            return default(T);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    //	This makes it easy to create, name and place unique new ScriptableObject asset files.
    /// </summary>
    public static T CreateAsset<T>(string pathFolderName, string assetName) where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();

        string path = "Assets";
        if (pathFolderName != null)
        {
            path += "/" + pathFolderName;
        }

        string assetPathAndName = "";

        if (assetName == "")
        {
            assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).ToString() + ".asset");
        }
        else
        {
            assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + assetName + ".asset");
        }

        AssetDatabase.CreateAsset(asset, assetPathAndName);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        return asset;
    }
#endif

    public static float PercentageToFactor(int wholePercentage)
    {
        return (float)wholePercentage / 100;
    }

    public static int FactorToPercentage(float floatPercentage)
    {
        return Mathf.RoundToInt(floatPercentage * 100);
    }

    public static string FactorToPercentageText(float floatPercentage)
    {
        int percentage = FactorToPercentage(floatPercentage);
        return percentage.ToString() + "%";
    }

    public static string ReplaceWordInString(string originalString, string wordToReplace, string newWord)
    {
        string pattern = $@"\b{wordToReplace}\b";
        return  Regex.Replace(originalString, pattern, newWord);
    }
}
