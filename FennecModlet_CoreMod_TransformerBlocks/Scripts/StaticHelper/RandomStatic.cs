using System;

static class RandomStatic
{	
	public static Random random = new Random();
	
	public static double Next()
	{
		return random.NextDouble();
	}
}