using System;
using System.Collections.Generic;

/**
 * Class to contain methods for string manipulations that are used across modlets.
 */

public static class StringHelpers
{
    /**
     * Reads a comma separated string into a list.
     */

    public static List<string> WriteStringToList(string _s)
    {
        if (_s == null)
        {
            return new List<string>() { "" };
        }

        if (!_s.Contains(","))
        {
            return new List<string>() { _s };
        }

        string[] strings = _s.Split(',');
        List<string> list = new List<string>();
        foreach (string str in strings)
        {
            list.Add(str.Trim());
        }
        return list;
    }


    /**
     * Writes a list to a comma separated string.
     */

    public static string WriteListToString(List<string> _list)
    {
        if (_list == null)
        {
            return "";
        }

        switch (_list.Count)
        {
            case 0:
                return "";
            case 1:
                return _list[0];
            default:
                return String.Join(",", _list);
        }
    }


    /**
     * Writes a vector3i to a string.
     */

    public static string WriteVector3iToString(Vector3i _vector)
    {
        return _vector.ToStringNoBlanks();
    }


    /**
     * Writes a string to a vector3i.
     */

    public static Vector3i WriteStringToVector3i(string _s)
    {
        List<string> values = WriteStringToList(_s);
        switch(values.Count)
        {
            case 1:
                int vector3iValue;
                if (!int.TryParse(_s, out vector3iValue))
                {
                    return Vector3i.zero;
                }
                return new Vector3i(vector3iValue, vector3iValue, vector3iValue);
            case 3:
                List<int> valueInts = new List<int>();
                foreach (string value in values)
                {
                    int valueInt;
                    if (!int.TryParse(value, out valueInt))
                    {
                        return Vector3i.zero;
                    }
                    valueInts.Add(valueInt);
                }
                return new Vector3i(valueInts[0], valueInts[1], valueInts[2]);
            default:
                return Vector3i.zero; 
        }
    }
}