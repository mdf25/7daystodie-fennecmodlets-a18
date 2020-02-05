using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class TransformerItemInput : ITransformerItem
{
	public ItemStack itemStack;
    public double prob;
	
	public TransformerItemInput(ItemValue itemValue, int count, double prob = 1.0)
    {
        this.itemStack = new ItemStack(itemValue, count);
        this.prob = NumHelpers.CheckProb(prob);
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

        double itemProb;
        if (!double.TryParse(inputData.Split(',')[2], out itemProb))
        {
            throw new Exception("The item prob could not be parsed as a double.");
        }

        return new TransformerItemInput(new ItemValue(itemId), itemCount, itemProb);
    }


    /**
     * Writes out the TransformerItemInput to a string for saving to a stream.
     */

    public string Write()
    {
        return "#tI#" + itemStack.itemValue.type.ToString() + "," + itemStack.count.ToString() + "," + prob.ToString() + "#_tI#";
    }


    /**
     * Debug writeout
     */
    public override string ToString()
    {
        string name = ItemClass.GetForId(itemStack.itemValue.type).GetItemName();
        string count = this.itemStack.count.ToString();
        string prob = this.prob.ToString();

        return "Transformer Input: " + name + " (" + count + ") with probability " + prob;
    }
}
