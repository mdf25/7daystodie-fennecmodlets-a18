using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class TransformerItemInput : ITransformerItem
{
	public ItemStack itemStack;
	
	public TransformerItemInput(ItemValue itemValue, int count)
	{
		this.itemStack = new ItemStack(itemValue, count);
	}


    /**
     * Reads a trasnformation input string and returns it as a TransformerItemInput if valid.
     */

    public static TransformerItemInput Read(string _s)
    {
        Match inputCheck = TransformationCollection.readParser["tInputParse"].Match(_s);
        if (!inputCheck.Success)
        {
            throw new Exception("No inputs found in string " + _s + ".");
        }

        string inputData = inputCheck.Groups[1].ToString();

        int itemId;
        if (!int.TryParse(inputData.Split(',')[0], out itemId))
        {
            throw new Exception("The item ID could not be parsed as an integer.");
        }

        int itemCount;
        if (!int.TryParse(inputData.Split(',')[1], out itemCount))
        {
            throw new Exception("The item count could not be parsed as an integer.");
        }

        return new TransformerItemInput(new ItemValue(itemId), itemCount);
    }


    /**
     * Writes out the TransformerItemInput to a string for saving to a stream.
     */

    public string Write()
    {
        return "#tI#" + itemStack.itemValue.type.ToString() + "," + itemStack.count.ToString() + "#_tI#";
    }


    /**
     * Debug writeout
     */
    public override string ToString()
    {
        string name = ItemClass.GetForId(itemStack.itemValue.type).GetItemName();
        string count = this.itemStack.count.ToString();

        return "Transformer Input: " + name + " (" + count + ")";
    }
}
