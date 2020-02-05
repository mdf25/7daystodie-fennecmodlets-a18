using System;
using System.Text.RegularExpressions;

public class TransformerItemOutput : ITransformerItem
{
	public ItemStack itemStack;
	public double prob;
	
	public TransformerItemOutput(ItemValue itemValue, int count, double prob = 1.0)
	{
		this.itemStack 	= new ItemStack(itemValue, count);
		this.prob		= NumHelpers.CheckProb(prob);
	}


    /**
     * Reads a string to return a TransformerItemOutput.
     */

    public static TransformerItemOutput Read(string _s)
    {
        Match outputCheck = TransformationCollection.readParser["tOutputParse"].Match(_s);
        if (!outputCheck.Success)
        {
            throw new Exception("No outputs found in string " + _s + ".");
        }

        string outputData = outputCheck.Groups[1].ToString();

        int itemId;
        if (!int.TryParse(outputData.Split(',')[0], out itemId))
        {
            throw new Exception("The item ID could not be parsed as an integer.");
        }

        int itemCount;
        if (!int.TryParse(outputData.Split(',')[1], out itemCount))
        {
            throw new Exception("The item count could not be parsed as an integer.");
        }

        double itemProb;
        if (!double.TryParse(outputData.Split(',')[2], out itemProb))
        {
            throw new Exception("The item prob could not be parsed as a double.");
        }
        
        return new TransformerItemOutput(new ItemValue(itemId), itemCount, itemProb); 
    }


    /**
     * Writes the TransformerItemOutput to a string for saving to stream.
     */

    public string Write()
    {
        return "#tO#" + itemStack.itemValue.type.ToString() + "," + itemStack.count.ToString() + "," + this.prob.ToString() + "#_tO#";
    }


    /**
     * Debug writeout
     */

    public override string ToString()
    {
        string name     = ItemClass.GetForId(itemStack.itemValue.type).GetItemName();
        string count    = this.itemStack.count.ToString();
        string prob     = this.prob.ToString();

        return "Transformer Output: " + name + " (" + count + ")" + " with prob " + prob;
    }
}
