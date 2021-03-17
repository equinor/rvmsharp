namespace RvmSharp.Tessellation
{
    using System;

    public class Asserts
    {
        internal static void AssertEquals<T>(string name1, T value1, string name2, T value2) where T : IEquatable<T>
        {
            if ((value1?.Equals(value2) == true))
                return;

            throw new Exception($"Expected {name1} {value1} to equal {name2} {value2}.");
        }

        internal static void AssertNotEquals<T>(string name1, T value1, string name2, T value2) where T : IEquatable<T>
        {
            if ((value1?.Equals(value2) != true))
                return;

            throw new Exception($"Expected {name1} {value1} to equal {name2} {value2}.");
        }
    }
}