namespace DotNetZipAdditionalPlatforms
{
    using DotNetZipAdditionalPlatforms.Zip;
    using System;
    using System.IO;
    using System.Text;

    internal class TimeCriterion : SelectionCriterion
    {
        internal ComparisonOperator Operator;
        internal DateTime Time;
        internal WhichTime Which;

        private bool _Evaluate(DateTime x)
        {
            switch (this.Operator)
            {
                case ComparisonOperator.GreaterThan:
                    return (x > this.Time);

                case ComparisonOperator.GreaterThanOrEqualTo:
                    return (x >= this.Time);

                case ComparisonOperator.LesserThan:
                    return (x < this.Time);

                case ComparisonOperator.LesserThanOrEqualTo:
                    return (x <= this.Time);

                case ComparisonOperator.EqualTo:
                    return (x == this.Time);

                case ComparisonOperator.NotEqualTo:
                    return (x != this.Time);
            }
            throw new ArgumentException("Operator");
        }

        internal override bool Evaluate(ZipEntry entry)
        {
            DateTime accessedTime;
            switch (this.Which)
            {
                case WhichTime.atime:
                    accessedTime = entry.AccessedTime;
                    break;

                case WhichTime.mtime:
                    accessedTime = entry.ModifiedTime;
                    break;

                case WhichTime.ctime:
                    accessedTime = entry.CreationTime;
                    break;

                default:
                    throw new ArgumentException("??time");
            }
            return this._Evaluate(accessedTime);
        }

        internal override bool Evaluate(string filename)
        {
            DateTime time;
            switch (this.Which)
            {
                case WhichTime.atime:
                    time = File.GetLastAccessTime(filename).ToUniversalTime();
                    break;

                case WhichTime.mtime:
                    time = File.GetLastWriteTime(filename).ToUniversalTime();
                    break;

                case WhichTime.ctime:
                    time = File.GetCreationTime(filename).ToUniversalTime();
                    break;

                default:
                    throw new ArgumentException("Operator");
            }
            return this._Evaluate(time);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(this.Which.ToString()).Append(" ").Append(EnumUtil.GetDescription(this.Operator)).Append(" ").Append(this.Time.ToString("yyyy-MM-dd-HH:mm:ss"));
            return builder.ToString();
        }
    }
}

