namespace DotNetZipAdditionalPlatforms
{
    using DotNetZipAdditionalPlatforms.Zip;
    using System;
    using System.Text;

    internal class CompoundCriterion : SelectionCriterion
    {
        private SelectionCriterion _Right;
        internal LogicalConjunction Conjunction;
        internal SelectionCriterion Left;

        internal override bool Evaluate(ZipEntry entry)
        {
            bool flag = this.Left.Evaluate(entry);
            switch (this.Conjunction)
            {
                case LogicalConjunction.AND:
                    if (flag)
                    {
                        flag = this.Right.Evaluate(entry);
                    }
                    return flag;

                case LogicalConjunction.OR:
                    if (!flag)
                    {
                        flag = this.Right.Evaluate(entry);
                    }
                    return flag;

                case LogicalConjunction.XOR:
                    return (flag ^ this.Right.Evaluate(entry));
            }
            return flag;
        }

        internal override bool Evaluate(string filename)
        {
            bool flag = this.Left.Evaluate(filename);
            switch (this.Conjunction)
            {
                case LogicalConjunction.AND:
                    if (flag)
                    {
                        flag = this.Right.Evaluate(filename);
                    }
                    return flag;

                case LogicalConjunction.OR:
                    if (!flag)
                    {
                        flag = this.Right.Evaluate(filename);
                    }
                    return flag;

                case LogicalConjunction.XOR:
                    return (flag ^ this.Right.Evaluate(filename));
            }
            throw new ArgumentException("Conjunction");
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("(").Append((this.Left != null) ? this.Left.ToString() : "null").Append(" ").Append(this.Conjunction.ToString()).Append(" ").Append((this.Right != null) ? this.Right.ToString() : "null").Append(")");
            return builder.ToString();
        }

        internal SelectionCriterion Right
        {
            get
            {
                return this._Right;
            }
            set
            {
                this._Right = value;
                if (value == null)
                {
                    this.Conjunction = LogicalConjunction.NONE;
                }
                else if (this.Conjunction == LogicalConjunction.NONE)
                {
                    this.Conjunction = LogicalConjunction.AND;
                }
            }
        }
    }
}

