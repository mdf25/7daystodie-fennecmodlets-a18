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
}