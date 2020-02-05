using System;

public static class NumHelpers
{
    public static double CheckProb(double prob)
    {
        if (prob < 0.0)
        {
            return 0.0;
        }

        if (prob > 1.0)
        {
            return 1.0;
        }

        return prob;
    }
}