namespace DotNetZipAdditionalPlatforms
{
    using DotNetZipAdditionalPlatforms.Zip;
    using System;
    using System.IO;
    using System.Text;

    internal class SizeCriterion : SelectionCriterion
    {
        internal ComparisonOperator Operator;
        internal long Size;

        private bool _Evaluate(long Length)
        {
            switch (this.Operator)
            {
                case ComparisonOperator.GreaterThan:
                    return (Length > this.Size);

                case ComparisonOperator.GreaterThanOrEqualTo:
                    return (Length >= this.Size);

                case ComparisonOperator.LesserThan:
                    return (Length < this.Size);

                case ComparisonOperator.LesserThanOrEqualTo:
                    return (Length <= this.Size);

                case ComparisonOperator.EqualTo:
                    return (Length == this.Size);

                case ComparisonOperator.NotEqualTo:
                    return (Length != this.Size);
            }
            throw new ArgumentException("Operator");
        }

        internal override bool Evaluate(ZipEntry entry)
        {
            return this._Evaluate(entry.UncompressedSize);
        }

        internal override bool Evaluate(string filename)
        {
            FileInfo info = new FileInfo(filename);
            return this._Evaluate(info.Length);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("size ").Append(EnumUtil.GetDescription(this.Operator)).Append(" ").Append(this.Size.ToString());
            return builder.ToString();
        }
    }
}

