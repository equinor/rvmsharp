namespace CadRevealComposer.Writers;

using System;
using System.Collections.Generic;
using System.Linq;

public static class FibbonaciEncoding
{
    private static readonly ulong[] FibCache =
    {
        0,
        1,
        1,
        2,
        3,
        5,
        8,
        13,
        21,
        34,
        55,
        89,
        144,
        233,
        377,
        610,
        987,
        1597,
        2584,
        4181,
        6765,
        10946,
        17711,
        28657,
        46368,
        75025,
        121393,
        196418,
        317811,
        514229,
        832040,
        1346269,
        2178309,
        3524578,
        5702887,
        9227465,
        14930352,
        24157817,
        39088169,
        63245986,
        102334155,
        165580141,
        267914296,
        433494437,
        701408733,
        1134903170,
        1836311903,
        2971215073,
        4807526976,
        7778742049,
        12586269025,
        20365011074,
        32951280099,
        53316291173,
        86267571272,
        139583862445,
        225851433717,
        365435296162,
        591286729879,
        956722026041,
        1548008755920,
        2504730781961,
        4052739537881,
        6557470319842,
        10610209857723,
        17167680177565,
        27777890035288,
        44945570212853,
        72723460248141,
        117669030460994
    };

    public static byte[] EncodeArray(ulong[] input)
    {
        var output = new List<byte>();

        var encoded = input.Select(v => FibEncode(v)).ToArray();

        byte current = 0;
        int pos = 0;
        foreach (ulong v in encoded)
        {
            int oneCount = 0;
            for (int v_pos = 0;; v_pos++)
            {
                if (v_pos >= 64)
                {
                    throw new Exception("Encoding failed");
                }

                ulong bit = (v >> v_pos) & 1;
                if (bit == 1)
                {
                    oneCount += 1;
                    current |= (byte)(1 << (7 - pos));
                }
                else
                {
                    oneCount = 0;
                }

                pos += 1;
                if (pos == 8)
                {
                    output.Add(current);
                    pos = 0;
                    current = 0;
                }

                if (oneCount == 2)
                {
                    break;
                }
            }
        }

        if (pos > 0)
        {
            output.Add(current);
        }

        return output.ToArray();
    }

    private static ulong FibEncode(ulong input)
    {
        ulong rest = input + 1;
        ulong result = 0u;
        int lastBit = 0;
        while (rest != 0)
        {
            for (int i = 0;; i++)
            {
                ulong value = Fib(i + 2);
                if (value > rest)
                {
                    int pos = i - 1;
                    rest = rest - Fib(pos + 2);
                    result = result | ((ulong)1 << pos);
                    if (i > lastBit)
                    {
                        lastBit = pos;
                    }

                    break;
                }
            }
        }

        result |= (ulong)1 << (lastBit + 1);
        return result;
    }

    private static ulong Fib(int i)
    {
        if (i >= 70)
        {
            throw new ArgumentOutOfRangeException();
        }

        return FibCache[i];
    }
}