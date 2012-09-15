namespace DotNetZipAdditionalPlatforms
{
    using DotNetZipAdditionalPlatforms.Zip;
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    internal abstract class SelectionCriterion
    {
        protected SelectionCriterion()
        {
        }

        [Conditional("SelectorTrace")]
        protected static void CriterionTrace(string format, params object[] args)
        {
        }

        internal abstract bool Evaluate(ZipEntry entry);
        internal abstract bool Evaluate(string filename);

        internal virtual bool Verbose { get; set; }
    }
}

