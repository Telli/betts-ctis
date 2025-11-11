using System.Collections.Generic;

namespace BettsTax.Core.DTOs
{
    public class QuickActionDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // URL or action identifier
        public bool Enabled { get; set; } = true;
        public int Order { get; set; }
    }

    public class QuickActionsResponseDto
    {
        public List<QuickActionDto> Actions { get; set; } = new();
        public string UserRole { get; set; } = string.Empty;
        public Dictionary<string, int> Counts { get; set; } = new(); // e.g., "pendingApprovals": 5
    }
}
