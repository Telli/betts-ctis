using System;

namespace BettsTax.Core.Options
{
    public class DocumentRetentionOptions
    {
        // How long to keep versions by age
        public int RetentionDays { get; set; } = 365;
        // Minimum number of most-recent versions to keep per document
        public int KeepMinVersions { get; set; } = 3;
        // Grace period after soft delete before physical deletion
        public int PhysicalDeleteGraceDays { get; set; } = 30;
        // How many records to process per pass
        public int BatchSize { get; set; } = 200;
        // How often the job runs (minutes)
        public int IntervalMinutes { get; set; } = 60;
    }
}
