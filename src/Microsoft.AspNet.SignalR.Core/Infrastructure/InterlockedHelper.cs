using System.Threading;

public static class InterlockedHelper
{
    public static bool CompareExchangeOr(ref int location, int value, int comparandA, int comparandB)
    {
        return Interlocked.CompareExchange(ref location, value, comparandA) == comparandA ||
               Interlocked.CompareExchange(ref location, value, comparandB) == comparandB;
    }
}