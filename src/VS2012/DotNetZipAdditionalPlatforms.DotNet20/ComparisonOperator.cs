namespace DotNetZipAdditionalPlatforms
{
    using System;
    using System.ComponentModel;

    internal enum ComparisonOperator
    {
        [Description("=")]
        EqualTo = 4,
        [Description(">")]
        GreaterThan = 0,
        [Description(">=")]
        GreaterThanOrEqualTo = 1,
        [Description("<")]
        LesserThan = 2,
        [Description("<=")]
        LesserThanOrEqualTo = 3,
        [Description("!=")]
        NotEqualTo = 5
    }
}

