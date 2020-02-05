using System;

static class RandomStatic
{	
	public static Random random = new Random();
	
	public static double Next()
	{
		return random.NextDouble();
	}

    public static int Range(int lower, int upper)
    {
        return random.Next(lower, upper);
    }
}