using Pgvector;

namespace BettsTax.Data.Models
{
    /// <summary>
    /// RAG-based bot configuration
    /// Phase 3: Bot Capabilities with RAG
    /// </summary>
    
    /// <summary>
    /// LLM Provider types
    /// </summary>
    public enum LLMProvider
    {
        OpenAI,
        Anthropic,
        Gemini
    }
    
    /// <summary>
    /// Bot configuration settings
    /// </summary>
    public class BotConfiguration
    {
        public int BotConfigurationId { get; set; }
        
        /// <summary>
        /// Configuration name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// LLM provider to use
        /// </summary>
        public LLMProvider Provider { get; set; }
        
        /// <summary>
        /// Model name (e.g., "gpt-4", "claude-3-opus", "gemini-pro")
        /// </summary>
        public string ModelName { get; set; } = string.Empty;
        
        /// <summary>
        /// API key (encrypted)
        /// </summary>
        public string ApiKeyEncrypted { get; set; } = string.Empty;
        
        /// <summary>
        /// API endpoint URL (optional, for custom endpoints)
        /// </summary>
        public string? ApiEndpoint { get; set; }
        
        /// <summary>
        /// Temperature for generation (0.0 - 1.0)
        /// </summary>
        public double Temperature { get; set; } = 0.7;
        
        /// <summary>
        /// Max tokens for response
        /// </summary>
        public int MaxTokens { get; set; } = 1000;
        
        /// <summary>
        /// Top K results from vector search
        /// </summary>
        public int TopK { get; set; } = 5;
        
        /// <summary>
        /// Similarity threshold for vector search (0.0 - 1.0)
        /// </summary>
        public double SimilarityThreshold { get; set; } = 0.7;
        
        /// <summary>
        /// System prompt for the bot
        /// </summary>
        public string SystemPrompt { get; set; } = "You are a helpful tax compliance assistant for Sierra Leone.";
        
        /// <summary>
        /// Whether this configuration is active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Whether this is the default configuration
        /// </summary>
        public bool IsDefault { get; set; } = false;
        
        /// <summary>
        /// Created by user ID
        /// </summary>
        public string CreatedById { get; set; } = string.Empty;
        
        /// <summary>
        /// Created date
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Last updated by user ID
        /// </summary>
        public string? UpdatedById { get; set; }
        
        /// <summary>
        /// Last updated date
        /// </summary>
        public DateTime? UpdatedDate { get; set; }
        
        // Navigation properties
        public ApplicationUser? CreatedBy { get; set; }
        public ApplicationUser? UpdatedBy { get; set; }
    }
    
    /// <summary>
    /// Knowledge base document for RAG
    /// </summary>
    public class KnowledgeDocument
    {
        public int KnowledgeDocumentId { get; set; }
        
        /// <summary>
        /// Document title
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Document content
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Document category (e.g., "Tax Law", "FAQ", "Procedure")
        /// </summary>
        public string Category { get; set; } = string.Empty;
        
        /// <summary>
        /// Document tags for filtering
        /// </summary>
        public List<string> Tags { get; set; } = new();
        
        /// <summary>
        /// Source file path (if uploaded)
        /// </summary>
        public string? SourceFilePath { get; set; }
        
        /// <summary>
        /// Source URL (if from web)
        /// </summary>
        public string? SourceUrl { get; set; }
        
        /// <summary>
        /// Document metadata (JSON)
        /// </summary>
        public string? Metadata { get; set; }
        
        /// <summary>
        /// Whether this document is active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Uploaded by user ID
        /// </summary>
        public string UploadedById { get; set; } = string.Empty;
        
        /// <summary>
        /// Upload date
        /// </summary>
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Last updated date
        /// </summary>
        public DateTime? UpdatedDate { get; set; }
        
        // Navigation properties
        public ApplicationUser? UploadedBy { get; set; }
        public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }
    
    /// <summary>
    /// Document chunk with vector embedding for RAG
    /// Uses pgvector for similarity search
    /// </summary>
    public class DocumentChunk
    {
        public int DocumentChunkId { get; set; }
        
        /// <summary>
        /// Parent document ID
        /// </summary>
        public int KnowledgeDocumentId { get; set; }
        
        /// <summary>
        /// Chunk text content
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Vector embedding (1536 dimensions for OpenAI ada-002)
        /// </summary>
        public Vector? Embedding { get; set; }
        
        /// <summary>
        /// Chunk index in document
        /// </summary>
        public int ChunkIndex { get; set; }
        
        /// <summary>
        /// Token count for this chunk
        /// </summary>
        public int TokenCount { get; set; }
        
        /// <summary>
        /// Chunk metadata (JSON)
        /// </summary>
        public string? Metadata { get; set; }
        
        /// <summary>
        /// Created date
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public KnowledgeDocument? KnowledgeDocument { get; set; }
    }
    
    /// <summary>
    /// Bot conversation history
    /// </summary>
    public class BotConversation
    {
        public int BotConversationId { get; set; }
        
        /// <summary>
        /// User ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Conversation title (auto-generated from first message)
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Started date
        /// </summary>
        public DateTime StartedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Last message date
        /// </summary>
        public DateTime LastMessageDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Whether conversation is archived
        /// </summary>
        public bool IsArchived { get; set; } = false;
        
        // Navigation properties
        public ApplicationUser? User { get; set; }
        public ICollection<BotMessage> Messages { get; set; } = new List<BotMessage>();
    }
    
    /// <summary>
    /// Individual bot message
    /// </summary>
    public class BotMessage
    {
        public int BotMessageId { get; set; }
        
        /// <summary>
        /// Conversation ID
        /// </summary>
        public int BotConversationId { get; set; }
        
        /// <summary>
        /// Message role (user, assistant, system)
        /// </summary>
        public string Role { get; set; } = string.Empty;
        
        /// <summary>
        /// Message content
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Retrieved context chunks (JSON array of chunk IDs)
        /// </summary>
        public string? RetrievedChunks { get; set; }
        
        /// <summary>
        /// Token count
        /// </summary>
        public int TokenCount { get; set; }
        
        /// <summary>
        /// Message timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// LLM provider used
        /// </summary>
        public LLMProvider? Provider { get; set; }
        
        /// <summary>
        /// Model name used
        /// </summary>
        public string? ModelName { get; set; }
        
        // Navigation properties
        public BotConversation? Conversation { get; set; }
    }
    
    /// <summary>
    /// Bot feedback for improving responses
    /// </summary>
    public class BotFeedback
    {
        public int BotFeedbackId { get; set; }
        
        /// <summary>
        /// Message ID being rated
        /// </summary>
        public int BotMessageId { get; set; }
        
        /// <summary>
        /// User ID providing feedback
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Rating (1-5)
        /// </summary>
        public int Rating { get; set; }
        
        /// <summary>
        /// Feedback comment
        /// </summary>
        public string? Comment { get; set; }
        
        /// <summary>
        /// Whether response was helpful
        /// </summary>
        public bool WasHelpful { get; set; }
        
        /// <summary>
        /// Feedback timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public BotMessage? Message { get; set; }
        public ApplicationUser? User { get; set; }
    }
    
    /// <summary>
    /// Embedding job status for document processing
    /// </summary>
    public class EmbeddingJob
    {
        public int EmbeddingJobId { get; set; }
        
        /// <summary>
        /// Knowledge document ID
        /// </summary>
        public int KnowledgeDocumentId { get; set; }
        
        /// <summary>
        /// Job status
        /// </summary>
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
        
        /// <summary>
        /// Total chunks to process
        /// </summary>
        public int TotalChunks { get; set; }
        
        /// <summary>
        /// Processed chunks count
        /// </summary>
        public int ProcessedChunks { get; set; }
        
        /// <summary>
        /// Error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Started date
        /// </summary>
        public DateTime StartedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Completed date
        /// </summary>
        public DateTime? CompletedDate { get; set; }
        
        // Navigation properties
        public KnowledgeDocument? KnowledgeDocument { get; set; }
    }
}
