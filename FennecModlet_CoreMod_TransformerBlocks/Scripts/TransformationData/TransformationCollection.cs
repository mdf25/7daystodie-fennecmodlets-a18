using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public class TransformationCollection
{
	public List<TransformationData> collection;

    /**
     * Regexes for parsing input strings into the system and reading collections and data.
     */
    public static Dictionary<string, Regex> readParser = new Dictionary<string, Regex>()
    {
        { "tColParse",      new Regex(@"#tC#(.+?)#_tC#") },
        { "tDataParse",     new Regex(@"#tD#(.+?)#_tD#") },
        { "tTimeParse",     new Regex(@"#tT#(.+?)#_tT#") },
        { "tInputParse",    new Regex(@"#tI#(.+?)#_tI#") },
        { "tOutputParse",   new Regex(@"#tO#(.+?)#_tO#") }
    };

    public TransformationCollection()
	{
		this.collection = new List<TransformationData>();
	}
	
	public TransformationCollection (List<TransformationData> collection)
	{
		this.collection = collection;
	}

    public TransformationCollection (List<string> hashes)
    {
        List<TransformationData> collection = new List<TransformationData>();
        foreach (string hash in hashes)
        {
            if (TransformationData.hashmap.ContainsKey(hash))
            {
                collection.Add(TransformationData.hashmap[hash]);
            }
        }
        this.collection = collection;
    }
	

	/**
	 *	Adds a TransformationData object to the collection.
	 */
	 
	public void Add(TransformationData transformationData)
	{
		this.collection.Add(transformationData);
	}


    /**
     * Reads a string into a TransformationCollection object.
     */

    public static TransformationCollection Read(string _s, bool fromHash = false)
    {
        MatchCollection collectionCheck = TransformationCollection.readParser["tColParse"].Matches(_s);
        if (collectionCheck.Count == 0)
        {
            throw new Exception("Could not read data string " + _s + " into TransformationCollection.");
        }

        MatchCollection tDataGroups = TransformationCollection.readParser["tDataParse"].Matches(_s);
        if (tDataGroups.Count == 0)
        {
            throw new Exception("Could not find groups for data string " + _s + ".");
        }

        Dictionary<int, string> tDataStrings = new Dictionary<int, string>();

        int i = 0;
        foreach (Match tDataGroup in tDataGroups)
        {
            i += 1;
            tDataStrings.Add(i, tDataGroup.Groups[(fromHash ? 0 : 1)].ToString());
        }

        TransformationCollection tCollection = new TransformationCollection();

        foreach (KeyValuePair<int, string> entry in tDataStrings)
        {
            tCollection.Add(TransformationData.Read(entry.Value, fromHash));
        }
        
        return tCollection;
    }
    

    /**
     * Writes out transformation data to a string.
     */
    
    public string Write(bool toHash = false)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("#tC#");
        foreach (TransformationData tData in this.collection)
        {
            sb.Append(tData.Write(toHash));
        }
        sb.Append("#_tC#");
        return sb.ToString();
    }


    /**
     * Writes out the collection for debugging
     */

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Transformation Collection:");
        sb.Append(Environment.NewLine);
        foreach (TransformationData tData in this.collection)
        {
            sb.Append(tData.ToString());
        }
        return sb.ToString();
    }
}
