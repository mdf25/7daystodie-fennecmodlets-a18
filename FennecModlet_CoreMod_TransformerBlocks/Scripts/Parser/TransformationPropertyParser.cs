using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class TransformationPropertyParser
{
    public TransformationPropertyParser(BlockTransformer blockTransformer)
    {
        this.blockTransformer       = blockTransformer;
        this.blockName              = blockTransformer.GetBlockName();
        this.blockTransformerBlock  = Block.GetBlockValue(this.blockName, true);
    }


    /**
	 *	Parses the transformations properties for the block.
	 */

    public void ParseDynamicProperties()
    {
        this.GetTransformationPropertiesForBlock();
        this.PopulateTransformationData();
        this.CheckAllItemsExist();
        this.ParseTransformationData();
        this.CheckRequiresPower();
        this.CheckRequiresHeat();
    }


    /**
	 * Checks whether there is a property class of Transformations in the list.
	 */

    protected bool HasTransformationPropertyClass()
    {
        return this.blockTransformerBlock.Block.Properties.Classes.ContainsKey(propClassTransformations);
    }


    /**
	 *  Returns a list of Transformations properties.
	 */

    protected void GetTransformationPropertiesForBlock()
    {
        if (this.blockTransformerBlock.type == 0)
        {
            Log.Warning("Transformer block " + this.blockName + " is not identified.");
            return;
        }

        if (!this.HasTransformationPropertyClass())
        {
            throw new ArgumentException("Block " + this.blockName + " has no property class of Transformations.");
        }

        this.transformationPropClass = this.blockTransformerBlock.Block.Properties.Classes[propClassTransformations];
        foreach (KeyValuePair<string, object> entry in this.transformationPropClass.Values.Dict.Dict)
        {
            if (!this.PropStringFormattedCorrectly(entry.Key))
            {
                throw new Exception("The property " + entry.Key + " is not a valid property name.");
            }
            this.transformationProperties.Add(entry.Key);
        }
    }
    

    /**
	 *	Checks that the Transformation properties are defined correctly.
	 */

    protected bool PropStringFormattedCorrectly(string _string)
    {
        if (!_string.StartsWith(this.propValueTransformation))
        {
            return false;
        }
        return ((int)this.GetType(_string) != ((int)TransformationPropertyParser.Type.INVALID));
    }
    

    /**
	 *	Populates the protected dictionaries with max number of transformations, inputs and outputs.
	 */

    protected void PopulateTransformationData()
    {
        Regex rgx = new Regex(this.pattern);
        foreach (string transformationProperty in this.transformationProperties)
        {
            if (transformationProperty.EndsWith(this.timeString))
            {
                continue;
            }

            MatchCollection matches = rgx.Matches(transformationProperty);
            if (matches.Count != 2)
            {
                throw new ArgumentException("Transformation property " + transformationProperty + " is not correctly formatted.");
            }

            int transformationIndex = int.Parse(matches[0].Value);
            int inputOutputIndex = int.Parse(matches[1].Value);

            this.UpdateDictionaryWithHighest(this.GetInputOutputDictionary(transformationProperty), transformationIndex, inputOutputIndex);
            this.AddToListIfNew(this.transformationIndexes, transformationIndex);
        }
    }
    

    /**
	 *	Returns the input or output dictionary to add to, depending on the transformation string.
	 */

    protected Dictionary<int, int> GetInputOutputDictionary(string transformationString)
    {
        switch (this.GetType(transformationString))
        {
            case (int)TransformationPropertyParser.Type.INPUT:
                return this.transformMaxInputs;
            case (int)TransformationPropertyParser.Type.OUTPUT:
                return this.transformMaxOutputs;
        }
        
        throw new ArgumentException("The string " + transformationString + " does not contain input or output data.");
    }
    
    
    /**
     * Gets the transformation type based on the transformation string given.
     */

    protected int GetType(string transformationString)
    {
        if (transformationString.Contains(this.inputString))
        {
            return (int)TransformationPropertyParser.Type.INPUT;
        } 
        else if (transformationString.Contains(this.outputString))
        {
            return (int)TransformationPropertyParser.Type.OUTPUT;
        }
        else if (transformationString.Contains(this.timeString))
        {
            return (int)TransformationPropertyParser.Type.TIME;
        }
        return (int)TransformationPropertyParser.Type.INVALID;
    }

    
    /**
     *  Adds an entry to a list if it is not found.
     */

    protected void AddToListIfNew(List<int> _list, int _value)
    {
        if (!this.ListHas(_list, _value))
        {
            _list.Add(_value);
        }
    }


    /**
     * Checks if a value is found in a list or not.
     */

    protected bool ListHas(List<int> _list, int _value)
    {
        if (_list.Count == 0)
            return false;

        foreach (int existingValue in _list)
        {
            if (_value == existingValue)
            {
                return true;
            }
        }
        return false;
    }
    

    /**
	 *	Update the transformation dictionary with max value.
	 */

    protected void UpdateDictionaryWithHighest(Dictionary<int, int> dict, int key, int newValue)
    {
        if (dict.ContainsKey(key))
        {
            if (dict[key] >= newValue)
            {
                return;
            }
            dict.Remove(key);
        }
        dict.Add(key, newValue);
    }
    

    /**
	 * Checks all items exist.
	 */

    protected void CheckAllItemsExist()
    {
        foreach (int transformationIndex in this.transformationIndexes)
        {
            int maxInputs   = this.transformMaxInputs[transformationIndex];
            int maxOutputs  = this.transformMaxOutputs[transformationIndex];

            for (int i = 1; i <= maxInputs; i += 1)
            {
                string transformProperty = this.propValueTransformation + transformationIndex.ToString() + this.inputString + i.ToString();
                if (!this.transformationPropClass.Values.ContainsKey(transformProperty))
                {
                    Log.Warning("Missing the property " + transformProperty + ". Skipping.");
                    continue;
                }

                string propValue = this.transformationPropClass.Values[transformProperty];

                // If there are commas, we need to split out just to get the item value.
                string itemName = (propValue.Contains(",") ? propValue.Split(',')[0] : propValue).Trim();
                ItemValue item = ItemClass.GetItem(itemName, false);

                // Add the item to the dictionary if it was defined.
                this.InsertIfNew(this.itemCheck, item, (item.type != 0));
            }

            for (int j = 1; j <= maxOutputs; j += 1)
            {
                string transformProperty = this.propValueTransformation + transformationIndex.ToString() + this.outputString + j.ToString();
                if (!this.transformationPropClass.Values.ContainsKey(transformProperty))
                {
                    Log.Warning("Missing the property " + transformProperty + ". Skipping.");
                    continue;
                }

                string propValue = this.transformationPropClass.Values[transformProperty];

                // If there are commas, we need to split out just to get the item value.
                string itemName = (propValue.Contains(",") ? propValue.Split(',')[0] : propValue).Trim();
                ItemValue item = ItemClass.GetItem(itemName, false);

                // Add the item to the dictionary if it was defined.
                this.InsertIfNew(this.itemCheck, item, (item.type != 0));
            }
        }

        // Put all items not found into a list so that we can print them to console.
        List<string> missingItems = new List<string>();
        foreach (KeyValuePair<ItemValue, bool> entry in this.itemCheck)
        {
            if (entry.Value == false)
            {
                missingItems.Add(entry.Key.ItemClass.GetItemName());
            }
        }

        this.allItemsValid = (missingItems.Count == 0);
        if (!this.allItemsValid)
        {
            string missingItemsToPrint = String.Join(", ", missingItems);
            throw new Exception("The following transformation items for " + this.blockName + " do not exist in items.xml: " + missingItems);
        }
    }
    

    /**
	 *	Inserts a key into the dictionary only if it's a new key.
	 */

    protected void InsertIfNew(Dictionary<ItemValue, bool> dict, ItemValue _itemValue, bool _bool)
    {
        if (dict.ContainsKey(_itemValue))
        {
            return;
        }
        dict.Add(_itemValue, _bool);
    }
    

    /**
	 *	Takes all transformation data and creates a transformation collection from it.
	 */

    protected void ParseTransformationData()
    {
        Dictionary<int, TransformationData> parsedTransformationData = new Dictionary<int, TransformationData>();
        this.AddTransformationDataEntries(parsedTransformationData);
        this.AddTimeDataToTransformationData(parsedTransformationData);
        this.AddIOToTransformationData(parsedTransformationData);
        this.AddToHashmap(parsedTransformationData);
        this.ConvertToTransformationCollection(parsedTransformationData);
    }
    

    /**
     * Adds the keys into the transformation data dictionary for each transformation found.
     */

    protected void AddTransformationDataEntries(Dictionary<int, TransformationData> tData)
    {
        foreach (int transformationIndex in this.transformationIndexes)
        {
            tData.Add(transformationIndex, new TransformationData());
        }
    }

    
    /**
     * Adds the set time from XML to the transformation data.
     */

    protected void AddTimeDataToTransformationData(Dictionary<int, TransformationData> tData)
    {
        foreach (int transformationIndex in this.transformationIndexes)
        {
            string timeProperty = this.propValueTransformation + transformationIndex.ToString() + this.timeString;
            tData[transformationIndex].transformationTime = this.GetTransformationTime(timeProperty);
        }
    }
    

    /**
     * Gets the transformation time as a double for the transformation property. If none found or parse error, return as a defult double.
     */

    protected double GetTransformationTime(string timeProperty)
    {
        double time;
        if (!this.transformationPropClass.Values.ContainsKey(timeProperty))
        {
            return this.defaultTransformationTime;
        }

        if (!StringParsers.TryParseDouble(this.transformationPropClass.Values[timeProperty].Trim(), out time))
        {
            Log.Warning("Could not parse double for " + timeProperty + ". Using default time of " + this.defaultTransformationTime + ".");
            return this.defaultTransformationTime;
        }
        return time;

    }
    

    /**
     * Adds the Transformation{X}_Input{Y} fields to the tData structure.
     */

    protected void AddIOToTransformationData(Dictionary<int, TransformationData> tData)
    {
        foreach (int transformationIndex in this.transformationIndexes)
        {
            this.AddToTData(tData, transformationIndex, this.transformMaxInputs[transformationIndex], (int)TransformationPropertyParser.Type.INPUT);
            this.AddToTData(tData, transformationIndex, this.transformMaxOutputs[transformationIndex], (int)TransformationPropertyParser.Type.OUTPUT);
        }
    }


    /**
     * Adds an element to transformation data.
     */

    protected void AddToTData(Dictionary<int, TransformationData> tData, int transformationIndex, int maxIOIndex, int type)
    {
        string ioString;

        switch (type)
        {
            case ((int)TransformationPropertyParser.Type.INPUT):
                ioString = this.inputString;
                break;
            case ((int)TransformationPropertyParser.Type.OUTPUT):
                ioString = this.outputString;
                break;
            default:
                Log.Warning("Property is not an input or output string. Skipping.");
                return;
        }

        for (int i = 1; i <= maxIOIndex; i += 1)
        {
            string ioProp = this.propValueTransformation + transformationIndex.ToString() + ioString + i.ToString();
            if (!this.transformationPropClass.Values.ContainsKey(ioProp))
            {
                Log.Warning("The following property is not defined: " + ioProp + ". Skipping.");
                continue;
            }

            tData[transformationIndex].Add(this.GetITransformerItemFor(ioProp));
        }
    }



    /**
     *  Converts the property value string into a TransformerItemInput or TransformerItemOutput.
     */

    protected ITransformerItem GetITransformerItemFor(string itemPropName)
    {
        string itemPropData = this.transformationPropClass.Values[itemPropName];

        int type = this.GetType(itemPropName);
        if ((int)type != ((int)TransformationPropertyParser.Type.INPUT) && (int)type != ((int)TransformationPropertyParser.Type.OUTPUT))
        {
            throw new Exception("Item property name " + itemPropName + " is not an input or output string.");
        }

        string itemName = "";
        int itemCount   = defaultItemCount;
        double itemProb = defaultItemProb;

        if (!itemPropData.Contains(","))
        {
            itemName    = itemPropData.Trim();
            itemCount   = this.defaultItemCount;
        }
        else
        {
            string[] propData = itemPropData.Split(',');
            itemName = propData[0].Trim();
            if (!int.TryParse(propData[1].Trim(), out itemCount))
            {
                Log.Warning("Could not parse " + propData[1] + " as int value. Using default " + this.defaultItemCount + ".");
                itemCount = this.defaultItemCount;
            }

            if (type.Equals((int)TransformationPropertyParser.Type.OUTPUT))
            {
                itemProb = this.defaultItemProb;
                if (propData.GetLength(0) > 2)
                {
                    if (!double.TryParse(propData[2].Trim(), out itemProb))
                    {
                        Log.Warning("Could not parse " + propData[2] + " as a probability double.");
                        itemProb = this.defaultItemProb;
                    }
                }
            }
        }
        
        return this.GetITransformerItemForType(type, itemName, itemCount, itemProb);
    }


    /**
     * Returns a TransformerItemInput or TransformerItemOutput for the ITransformerItem passed in.
     */

    protected ITransformerItem GetITransformerItemForType(int type, string itemName, int itemCount, double itemProb)
    {
        switch (type)
        {
            case ((int)TransformationPropertyParser.Type.INPUT):
                return new TransformerItemInput(ItemClass.GetItem(itemName), itemCount);
            case ((int)TransformationPropertyParser.Type.OUTPUT):
                return new TransformerItemOutput(ItemClass.GetItem(itemName), itemCount, itemProb);
            default:
                throw new Exception("Item property name " + itemName + " is not an input or output string.");
        }
    }


    /**
     * Adds the transformation data to hashmap.
     */

    protected void AddToHashmap(Dictionary<int, TransformationData> parsedTransformationData)
    {
        foreach (KeyValuePair<int, TransformationData> entry in parsedTransformationData)
        {
            TransformationData.AddToHashmap(entry.Value);
        }
    }


    /**
     * Converts the list of transformation data to a collection.
     */

    protected void ConvertToTransformationCollection(Dictionary<int, TransformationData> parsedTransformationData)
    {
        List<TransformationData> transformationList = new List<TransformationData>();
        foreach (KeyValuePair<int, TransformationData> entry in parsedTransformationData)
        {
            transformationList.Add(entry.Value);
        }
        this.collection = new TransformationCollection(transformationList);
    }


    /**
     * Checks whether the block requires power and parses the power sources.
     */

    protected void CheckRequiresPower()
    {
        if (!this.blockTransformerBlock.Block.Properties.Values.ContainsKey(propRequirePower))
        {
            this.requirePower = false;
            this.powerSources = new List<string>() { "electricwirerelay" };
            return;
        }

        string requirePowerValue = blockTransformerBlock.Block.Properties.Values[propRequirePower].ToString();
        bool requirePower;
        this.requirePower = (StringParsers.TryParseBool(requirePowerValue, out requirePower) ? requirePower : false);

        this.powerSources = new List<string>();
        if (!this.blockTransformerBlock.Block.Properties.Values.ContainsKey(propPowerSources))
        {
            this.powerSources.Add("electricwirerelay");
        }
        else
        {
            string[] powerSources = this.blockTransformerBlock.Block.Properties.Values[propPowerSources].Split(',');
            for (int i = 0; i < powerSources.Length; i += 1)
            {
                this.powerSources.Add(powerSources[i].Trim());
            }
        }

        this.CheckBlocksDefined(this.powerSources);
    }


    /**
     * Checks whether block requires heat and parses the heat sources.
     */

    protected void CheckRequiresHeat()
    {
        if (!this.blockTransformerBlock.Block.Properties.Values.ContainsKey(propRequireHeat))
        {
            this.requireHeat = false;
            this.heatSources = new List<string>() { "campfire" };
            return;
        }

        string requireHeatValue = blockTransformerBlock.Block.Properties.Values[propRequireHeat].ToString();
        bool requireHeat;
        this.requireHeat = (StringParsers.TryParseBool(requireHeatValue, out requireHeat) ? requireHeat : false);

        this.heatSources = new List<string>();
        if (!this.blockTransformerBlock.Block.Properties.Values.ContainsKey(propHeatSources))
        {
            this.heatSources.Add("canpfire");
        }
        else
        {
            string[] heatSources = this.blockTransformerBlock.Block.Properties.Values[propHeatSources].Split(',');
            for (int i = 0; i < heatSources.Length; i += 1)
            {
                this.heatSources.Add(heatSources[i].Trim());
            }
        }

        this.CheckBlocksDefined(this.heatSources);
    }


    /**
     * Checks whether a block is defined or not.
     */

    protected void CheckBlocksDefined(List<string> blockNames)
    {
        foreach (string blockName in blockNames)
        {
            Block block = Block.GetBlockByName(blockName);
            if (block == null)
            {
                Log.Error("Block with name " + blockName + " not found. Removing.");
                blockNames.Remove(blockName);
            }
        }
    }


    // Block input data
    protected BlockTransformer blockTransformer;
    protected string blockName;
    protected BlockValue blockTransformerBlock;
    protected DynamicProperties transformationPropClass;

    // The name of the transformations class and properties
    protected string propClassTransformations           = "Transformations";
    protected string propValueTransformation            = "Transformation";
    protected string propRequirePower                   = "RequirePower";
    protected string propPowerSources                   = "PowerSources";
    protected string propRequireHeat                    = "RequireHeat";
    protected string propHeatSources                    = "HeatSources";

    // Used to detect inputs, outputs, and time strings
    protected string inputString                        = "_Input";
    protected string outputString                       = "_Output";
    protected string timeString                         = "_Time";

    protected enum Type { INVALID = 0, INPUT = 1, OUTPUT = 2, TIME = 3 };

    // Used for extracting transformation numbers from the string.
    protected string pattern                            = @"([0-9]+)";

    // A list holding all transformation properties and how many.
    protected List<string> transformationProperties     = new List<string>();
    protected List<int> transformationIndexes           = new List<int>();

    // These dictionaries hold how many inputs and outputs each transformation has.
    protected Dictionary<int, int> transformMaxInputs   = new Dictionary<int, int>();
    protected Dictionary<int, int> transformMaxOutputs  = new Dictionary<int, int>();

    // This list contains all item classes defined in the transformation class, for checking.
    protected Dictionary<ItemValue, bool> itemCheck     = new Dictionary<ItemValue, bool>();

    protected bool allItemsValid                        = true;

    // Default values
    protected int defaultItemCount                      = 1;
    protected double defaultTransformationTime          = 2.0;
    protected double defaultItemProb                    = 1.0;
    
    // The parsed transformation data.
    public TransformationCollection collection;
    public bool requirePower;
    public List<string> powerSources;
    public bool requireHeat;
    public List<string> heatSources;
}
