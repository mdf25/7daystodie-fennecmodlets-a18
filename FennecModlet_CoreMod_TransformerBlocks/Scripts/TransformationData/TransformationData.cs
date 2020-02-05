using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

public class TransformationData
{
    public static Dictionary<string, TransformationData> hashmap;
    public Dictionary<string, List<ITransformerItem>> inputsOutputs;
	public double transformationTime;
	public string inputs = "Inputs";
	public string outputs = "Outputs";
    
	public TransformationData()
	{
		this.inputsOutputs = new Dictionary<string, List<ITransformerItem>>();
	}
	

    /**
     * If the transformation data is initialised on construction, add hashmap automatically.
     */

    public TransformationData(List<ITransformerItem> items, double transformationTime) : this()
    {
        this.transformationTime = transformationTime;
        foreach (ITransformerItem item in items)
        {
            this.Add(item);
        }
        this.AddToHashmap();
    }


	/**
	 *	Adds a new item to the input or output area of the dictionary, depending on the type.
	 */

	public void Add(ITransformerItem transformerItem)
	{
        List<ITransformerItem> transformerItems;
        string key = this.GetKey(transformerItem);
        if (key == "")
        {
            Log.Warning("Empty Key.");
            return;
        }
        
		if (this.inputsOutputs.ContainsKey(key)) {
			transformerItems = this.inputsOutputs[key];
			transformerItems.Add(transformerItem);
			this.inputsOutputs.Remove(key);
			this.inputsOutputs.Add(key, transformerItems);
            return;
		}
        
        transformerItems = new List<ITransformerItem>();
        transformerItems.Add(transformerItem);
        this.inputsOutputs.Add(key, transformerItems);
	}
	

	/**
	 *	Checks which key to return from the dictionary based on types.
	 */
	
	public string GetKey(ITransformerItem transformerItem)
	{
		if (transformerItem is TransformerItemInput)
		{
			return this.inputs;
		}
		if (transformerItem is TransformerItemOutput)
		{
			return this.outputs;
		}
        return "";
	}
	

	/**
	 *	Checks whether the data has inputs specified.
	 */
	public bool HasInputs()
	{
        return (this.inputsOutputs.ContainsKey(inputs));
	}
	

	/**
	 *	Checks whether the data has outputs specified.
	 */
	public bool HasOutputs()
	{
        return (this.inputsOutputs.ContainsKey(outputs));
	}
	

	/**
	 *	Retrieves all inputs from the item collection.
	 */
	
	public Dictionary<ItemStack, double> GetAllInputs()
	{
        Dictionary<ItemStack, double> inputTable = new Dictionary<ItemStack, double>();
        if (!this.HasInputs())
        {
            return inputTable;
        }

        foreach (TransformerItemInput transformerItemInput in this.inputsOutputs[this.inputs])
        {
            inputTable.Add(transformerItemInput.itemStack, transformerItemInput.prob);
        }

        return inputTable;
    }


    /**
     * Returns a list of all inputs after probability calculations.
     */

    public List<ItemStack> GetAllInputsAfterProbabilityCalculation()
    {
        List<ItemStack> list = new List<ItemStack>();
        if (!this.HasInputs())
        {
            return list;
        }

        foreach (TransformerItemInput transformerItemInput in this.inputsOutputs[this.inputs])
        {
            double randomDouble = RandomStatic.Next();
            if (randomDouble <= transformerItemInput.prob)
            {
                list.Add(transformerItemInput.itemStack);
            }
        }

        return list;
    }

	
    
    /**
     *  Returns a dictionary of output itemstacks with their probabilities.
     */

    public Dictionary<ItemStack, double> GetAllOutputs()
    {
        Dictionary<ItemStack, double> outputTable = new Dictionary<ItemStack, double>();
        if (!this.HasOutputs())
        {
            return outputTable;
        }

        foreach (TransformerItemOutput transformerItemOutput in this.inputsOutputs[this.outputs])
        {
            outputTable.Add(transformerItemOutput.itemStack, transformerItemOutput.prob);
        }

        return outputTable;
    }
    

	/**
	 *	Retrieves all outputs from the item collection in a list, based on probabilities.
	 */
	
	public List<ItemStack> GetAllOutputsAfterProbabilityCalculation()
	{	
		List<ItemStack> list = new List<ItemStack>();
        if (!this.HasOutputs())
        {
            return list;
        }

		foreach (TransformerItemOutput transformerItemOutput in this.inputsOutputs[this.outputs])
		{
			double randomDouble = RandomStatic.Next();
			if (randomDouble <= transformerItemOutput.prob)
			{
				list.Add(transformerItemOutput.itemStack);
			}
		}
		
		return list;
	}
	

	/**
	 *	Checks whether all inputs are fulfilled, given a list of item stacks.
	 */
	 
	public bool HasAllInputs(ItemStack[] itemStacks, out List<Tuple<int, ItemStack>>locations)
	{
        locations = new List<Tuple<int, ItemStack>>();
        List<Tuple<int, ItemStack>> foundSoFar = new List<Tuple<int, ItemStack>>();

        // This is required to stop nullref, since sometimes null is passed in from an inventory.
        if (itemStacks == null)
        {
            return false;
        }

        if (!this.HasInputs())
        {
            return false;
        }

        // First check we have all inputs, before probability calculation.
		Dictionary<ItemStack, double> inputs = this.GetAllInputs();
        List<ItemStack> inputsToTake = this.GetAllInputsAfterProbabilityCalculation();
		int stacksToMatch = inputs.Count;
		int matches = 0;

        if (inputs.Count == 0)
        {
            return true;
        }

		foreach (KeyValuePair<ItemStack, double> entry in inputs)
		{
			for (int i = 0; i < itemStacks.Length; i += 1)
			{
                if (matches >= stacksToMatch)
                {
                    break;
                }

                if (itemStacks[i] == null)
                {
                    continue;
                }

                if (itemStacks[i].IsEmpty())
                {
                    continue;
                }
                
				if (itemStacks[i].itemValue.type != entry.Key.itemValue.type)
				{
					continue;
				}

                if (itemStacks[i].count >= entry.Key.count)
				{
                    matches += 1;
                    // If, after probability calc, we have this input, add the location.
                    if (inputsToTake.Contains(entry.Key))
                    {
                        foundSoFar.Add(new Tuple<int, ItemStack>(i, entry.Key));
                    }
				}
			}
		}

        if (matches < stacksToMatch)
        {
            return false;
        }

        locations = foundSoFar;
        return true;
	}


    /**
     * Reads transformation data from a string and outputs a new TransformationData object.
     */

    public static TransformationData Read(string _s, bool fromHash = false)
    {
        TransformationData tData;

        if (fromHash)
        {
            Log.Out("String: " + _s);
            Match match = TransformationCollection.readParser["tDataParse"].Match(_s);
            string hash = match.Groups[1].ToString();
            Log.Warning("Looking for hash: " + hash);
            if (TransformationData.GetTransformationDataWithHash(hash) == null)
            {
                Log.Out("Here's your problem.");
            }

            return TransformationData.GetTransformationDataWithHash(hash);
        }

        tData = new TransformationData();
        TransformationData.ReadTime(_s, ref tData);
        TransformationData.ReadInputs(_s, ref tData);
        TransformationData.ReadOutputs(_s, ref tData);
        TransformationData.AddToHashmap(tData);
        return tData;
    }
    

    /**
     * Reads the time string from the TransformationData string.
     */

    private static void ReadTime(string _s, ref TransformationData tData)
    {
        MatchCollection timeCheck = TransformationCollection.readParser["tTimeParse"].Matches(_s);
        if (timeCheck.Count == 0)
        {
            throw new Exception("No time parameter found in string " + _s + ".");
        }

        double time;
        if (!StringParsers.TryParseDouble(timeCheck[0].Groups[1].ToString(), out time))
        {
            throw new Exception("Could not parse " + timeCheck[0].Value.ToString() + " as a double.");
        }
        tData.transformationTime = time;
    }
    

    /**
     * Reads all inputs from the TransformationData input strings
     */

    private static void ReadInputs(string _s, ref TransformationData tData)
    {
        MatchCollection inputCheck = TransformationCollection.readParser["tInputParse"].Matches(_s);
        if (inputCheck.Count == 0)
        {
            throw new Exception("No inputs found in string " + _s + ".");
        }

        foreach (Match inputMatch in inputCheck)
        {
            tData.Add(TransformerItemInput.Read(inputMatch.ToString()));
        }
    }


    /**
     * Reads all outputs from the TransformationData output strings.
     */

    private static void ReadOutputs(string _s, ref TransformationData tData)
    {
        MatchCollection outputCheck = TransformationCollection.readParser["tOutputParse"].Matches(_s);
        if (outputCheck.Count == 0)
        {
            throw new Exception("No outputs found in string " + _s + ".");
        }

        foreach (Match outputMatch in outputCheck)
        {
            tData.Add(TransformerItemOutput.Read(outputMatch.ToString()));
        }
    }


    /**
     * Writes the transformation data to a string for saving to a stream.
     */

    public string Write(bool toHash = false)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("#tD#");
        if (toHash)
        {
            sb.Append(this.GetMD5Hash());
        }
        else
        {
            sb.Append("#tT#");
            sb.Append(this.transformationTime.ToString());
            sb.Append("#_tT#");
            foreach (TransformerItemInput tInputs in this.inputsOutputs[this.inputs])
            {
                sb.Append(tInputs.Write());
            }
            foreach (TransformerItemOutput tOutputs in this.inputsOutputs[this.outputs])
            {
                sb.Append(tOutputs.Write());
            }
        }
        sb.Append("#_tD#");
        return sb.ToString();
    }


    /**
     * Returns string for debugging.
     */

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Transformation Data:");
        sb.Append(Environment.NewLine);
        foreach (TransformerItemInput input in this.inputsOutputs[this.inputs])
        {
            sb.Append("   ");
            sb.Append(input.ToString());
            sb.Append(Environment.NewLine);
        }
        foreach (TransformerItemOutput output in this.inputsOutputs[this.outputs])
        {
            sb.Append("   ");
            sb.Append(output.ToString());
            sb.Append(Environment.NewLine);
        }
        return sb.ToString();
    }
    
    
    /**
     * Returns whether this is the same as another.
     */

    public bool IsSameAs(TransformationData _other)
    {
        return (this.GetMD5Hash() == _other.GetMD5Hash());
    }


    /**
     * Returns whether this is the same as another, if only the hash is known.
     */

    public bool IsSameAs(string hash)
    {
        return (this.GetMD5Hash() == hash);
    }
    

    /**
     * Returns the md5 hash for this object.
     */

    public string GetMD5Hash()
    {
        return MD5Hash.Calculate(this.Write());
    }


    /**
     * Returns a transformation data object from hash map.
     */

    public static TransformationData GetTransformationDataWithHash(string hash)
    {
        if (TransformationData.hashmap.ContainsKey(hash))
        {
            return TransformationData.hashmap[hash];
        }
        return null;
    }


    /**
     * Allows a transformation object to be added to hash map externally.
     */

    public static bool AddToHashmap(TransformationData tData)
    {
        if (TransformationData.hashmap == null)
        {
            TransformationData.hashmap = new Dictionary<string, TransformationData>();
        }

        string hash = tData.GetMD5Hash();
        if (!TransformationData.hashmap.ContainsKey(hash))
        {
            TransformationData.hashmap.Add(hash, tData);
            return true;
        }
        return false;
    }


    /**
     * If this class wants to add a hash map, this is done privately.
     */

    private bool AddToHashmap()
    {
        return TransformationData.AddToHashmap(this);
    }
}
