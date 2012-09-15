namespace DotNetZipAdditionalPlatforms
{
    using DotNetZipAdditionalPlatforms.Zip;
    using System;
    using System.IO;
    using System.Text;

    internal class TypeCriterion : SelectionCriterion
    {
        private char ObjectType;
        internal ComparisonOperator Operator = ComparisonOperator.GreaterThan;

        internal override bool Evaluate(ZipEntry entry)
        {
            bool flag = (this.ObjectType == 'D') ? entry.IsDirectory : !entry.IsDirectory;
            if (this.Operator != ComparisonOperator.EqualTo)
            {
                flag = !flag;
            }
            return flag;
        }

        internal override bool Evaluate(string filename)
        {
            bool flag = (this.ObjectType == 'D') ? Directory.Exists(filename) : File.Exists(filename);
            if (this.Operator != ComparisonOperator.EqualTo)
            {
                flag = !flag;
            }
            return flag;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("type ").Append(EnumUtil.GetDescription(this.Operator)).Append(" ").Append(this.AttributeString);
            return builder.ToString();
        }

        internal string AttributeString
        {
            get
            {
                return this.ObjectType.ToString();
            }
            set
            {
                if ((value.Length != 1) || ((value[0] != 'D') && (value[0] != 'F')))
                {
                    throw new ArgumentException("Specify a single character: either D or F");
                }
                this.ObjectType = value[0];
            }
        }
    }
}

