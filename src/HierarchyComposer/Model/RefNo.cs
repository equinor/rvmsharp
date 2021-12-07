namespace HierarchyComposer.Model
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Text.RegularExpressions;

    /// <summary>
    /// In the PDMS format every element has an ID, which is unique to an object.
    /// It consists of two parts, where the firs is a DatabaseNumber
    /// and the second a SequenceNumber
    /// It will look like this in the data: "=123/456"
    /// </summary>
    [DebuggerDisplay("={DbNo}/{SequenceNo}")]
    public class RefNo
    {
        public int DbNo { get; }
        public int SequenceNo { get; }

        public override string ToString()
        {
            return $"={DbNo}/{SequenceNo}";
        }

        public RefNo(int dbNo, int sequenceNo)
        {
            DbNo = dbNo;
            SequenceNo = sequenceNo;
        }

        /// <summary>
        /// Matches strings with this pattern: =123/456, with one capturing group () for each number.
        /// </summary>
        private static readonly Regex RefNoRegex = new Regex(@"^=(\d+)\/(\d+)$", RegexOptions.Compiled);

        /// <summary>
        /// Parse a RefNo string into a RefNo
        /// </summary>
        /// <param name="refNo">Expects a "=123/456" formatted string</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">If the input format is not correct</exception>
        /// <exception cref="ArgumentNullException"></exception>
        [Pure]
        public static RefNo Parse(string refNo)
        {
            if (refNo == null)
                throw new ArgumentNullException(nameof(refNo));

            var match = RefNoRegex.Match(refNo);

            if (!match.Success)
                throw new ArgumentException($"Expected format '=123/321' '(=uint/uint)', was '{refNo}'", nameof(refNo));

            // Regex Group 0 is the entire match.
            var dbNo = int.Parse(match.Groups[1].Value);
            var sequenceNo = int.Parse(match.Groups[2].Value);

            if (dbNo < 0 || sequenceNo < 0)
                throw new ArgumentException($"Expected positive values, was '{refNo}'", nameof(refNo));

            return new RefNo(dbNo, sequenceNo);
        }
    }
}