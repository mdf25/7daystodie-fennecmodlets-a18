using System.Security.Cryptography;
using System.Text;

public static class MD5Hash
{
    private static MD5 hash = MD5.Create();


    /**
     * Computes the MD5 hash for an input string.
     */

    public static string Calculate(string _s)
    {
        byte[] inputBytes = Encoding.ASCII.GetBytes(_s);
        byte[] hash = MD5Hash.hash.ComputeHash(inputBytes);
        
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("X2"));
        }
        return sb.ToString();
    }
}
