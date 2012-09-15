namespace DotNetZipAdditionalPlatforms
{
    using DotNetZipAdditionalPlatforms.Zip;
    using System;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    internal class NameCriterion : SelectionCriterion
    {
        private string _MatchingFileSpec;
        private Regex _re;
        private string _regexString;
        internal ComparisonOperator Operator;

        private bool _Evaluate(string fullpath)
        {
            string input = (this._MatchingFileSpec.IndexOf('\\') == -1) ? Path.GetFileName(fullpath) : fullpath;
            bool flag = this._re.IsMatch(input);
            if (this.Operator != ComparisonOperator.EqualTo)
            {
                flag = !flag;
            }
            return flag;
        }

        internal override bool Evaluate(ZipEntry entry)
        {
            string fullpath = entry.FileName.Replace("/", @"\");
            return this._Evaluate(fullpath);
        }

        internal override bool Evaluate(string filename)
        {
            return this._Evaluate(filename);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("name ").Append(EnumUtil.GetDescription(this.Operator)).Append(" '").Append(this._MatchingFileSpec).Append("'");
            return builder.ToString();
        }

        internal virtual string MatchingFileSpec
        {
            set
            {
                if (Directory.Exists(value))
                {
                    this._MatchingFileSpec = @".\" + value + @"\*.*";
                }
                else
                {
                    this._MatchingFileSpec = value;
                }
                this._regexString = "^" + Regex.Escape(this._MatchingFileSpec).Replace(@"\\\*\.\*", @"\\([^\.]+|.*\.[^\\\.]*)").Replace(@"\.\*", @"\.[^\\\.]*").Replace(@"\*", ".*").Replace(@"\?", @"[^\\\.]") + "$";
                this._re = new Regex(this._regexString, RegexOptions.IgnoreCase);
            }
        }
    }
}

