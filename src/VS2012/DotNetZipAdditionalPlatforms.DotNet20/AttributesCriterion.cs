namespace DotNetZipAdditionalPlatforms
{
    using DotNetZipAdditionalPlatforms.Zip;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;

    internal class AttributesCriterion : SelectionCriterion
    {
        private FileAttributes _Attributes;
        internal ComparisonOperator Operator = ComparisonOperator.GreaterThan;

        private bool _Evaluate(FileAttributes fileAttrs)
        {
            bool flag = this._EvaluateOne(fileAttrs, FileAttributes.Hidden);
            if (flag)
            {
                flag = this._EvaluateOne(fileAttrs, FileAttributes.System);
            }
            if (flag)
            {
                flag = this._EvaluateOne(fileAttrs, FileAttributes.ReadOnly);
            }
            if (flag)
            {
                flag = this._EvaluateOne(fileAttrs, FileAttributes.Archive);
            }
            if (flag)
            {
                flag = this._EvaluateOne(fileAttrs, FileAttributes.NotContentIndexed);
            }
            if (flag)
            {
                flag = this._EvaluateOne(fileAttrs, FileAttributes.ReparsePoint);
            }
            if (this.Operator != ComparisonOperator.EqualTo)
            {
                flag = !flag;
            }
            return flag;
        }

        private bool _EvaluateOne(FileAttributes fileAttrs, FileAttributes criterionAttrs)
        {
            if ((this._Attributes & criterionAttrs) == criterionAttrs)
            {
                return ((fileAttrs & criterionAttrs) == criterionAttrs);
            }
            return true;
        }

        internal override bool Evaluate(ZipEntry entry)
        {
            FileAttributes fileAttrs = entry.Attributes;
            return this._Evaluate(fileAttrs);
        }

        internal override bool Evaluate(string filename)
        {
            if (Directory.Exists(filename))
            {
                return (this.Operator != ComparisonOperator.EqualTo);
            }
            FileAttributes fileAttrs = File.GetAttributes(filename);
            return this._Evaluate(fileAttrs);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("attributes ").Append(EnumUtil.GetDescription(this.Operator)).Append(" ").Append(this.AttributeString);
            return builder.ToString();
        }

        internal string AttributeString
        {
            get
            {
                string str = "";
                if ((this._Attributes & FileAttributes.Hidden) != 0)
                {
                    str = str + "H";
                }
                if ((this._Attributes & FileAttributes.System) != 0)
                {
                    str = str + "S";
                }
                if ((this._Attributes & FileAttributes.ReadOnly) != 0)
                {
                    str = str + "R";
                }
                if ((this._Attributes & FileAttributes.Archive) != 0)
                {
                    str = str + "A";
                }
                if ((this._Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    str = str + "L";
                }
                if ((this._Attributes & FileAttributes.NotContentIndexed) != 0)
                {
                    str = str + "I";
                }
                return str;
            }
            set
            {
                this._Attributes = FileAttributes.Normal;
                foreach (char ch in value.ToUpper())
                {
                    switch (ch)
                    {
                        case 'H':
                            if ((this._Attributes & FileAttributes.Hidden) != 0)
                            {
                                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Repeated flag. ({0})", ch), "value");
                            }
                            break;

                        case 'I':
                            if ((this._Attributes & FileAttributes.NotContentIndexed) != 0)
                            {
                                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Repeated flag. ({0})", ch), "value");
                            }
                            goto Label_018F;

                        case 'L':
                            if ((this._Attributes & FileAttributes.ReparsePoint) != 0)
                            {
                                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Repeated flag. ({0})", ch), "value");
                            }
                            goto Label_01D3;

                        case 'A':
                        {
                            if ((this._Attributes & FileAttributes.Archive) != 0)
                            {
                                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Repeated flag. ({0})", ch), "value");
                            }
                            this._Attributes |= FileAttributes.Archive;
                            continue;
                        }
                        case 'R':
                            if ((this._Attributes & FileAttributes.ReadOnly) != 0)
                            {
                                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Repeated flag. ({0})", ch), "value");
                            }
                            goto Label_00CC;

                        case 'S':
                            if ((this._Attributes & FileAttributes.System) != 0)
                            {
                                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Repeated flag. ({0})", ch), "value");
                            }
                            goto Label_010B;

                        default:
                            throw new ArgumentException(value);
                    }
                    this._Attributes |= FileAttributes.Hidden;
                    continue;
                Label_00CC:
                    this._Attributes |= FileAttributes.ReadOnly;
                    continue;
                Label_010B:
                    this._Attributes |= FileAttributes.System;
                    continue;
                Label_018F:
                    this._Attributes |= FileAttributes.NotContentIndexed;
                    continue;
                Label_01D3:
                    this._Attributes |= FileAttributes.ReparsePoint;
                }
            }
        }
    }
}

