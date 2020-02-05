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
        this.transformationBlockProps = this.blockTransformerBlock.Block.Properties;

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
        return (this.GetType(_string) != TransformationStringType.INVALID);
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
            case TransformationStringType.INPUT:
                return this.transformMaxInputs;
            case TransformationStringType.OUTPUT:
                return this.transformMaxOutputs;
        }
        
        throw new ArgumentException("The string " + transformationString + " does not contain input or output data.");
    }
    
    
    /**
     * Gets the transformation type based on the transformation string given.
     */

    protected TransformationStringType GetType(string transformationString)
    {
        if (transformationString.Contains(this.inputString))
        {
            return TransformationStringType.INPUT;
        } 
        else if (transformationString.Contains(this.outputString))
        {
            return TransformationStringType.OUTPUT;
        }
        else if (transformationString.Contains(this.timeString))
        {
            return TransformationStringType.TIME;
        }
        return TransformationStringType.INVALID;
    }

    
    /**
     *  Adds an entry to a list if it is not found.
     */

    protected void AddToListIfNew(List<int> _list, int _value)
    {
        if (!_list.Contains(_value))
        {
            _list.Add(_value);
        }
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
        if (this.transformationIndexes.Count == 0)
        {
            throw new Exception("No indexes were found.");
        }

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
                this.InsertIfNew(this.itemCheck, item, (item.type > 0));
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
                this.InsertIfNew(this.itemCheck, item, (item.type > 0));
            }
        }

        // Put all items not found into a list so that we can print them to console.
        List<string> missingItems = new List<string>();
        foreach (KeyValuePair<ItemValue, bool> entry in this.itemCheck)
        {
            Log.Warning("Checking id " + entry.Key.GetItemId());
            if (entry.Value == false)
            {
                Log.Warning("Not found.");
                missingItems.Add(entry.Key.ItemClass.GetItemName());
            }
        }

        this.allItemsValid = (missingItems.Count == 0);
        if (!this.allItemsValid)
        {
            string missingItemsToPrint = String.Join(", ", missingItems);
            throw new Exception("The following transformation items for " + this.blockName + " do not exist in items.xml: " + missingItemsToPrint);
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
            this.AddToTData(tData, transformationIndex, this.transformMaxInputs[transformationIndex], TransformationStringType.INPUT);
            this.AddToTData(tData, transformationIndex, this.transformMaxOutputs[transformationIndex], TransformationStringType.OUTPUT);
        }
    }


    /**
     * Adds an element to transformation data.
     */

    protected void AddToTData(Dictionary<int, TransformationData> tData, int transformationIndex, int maxIOIndex, TransformationStringType type)
    {
        string ioString;

        switch (type)
        {
            case (TransformationStringType.INPUT):
                ioString = this.inputString;
                break;
            case (TransformationStringType.OUTPUT):
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

        TransformationStringType type = this.GetType(itemPropName);
        if (type != TransformationStringType.INPUT && type != TransformationStringType.OUTPUT)
        {
            throw new Exception("Item property name " + itemPropName + " is not an input or output string.");
        }

        string itemName = "";
        int itemCount   = defaultItemCount;
        double itemProb = defaultItemProb;

        if (!itemPropData.Contains(","))
        {
            itemName    = itemPropData.Trim();
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

            itemProb = this.defaultItemProb;
            if (propData.Length > 2)
            {
                if (!double.TryParse(propData[2].Trim(), out itemProb))
                {
                    Log.Warning("Could not parse " + propData[2] + " as a probability double.");
                    itemProb = this.defaultItemProb;
                }
            }
          
        }
        
        return this.GetITransformerItemForType(type, itemName, itemCount, itemProb);
    }


    /**
     * Returns a TransformerItemInput or TransformerItemOutput for the ITransformerItem passed in.
     */

    protected ITransformerItem GetITransformerItemForType(TransformationStringType type, string itemName, int itemCount, double itemProb)
    {
        switch (type)
        {
            case (TransformationStringType.INPUT):
                return new TransformerItemInput(ItemClass.GetItem(itemName), itemCount, itemProb);
            case (TransformationStringType.OUTPUT):
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
        this.requirePower = false;
        this.powerSources = new List<string>() { "electricwirerelay" };

        string requirePowerValue;
        if (!this.PropExists(propRequirePower, out requirePowerValue))
        {
            return;
        }
        
        if (!StringParsers.TryParseBool(requirePowerValue, out this.requirePower))
        {
            throw new Exception("Require power could noot be parsed as a boolean value.");
        }
        
        this.powerSources = new List<string>();
        string powerSources;
        if (this.PropExists(propPowerSources, out powerSources))
        {
            this.powerSources = StringHelpers.WriteStringToList(powerSources);
            this.CheckBlocksDefined(this.powerSources);
            return;
        }
        this.powerSources.Add("electricwirerelay");
    }


    /**
     * Checks whether block requires heat and parses the heat sources.
     */

    protected void CheckRequiresHeat()
    {
        this.requireHeat = false;
        this.heatSources = new List<string>() { "campfire" };

        string requireHeatValue;
        if (!this.PropExists(propRequireHeat, out requireHeatValue))
        {
            return;
        }

        if (!StringParsers.TryParseBool(requireHeatValue, out this.requireHeat))
        {
            throw new Exception("Could not parse require heat parameter as a boolean.");
        }

        this.heatSources = new List<string>();
        string heatSources;
        if (this.PropExists(propHeatSources, out heatSources))
        {
            this.heatSources = StringHelpers.WriteStringToList(heatSources);
            this.CheckBlocksDefined(this.heatSources);
        }
        this.heatSources.Add("campfire");
    }


    /**
     * Checks whether nearby blocks are required and parses the nearby block sources.
     */

    protected void CheckRequireBlocksNearby()
    {
        this.requireNearbyBlocks = false;
        this.nearbyBlockRange = new Vector3i(1, 1, 1);
        this.nearbyBlockCount = 0;
        this.nearbyBlockNames = new List<string>();
        this.nearbyBlockTags = new List<string>();
        this.requireAllTags = false;

        // Checks whether we need nearby blocks or not. If not, just exit out as we no longer need to specify additional data.
        string requireNearbyBlocks;
        if (!this.PropExists(propRequireNearbyBlocks, out requireNearbyBlocks))
        {
            return;
        }
        
        if (!StringParsers.TryParseBool(requireNearbyBlocks, out this.requireNearbyBlocks))
        {
            throw new Exception("Require nearby blocks property needs to be true or false.");
        }

        // Check whether we have a range specified. If so, we need to parse it.
        string nearbyBlockRange;
        if (this.PropExists(propNearbyBlockRange, out nearbyBlockRange))
        {   
            List<string> blockRanges = StringHelpers.WriteStringToList(nearbyBlockRange);
            switch (blockRanges.Count)
            {
                case 1: // Clone the first value twice to make it a list of 3 values.
                    blockRanges.Add(blockRanges[0]);
                    blockRanges.Add(blockRanges[0]);
                    break;
                case 3:
                    break;
                default:
                    throw new Exception("Block range must either be a single integer or a comma separated list of 3 integers.");
            }

            int rangeX, rangeY, rangeZ;
            if (!int.TryParse(blockRanges[0], out rangeX))
            {
                throw new Exception("Could not parse X range as an int.");
            }
            if (!int.TryParse(blockRanges[1], out rangeY))
            {
                throw new Exception("Could not parse Y range as an int.");
            }
            if (!int.TryParse(blockRanges[2], out rangeZ))
            {
                throw new Exception("Could not parse Z range as an int.");
            }

            if (rangeX < 0 | rangeY < 0 | rangeZ < 0)
            {
                throw new Exception("Ranges for the block to search must be non-negative.");
            }

            this.nearbyBlockRange = new Vector3i(rangeX, rangeY, rangeZ);
        }

        // Check whether we have a count of nearby blocks.
        string nearbyBlocks;
        if (this.PropExists(propNearbyBlockCount, out nearbyBlocks))
        { 
            if (!int.TryParse(nearbyBlocks, out this.nearbyBlockCount))
            {
                throw new Exception("Could not parse nearby block count as integer value.");
            }
            if (this.nearbyBlockCount < 0)
            {
                throw new Exception("Nearby block count needs to be non-negative.");
            }
        }

        // Checks whether the blocks specified exist for block names.
        string nearbyBlockNames;
        if (this.PropExists(propNearbyBlockNames, out nearbyBlockNames))
        {
            this.nearbyBlockNames = StringHelpers.WriteStringToList(nearbyBlockNames);
            this.CheckBlocksDefined(this.nearbyBlockNames);
        }

        // Checks block tags.
        string nearbyBlockTags;
        if (this.PropExists(propNearbyBlockTags, out nearbyBlockTags))
        {
            this.nearbyBlockTags = StringHelpers.WriteStringToList(nearbyBlockTags);
        }

        // Checks require all tags.
        string requireAllTags;
        if (this.PropExists(propRequireAllTags, out requireAllTags))
        {
            if (!StringParsers.TryParseBool(requireAllTags, out this.requireAllTags))
            {
                throw new Exception("Could not parse require all tags parameter as a boolean value.");
            }
        }
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


    /**
     * Checks whether a property is defined, and returns it as a string if so.
     */

    protected bool PropExists(string key, out string value)
    {
        value = "";
        if (this.transformationBlockProps.Values.ContainsKey(key))
        {
            value = this.transformationBlockProps.Values[key].ToString();
            return true;
        }
        return false;
    }



    // Block input data
    protected BlockTransformer blockTransformer;
    protected string blockName;
    protected BlockValue blockTransformerBlock;
    protected DynamicProperties transformationPropClass;
    protected DynamicProperties transformationBlockProps;

    // The name of the transformations class and properties to parse.
    protected string propClassTransformations           = "Transformations";
    protected string propValueTransformation            = "Transformation";
    protected string propRequirePower                   = "RequirePower";
    protected string propPowerSources                   = "PowerSources";
    protected string propRequireHeat                    = "RequireHeat";
    protected string propHeatSources                    = "HeatSources";
    protected string propRequireNearbyBlocks            = "RequireNearbyBlocks";
    protected string propNearbyBlockRange               = "NearbyBlockRange";
    protected string propNearbyBlockCount               = "NearbyBlockCount";
    protected string propNearbyBlockNames               = "NearbyBlockNames";
    protected string propNearbyBlockTags                = "NearbyBlockTags";
    protected string propRequireAllTags                 = "RequireAllTags";
    
    // Used to detect inputs, outputs, and time strings
    protected string inputString                        = "_Input";
    protected string outputString                       = "_Output";
    protected string timeString                         = "_Time";

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
    public bool requireNearbyBlocks;
    public Vector3i nearbyBlockRange;
    public int nearbyBlockCount;
    public List<string> nearbyBlockNames;
    public List<string> nearbyBlockTags;
    public bool requireAllTags;
}
