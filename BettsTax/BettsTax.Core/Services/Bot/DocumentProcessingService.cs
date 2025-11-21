using BettsTax.Data;
using BettsTax.Data.Models;
using BettsTax.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using System.Text;

namespace BettsTax.Core.Services.Bot
{
    /// <summary>
    /// Service for processing documents and generating embeddings
    /// Phase 3: Bot Capabilities - Document Processing
    /// </summary>
    public interface IDocumentProcessingService
    {
        Task<Result<KnowledgeDocument>> UploadDocumentAsync(string title, string content, string category, List<string> tags, string userId);
        Task<Result<bool>> ProcessDocumentAsync(int documentId);
        Task<Result<List<KnowledgeDocument>>> GetDocumentsAsync(string? category = null);
        Task<Result<bool>> DeleteDocumentAsync(int documentId);
        Task<Result<EmbeddingJob>> GetEmbeddingJobStatusAsync(int jobId);
    }
    
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILLMProviderFactory _providerFactory;
        private readonly ILogger<DocumentProcessingService> _logger;
        private const int ChunkSize = 500; // tokens per chunk
        private const int ChunkOverlap = 50; // token overlap between chunks
        
        public DocumentProcessingService(
            ApplicationDbContext context,
            ILLMProviderFactory providerFactory,
            ILogger<DocumentProcessingService> logger)
        {
            _context = context;
            _providerFactory = providerFactory;
            _logger = logger;
        }
        
        public async Task<Result<KnowledgeDocument>> UploadDocumentAsync(
            string title, 
            string content, 
            string category, 
            List<string> tags, 
            string userId)
        {
            try
            {
                var document = new KnowledgeDocument
                {
                    Title = title,
                    Content = content,
                    Category = category,
                    Tags = tags,
                    IsActive = true,
                    UploadedById = userId,
                    UploadedDate = DateTime.UtcNow
                };
                
                _context.Set<KnowledgeDocument>().Add(document);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Uploaded knowledge document {DocumentId}: {Title}", 
                    document.KnowledgeDocumentId, title);
                
                // Start background processing
                _ = Task.Run(async () => await ProcessDocumentAsync(document.KnowledgeDocumentId));
                
                return Result.Success(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return Result.Failure<KnowledgeDocument>("Failed to upload document");
            }
        }
        
        public async Task<Result<bool>> ProcessDocumentAsync(int documentId)
        {
            EmbeddingJob? job = null;
            
            try
            {
                var document = await _context.Set<KnowledgeDocument>()
                    .FirstOrDefaultAsync(d => d.KnowledgeDocumentId == documentId);
                
                if (document == null)
                {
                    return Result.Failure<bool>("Document not found");
                }
                
                // Get active configuration
                var config = await _context.Set<BotConfiguration>()
                    .FirstOrDefaultAsync(c => c.IsActive && c.IsDefault);
                
                if (config == null)
                {
                    return Result.Failure<bool>("No active bot configuration found");
                }
                
                // Get LLM provider
                var provider = await _providerFactory.GetProviderAsync(config);
                
                // Split document into chunks
                var chunks = SplitIntoChunks(document.Content, provider);
                
                // Create embedding job
                job = new EmbeddingJob
                {
                    KnowledgeDocumentId = documentId,
                    Status = "Processing",
                    TotalChunks = chunks.Count,
                    ProcessedChunks = 0,
                    StartedDate = DateTime.UtcNow
                };
                _context.Set<EmbeddingJob>().Add(job);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Processing {Count} chunks for document {DocumentId}", 
                    chunks.Count, documentId);
                
                // Process each chunk
                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunkText = chunks[i];
                    
                    // Generate embedding
                    var embeddingResult = await provider.GenerateEmbeddingAsync(chunkText);
                    
                    if (!embeddingResult.IsSuccess)
                    {
                        _logger.LogError("Failed to generate embedding for chunk {Index}: {Error}", 
                            i, embeddingResult.ErrorMessage);
                        continue;
                    }
                    
                    // Save chunk with embedding
                    var chunk = new DocumentChunk
                    {
                        KnowledgeDocumentId = documentId,
                        Content = chunkText,
                        Embedding = new Vector(embeddingResult.Value),
                        ChunkIndex = i,
                        TokenCount = provider.CountTokens(chunkText),
                        CreatedDate = DateTime.UtcNow
                    };
                    
                    _context.Set<DocumentChunk>().Add(chunk);
                    
                    // Update job progress
                    job.ProcessedChunks = i + 1;
                    
                    // Save every 10 chunks
                    if ((i + 1) % 10 == 0)
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Processed {Processed}/{Total} chunks for document {DocumentId}", 
                            job.ProcessedChunks, job.TotalChunks, documentId);
                    }
                }
                
                // Final save
                job.Status = "Completed";
                job.CompletedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Completed processing document {DocumentId} with {Count} chunks", 
                    documentId, chunks.Count);
                
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {DocumentId}", documentId);
                
                if (job != null)
                {
                    job.Status = "Failed";
                    job.ErrorMessage = ex.Message;
                    job.CompletedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
                
                return Result.Failure<bool>("Failed to process document");
            }
        }
        
        public async Task<Result<List<KnowledgeDocument>>> GetDocumentsAsync(string? category = null)
        {
            try
            {
                var query = _context.Set<KnowledgeDocument>()
                    .Where(d => d.IsActive);
                
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(d => d.Category == category);
                }
                
                var documents = await query
                    .OrderByDescending(d => d.UploadedDate)
                    .ToListAsync();
                
                return Result.Success(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents");
                return Result.Failure<List<KnowledgeDocument>>("Failed to retrieve documents");
            }
        }
        
        public async Task<Result<bool>> DeleteDocumentAsync(int documentId)
        {
            try
            {
                var document = await _context.Set<KnowledgeDocument>()
                    .Include(d => d.Chunks)
                    .FirstOrDefaultAsync(d => d.KnowledgeDocumentId == documentId);
                
                if (document == null)
                {
                    return Result.Failure<bool>("Document not found");
                }
                
                // Delete chunks
                _context.Set<DocumentChunk>().RemoveRange(document.Chunks);
                
                // Delete document
                _context.Set<KnowledgeDocument>().Remove(document);
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted document {DocumentId} with {Count} chunks", 
                    documentId, document.Chunks.Count);
                
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                return Result.Failure<bool>("Failed to delete document");
            }
        }
        
        public async Task<Result<EmbeddingJob>> GetEmbeddingJobStatusAsync(int jobId)
        {
            try
            {
                var job = await _context.Set<EmbeddingJob>()
                    .FirstOrDefaultAsync(j => j.EmbeddingJobId == jobId);
                
                if (job == null)
                {
                    return Result.Failure<EmbeddingJob>("Job not found");
                }
                
                return Result.Success(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job status");
                return Result.Failure<EmbeddingJob>("Failed to retrieve job status");
            }
        }
        
        #region Private Helper Methods
        
        private List<string> SplitIntoChunks(string text, ILLMProvider provider)
        {
            var chunks = new List<string>();
            var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            var currentChunk = new StringBuilder();
            int currentTokens = 0;
            
            foreach (var word in words)
            {
                var wordTokens = provider.CountTokens(word);
                
                if (currentTokens + wordTokens > ChunkSize && currentChunk.Length > 0)
                {
                    // Save current chunk
                    chunks.Add(currentChunk.ToString().Trim());
                    
                    // Start new chunk with overlap
                    var overlapWords = GetLastNWords(currentChunk.ToString(), ChunkOverlap, provider);
                    currentChunk = new StringBuilder(overlapWords);
                    currentTokens = provider.CountTokens(overlapWords);
                }
                
                currentChunk.Append(word).Append(' ');
                currentTokens += wordTokens;
            }
            
            // Add final chunk
            if (currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());
            }
            
            return chunks;
        }
        
        private string GetLastNWords(string text, int tokenCount, ILLMProvider provider)
        {
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();
            int tokens = 0;
            
            for (int i = words.Length - 1; i >= 0; i--)
            {
                var wordTokens = provider.CountTokens(words[i]);
                if (tokens + wordTokens > tokenCount)
                {
                    break;
                }
                
                result.Insert(0, words[i] + " ");
                tokens += wordTokens;
            }
            
            return result.ToString().Trim();
        }
        
        #endregion
    }
}
