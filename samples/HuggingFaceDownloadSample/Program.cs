// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

/// <summary>
/// Sample application demonstrating how to download HuggingFace models using Foundry Local Core.
/// 
/// IMPORTANT: This sample uses ONNX Runtime GenAI compatible models from the 'onnxruntime' organization.
/// These models include genai_config.json which is REQUIRED for inference with Foundry Local Core.
/// 
/// Compatible models: https://huggingface.co/onnxruntime
/// 
/// This sample shows:
/// 1. Downloading a model from HuggingFace Hub (onnxruntime/Phi-3-mini-4k-instruct-onnx)
/// 2. Automatic metadata generation (model family, task, license, context length, etc.)
/// 3. Reading and displaying the generated metadata
/// 4. Handling subdirectory models (onnxruntime/gpt-oss-20b-onnx)
/// 5. Loading and running inference (both streaming and non-streaming)
/// </summary>

using Microsoft.Neutron.Downloader;
using Microsoft.AI.Foundry.Local;
using Microsoft.AI.Foundry.Local.Contracts;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  HuggingFace Model Download Sample - Foundry Local Core       ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger("HFDownloadSample");

// Output directory for downloaded models
// Structure: With UseOrganizationStructure=true (default), models are organized as:
//            outputBaseDir/HuggingFace/<organization>/<model>/<path>
var outputBaseDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".foundry-local"
);

// ══════════════════════════════════════════════════════════════════
// Example 1: Download a simple model (no subdirectory)
// ══════════════════════════════════════════════════════════════════
Console.WriteLine("Example 1: Downloading a simple HuggingFace model");
Console.WriteLine("──────────────────────────────────────────────────");
Console.WriteLine("Note: Using microsoft/Phi-3-mini-4k-instruct-onnx");
Console.WriteLine("      (ONNX Runtime GenAI compatible - CPU optimized variant)");
Console.WriteLine();
await DownloadSimpleModel(outputBaseDir, logger);

Console.WriteLine();
Console.WriteLine("Press Enter to continue to Example 2...");
Console.ReadLine();
Console.WriteLine();

// ══════════════════════════════════════════════════════════════════
// Example 2: Download a model with subdirectory (more complex)
// ══════════════════════════════════════════════════════════════════
Console.WriteLine("Example 2: Downloading a model with subdirectory path");
Console.WriteLine("──────────────────────────────────────────────────────");
await DownloadModelWithSubdirectory(outputBaseDir, logger);

Console.WriteLine();
Console.WriteLine("Press Enter to continue to Example 3...");
Console.ReadLine();
Console.WriteLine();

// ══════════════════════════════════════════════════════════════════
// Example 3: Download Azure Catalog Model (qwen2.5-7b)
// ══════════════════════════════════════════════════════════════════
Console.WriteLine("Example 3: Downloading an Azure Catalog model");
Console.WriteLine("──────────────────────────────────────────────");
Console.WriteLine("Note: Using Qwen 2.5-7B from Azure Foundry Local Catalog");
Console.WriteLine("      (Demonstrates Azure catalog integration)");
Console.WriteLine();
await DownloadAzureCatalogModel(outputBaseDir, logger);

Console.WriteLine();
Console.WriteLine("Press Enter to continue to Example 4...");
Console.ReadLine();
Console.WriteLine();

// ══════════════════════════════════════════════════════════════════
// Example 4: Attempt to download incompatible model (error handling)
// ══════════════════════════════════════════════════════════════════
Console.WriteLine("Example 4: Attempting to download incompatible model");
Console.WriteLine("──────────────────────────────────────────────────────");
Console.WriteLine("Note: This demonstrates graceful error handling for models");
Console.WriteLine("      without genai_config.json");
Console.WriteLine();
await AttemptIncompatibleModelDownload(outputBaseDir, logger);

Console.WriteLine();
Console.WriteLine("Press Enter to continue to Example 5...");
Console.ReadLine();
Console.WriteLine();

// ══════════════════════════════════════════════════════════════════
// Example 5: Load and run inference with the downloaded model
// ══════════════════════════════════════════════════════════════════
Console.WriteLine("Example 5: Loading and running inference with the model");
Console.WriteLine("─────────────────────────────────────────────────────────");
await RunInferenceExample(outputBaseDir, logger);

Console.WriteLine();
Console.WriteLine("Press Enter to continue to Example 6...");
Console.ReadLine();
Console.WriteLine();

// ══════════════════════════════════════════════════════════════════
// Example 6: Test download_model command with HuggingFace path
// ══════════════════════════════════════════════════════════════════
Console.WriteLine("Example 6: Testing download_model command with HuggingFace path");
Console.WriteLine("──────────────────────────────────────────────────────────────");
await TestDownloadModelCommand(outputBaseDir, logger);

Console.WriteLine();
Console.WriteLine("Press Enter to continue to Example 7...");
Console.ReadLine();
Console.WriteLine();

// ══════════════════════════════════════════════════════════════════
// Example 7: List cached models
// ══════════════════════════════════════════════════════════════════
Console.WriteLine("Example 7: Listing all cached models");
Console.WriteLine("─────────────────────────────────────");
await ListCachedModelsExample(outputBaseDir, logger);

Console.WriteLine();
Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  All examples completed successfully!                          ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");

// Cleanup
if (FoundryLocalCore.IsInitialized)
{
    FoundryLocalCore.Shutdown();
}

// ══════════════════════════════════════════════════════════════════
// EXAMPLE 1: Simple Model Download
// ══════════════════════════════════════════════════════════════════
static async Task DownloadSimpleModel(string baseDir, ILogger logger)
{
    // Use an ONNX Runtime GenAI compatible model with subdirectory
    // These models include genai_config.json which is required for Foundry Local Core
    var modelId = "microsoft/Phi-3-mini-4k-instruct-onnx";
    var subdirectoryPath = "cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4";
    
    // The downloader will automatically create: baseDir/HuggingFace/microsoft/Phi-3-mini-4k-instruct-onnx
    var outputDir = HuggingFaceExtensions.BuildModelPath(baseDir, modelId);
    
    Console.WriteLine($"Model ID: {modelId}");
    Console.WriteLine($"Subdirectory: {subdirectoryPath}");
    Console.WriteLine($"Output Directory: {outputDir}");
    Console.WriteLine();
    
    // Check if model already downloaded using catalog
    if (!FoundryLocalCore.IsInitialized)
    {
        var initConfig = new Dictionary<string, string>
        {
            ["ModelCacheDir"] = baseDir
        };
        await FoundryLocalCore.InitializeAsync(initConfig);
    }
    
    var catalogCheck = FoundryLocalCore.Instance.GetCatalog();
    var cachedModelsCheck = await catalogCheck.GetCachedModelsAsync(CancellationToken.None);
    var existingModel = cachedModelsCheck.FirstOrDefault(m => m.StartsWith(modelId + ":"));
    
    if (existingModel != null)
    {
        Console.WriteLine("✓ Model already downloaded, skipping download...");
        Console.WriteLine();
        
        var actualPath = await catalogCheck.GetModelPathAsync(existingModel, CancellationToken.None);
        if (actualPath != null)
        {
            await DisplayModelMetadata(actualPath);
        }
        return;
    }
    
    // Create download info
    var downloadInfo = new DownloadModelInfo(
        ModelInfo: new ModelInfo(
            Uri: modelId,
            Revision: "main",
            Path: subdirectoryPath  // Download only this optimized variant
        ),
        OutputDirectory: baseDir,  // Pass base directory, downloader will organize it
        Token: null,               // Optional: Add HuggingFace token for private models
        BufferSize: null           // Use default buffer size
    );
    
    // Progress reporter
    var progress = new Progress<(string? fileName, double? percent)>(p =>
    {
        if (p.fileName != null && p.percent.HasValue)
        {
            Console.Write($"\r  ⬇ {p.fileName}: {p.percent:F1}%    ");
        }
    });
    
    try
    {
        Console.WriteLine("Starting download...");
        
        // Download with automatic metadata generation
        await downloadInfo.HuggingFaceDownloadWithMetadataAsync(
            blobPathFilterPredicate: null,  // Optional: filter which files to download
            progress: progress,
            logger: logger,
            cancellationToken: CancellationToken.None
        );
        
        Console.WriteLine();
        Console.WriteLine("✓ Download complete!");
        Console.WriteLine();
        
        // Get the catalog (already initialized from the check earlier)
        var catalog = FoundryLocalCore.Instance.GetCatalog();
        
        // Invalidate cache to force rescan after download
        catalog.InvalidateCachedModels();
        
        // Force a fresh scan by getting cached models
        var cachedModels = await catalog.GetCachedModelsAsync(CancellationToken.None);
        
        logger.LogInformation($"Found {cachedModels.Count} cached models total");
        foreach (var m in cachedModels)
        {
            logger.LogInformation($"  Cached model: {m}");
        }
        
        // Find models that start with our modelId (they'll have :SHA suffix)
        var downloadedModel = cachedModels.FirstOrDefault(m => m.StartsWith(modelId + ":"));
        
        if (downloadedModel != null)
        {
            var actualModelPath = await catalog.GetModelPathAsync(downloadedModel, CancellationToken.None);
            if (actualModelPath != null)
            {
                Console.WriteLine($"Model ID: {downloadedModel}");
                Console.WriteLine($"Model cached at: {actualModelPath}");
                Console.WriteLine();
                await DisplayModelMetadata(actualModelPath);
            }
        }
        else
        {
            Console.WriteLine("⚠ Catalog doesn't see the model yet, finding it manually...");
            Console.WriteLine($"Looking for models starting with: {modelId}:");
            
            // Try to find it manually in the filesystem
            // Parse org/model from modelId (e.g., "microsoft/Phi-3-mini-4k-instruct-onnx")
            var parts = modelId.Split('/');
            if (parts.Length == 2)
            {
                var modelBaseDir = Path.Combine(baseDir, "HuggingFace", parts[0], parts[1]);
                if (Directory.Exists(modelBaseDir))
                {
                    var versionedPath = Directory.GetDirectories(modelBaseDir).FirstOrDefault();
                    
                    if (versionedPath != null)
                    {
                        Console.WriteLine($"Found versioned folder: {versionedPath}");
                        var metadataPath = Directory.GetFiles(versionedPath, "inference_model.json", SearchOption.AllDirectories)
                            .FirstOrDefault();
                        
                        if (metadataPath != null)
                        {
                            var metadataDir = Path.GetDirectoryName(metadataPath);
                            Console.WriteLine($"Found metadata at: {metadataDir}");
                            Console.WriteLine();
                            await DisplayModelMetadata(metadataDir!);
                        }
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine($"✗ Error: {ex.Message}");
        logger.LogError(ex, "Download failed");
    }
}

// ══════════════════════════════════════════════════════════════════
// EXAMPLE 2: Download Model with Subdirectory
// ══════════════════════════════════════════════════════════════════
static async Task DownloadModelWithSubdirectory(string baseDir, ILogger logger)
{
    // Some models have their files in subdirectories
    // Example: onnxruntime/gpt-oss-20b-onnx has webgpu optimized version in a subdirectory
    var modelId = "onnxruntime/gpt-oss-20b-onnx";
    var subdirectoryPath = "webgpu/webgpu-int4-rtn-block-32";
    
    // The downloader will automatically create: baseDir/HuggingFace/onnxruntime/gpt-oss-20b-onnx
    var outputDir = HuggingFaceExtensions.BuildModelPath(baseDir, modelId);
    
    Console.WriteLine($"Model ID: {modelId}");
    Console.WriteLine($"Subdirectory: {subdirectoryPath}");
    Console.WriteLine($"Output Directory: {outputDir}");
    Console.WriteLine();
    
    // Check if model already downloaded using catalog
    if (!FoundryLocalCore.IsInitialized)
    {
        var initConfig = new Dictionary<string, string>
        {
            ["ModelCacheDir"] = baseDir
        };
        await FoundryLocalCore.InitializeAsync(initConfig);
    }
    
    var catalogCheck = FoundryLocalCore.Instance.GetCatalog();
    var cachedModelsCheck = await catalogCheck.GetCachedModelsAsync(CancellationToken.None);
    var existingModel = cachedModelsCheck.FirstOrDefault(m => m.StartsWith(modelId + ":"));
    
    if (existingModel != null)
    {
        Console.WriteLine("✓ Model already downloaded, skipping download...");
        Console.WriteLine();
        
        var actualPath = await catalogCheck.GetModelPathAsync(existingModel, CancellationToken.None);
        if (actualPath != null)
        {
            await DisplayModelMetadata(actualPath);
        }
        return;
    }
    
    // Create download info with subdirectory path
    var downloadInfo = new DownloadModelInfo(
        ModelInfo: new ModelInfo(
            Uri: modelId,
            Revision: "main",
            Path: subdirectoryPath  // Specify subdirectory to download only that variant
        ),
        OutputDirectory: baseDir,
        Token: null,
        BufferSize: null
    );
    
    var progress = new Progress<(string? fileName, double? percent)>(p =>
    {
        if (p.fileName != null && p.percent.HasValue)
        {
            Console.Write($"\r  ⬇ {p.fileName}: {p.percent:F1}%    ");
        }
    });
    
    try
    {
        Console.WriteLine("Starting download...");
        
        await downloadInfo.HuggingFaceDownloadWithMetadataAsync(
            progress: progress,
            logger: logger,
            cancellationToken: CancellationToken.None
        );
        
        Console.WriteLine();
        Console.WriteLine("✓ Download complete!");
        Console.WriteLine();
        
        // Get the actual model path from catalog
        var catalog = FoundryLocalCore.Instance.GetCatalog();
        
        // Invalidate cache to force rescan after download
        catalog.InvalidateCachedModels();
        
        var cachedModels = await catalog.GetCachedModelsAsync(CancellationToken.None);
        
        logger.LogInformation($"Found {cachedModels.Count} cached models total");
        foreach (var m in cachedModels)
        {
            logger.LogInformation($"  Cached model: {m}");
        }
        
        var downloadedModel = cachedModels.FirstOrDefault(m => m.StartsWith(modelId + ":"));
        
        if (downloadedModel != null)
        {
            var actualModelPath = await catalog.GetModelPathAsync(downloadedModel, CancellationToken.None);
            if (actualModelPath != null)
            {
                Console.WriteLine($"Model ID: {downloadedModel}");
                Console.WriteLine($"Model cached at: {actualModelPath}");
                Console.WriteLine();
                await DisplayModelMetadata(actualModelPath);
            }
        }
        else
        {
            Console.WriteLine("⚠ Catalog doesn't see the model yet, finding it manually...");
            Console.WriteLine($"Looking for models starting with: {modelId}:");
            
            // Try to find it manually in the filesystem
            // Parse org/model from modelId (e.g., "onnxruntime/gpt-oss-20b-onnx")
            var parts = modelId.Split('/');
            if (parts.Length == 2)
            {
                var modelBaseDir = Path.Combine(baseDir, "HuggingFace", parts[0], parts[1]);
                if (Directory.Exists(modelBaseDir))
                {
                    var versionedPath = Directory.GetDirectories(modelBaseDir).FirstOrDefault();
                    
                    if (versionedPath != null)
                    {
                        Console.WriteLine($"Found versioned folder: {versionedPath}");
                        var metadataPath = Directory.GetFiles(versionedPath, "inference_model.json", SearchOption.AllDirectories)
                            .FirstOrDefault();
                        
                        if (metadataPath != null)
                        {
                            var metadataDir = Path.GetDirectoryName(metadataPath);
                            Console.WriteLine($"Found metadata at: {metadataDir}");
                            Console.WriteLine();
                            await DisplayModelMetadata(metadataDir!);
                        }
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine($"✗ Error: {ex.Message}");
        logger.LogError(ex, "Download failed");
    }
}

// ══════════════════════════════════════════════════════════════════
// EXAMPLE 3: Azure Catalog Model Download
// ══════════════════════════════════════════════════════════════════
static async Task DownloadAzureCatalogModel(string baseDir, ILogger logger)
{
    Console.WriteLine("Checking available models in Azure Foundry Local catalog...");
    Console.WriteLine();
    
    try
    {
        // Initialize Foundry Local Core to access the catalog
        if (!FoundryLocalCore.IsInitialized)
        {
            Console.WriteLine("Initializing Foundry Local Core...");
            var config = new Dictionary<string, string>
            {
                ["ModelCacheDir"] = baseDir
            };
            await FoundryLocalCore.InitializeAsync(config);
            Console.WriteLine("✓ Initialized");
            Console.WriteLine();
        }
        
        // Get the catalog
        var catalog = FoundryLocalCore.Instance.GetCatalog();
        
        // List all available models from Azure catalog
        Console.WriteLine("Fetching available models from Azure catalog...");
        var allModels = await catalog.GetModelsAsync(CancellationToken.None);
        
        if (allModels.Count == 0)
        {
            Console.WriteLine("⚠ No models found in Azure catalog.");
            Console.WriteLine("This example requires network connectivity to Azure.");
            return;
        }
        
        Console.WriteLine($"Found {allModels.Count} models in Azure catalog");
        Console.WriteLine();
        
        // Download specific model: qwen2.5-1.5b-instruct-generic-cpu:4
        var targetModelId = "qwen2.5-1.5b-instruct-generic-cpu:4";
        var modelToDownload = allModels.FirstOrDefault(m => m.Id == targetModelId);
        
        if (modelToDownload == null)
        {
            Console.WriteLine($"⚠ Model '{targetModelId}' not found in Azure catalog.");
            Console.WriteLine();
            
            // Show some available models
            Console.WriteLine("Sample of available Azure catalog models:");
            foreach (var model in allModels.Take(5))
            {
                Console.WriteLine($"  • {model.Id} ({model.FileSizeMb ?? 0} MB) - Cached: {model.Cached}");
            }
            return;
        }
        
        // Check if already cached
        if (modelToDownload.Cached)
        {
            Console.WriteLine($"✓ Model '{targetModelId}' is already cached - skipping download!");
            Console.WriteLine();
            
            var modelPath = await catalog.GetModelPathAsync(targetModelId, CancellationToken.None);
            if (modelPath != null)
            {
                Console.WriteLine($"Model path: {modelPath}");
                Console.WriteLine();
                await DisplayModelMetadata(modelPath);
            }
            return;
        }
        
        var modelId = modelToDownload.Id;
        Console.WriteLine($"Selected model: {modelId}");
        Console.WriteLine($"Size: {modelToDownload.FileSizeMb} MB");
        Console.WriteLine($"Type: {modelToDownload.ModelType}");
        Console.WriteLine();
        
        // Download the model
        Console.WriteLine("Starting download from Azure catalog...");
        Console.WriteLine("Note: This may take several minutes depending on model size");
        Console.WriteLine();
        
        var progress = new Action<double>(percent =>
        {
            Console.Write($"\r  Progress: {percent:F1}%    ");
        });
        
        var result = await catalog.DownloadModelAsync(modelId, progress, CancellationToken.None);
        
        Console.WriteLine();
        
        if (result == DownloadModelResult.Success)
        {
            Console.WriteLine("✓ Download complete!");
            Console.WriteLine();
            
            var modelPath = await catalog.GetModelPathAsync(modelId, CancellationToken.None);
            if (modelPath != null)
            {
                Console.WriteLine($"Downloaded to: {modelPath}");
                Console.WriteLine();
                await DisplayModelMetadata(modelPath);
            }
        }
        else
        {
            Console.WriteLine($"⚠ Download result: {result}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine($"✗ Error: {ex.Message}");
        logger.LogError(ex, "Azure catalog download failed");
        Console.WriteLine();
        Console.WriteLine("Note: This example requires:");
        Console.WriteLine("  • Network connectivity to Azure");
        Console.WriteLine("  • Valid Azure Foundry Local catalog access");
        Console.WriteLine("  • You can skip this example and continue with others");
    }
}

// ══════════════════════════════════════════════════════════════════
// Helper: Display Model Metadata
// ══════════════════════════════════════════════════════════════════
static async Task DisplayModelMetadata(string modelPath)
{
    var metadataPath = Path.Combine(modelPath, "inference_model.json");
    
    if (!File.Exists(metadataPath))
    {
        Console.WriteLine($"⚠ Metadata file not found at: {metadataPath}");
        return;
    }
    
    Console.WriteLine("Generated Metadata:");
    Console.WriteLine("───────────────────");
    
    // Parse the metadata using JsonSerializer
    var jsonContent = await File.ReadAllTextAsync(metadataPath);
    var metadata = JsonSerializer.Deserialize<InferenceModelMetadata>(
        jsonContent,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    );
    
    if (metadata == null)
    {
        Console.WriteLine("⚠ Failed to parse metadata");
        return;
    }
    
    // Display all fields
    Console.WriteLine($"  Name:            {metadata.Name}");
    Console.WriteLine($"  Model Family:    {metadata.ModelFamily ?? "N/A"}");
    Console.WriteLine($"  Task:            {metadata.Task ?? "N/A"}");
    Console.WriteLine($"  License:         {metadata.License ?? "N/A"}");
    
    if (!string.IsNullOrEmpty(metadata.LicenseDescription))
    {
        Console.WriteLine($"  License Info:    {metadata.LicenseDescription}");
    }
    
    Console.WriteLine($"  Context Length:  {metadata.ContextLength?.ToString() ?? "N/A"} tokens");
    Console.WriteLine($"  Max Output:      {metadata.MaxOutputTokens?.ToString() ?? "N/A"} tokens");
    
    if (metadata.ToolCalling != null)
    {
        Console.WriteLine($"  Tool Calling:    {(metadata.ToolCalling.Supports ? "✓ Supported" : "Not supported")}");
        if (metadata.ToolCalling.Supports)
        {
            Console.WriteLine($"    Start Token:   {metadata.ToolCalling.StartToken}");
            Console.WriteLine($"    End Token:     {metadata.ToolCalling.EndToken}");
        }
    }
    
    if (!string.IsNullOrEmpty(metadata.Description))
    {
        var desc = metadata.Description.Length > 100 
            ? metadata.Description[..100] + "..." 
            : metadata.Description;
        Console.WriteLine($"  Description:     {desc}");
    }
    
    Console.WriteLine($"  Source URL:      {metadata.SourceUrl ?? "N/A"}");
    Console.WriteLine($"  Downloaded:      {metadata.DownloadedAt?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "N/A"}");
    
    // Display Git version information
    if (!string.IsNullOrEmpty(metadata.Revision) || !string.IsNullOrEmpty(metadata.CommitSha))
    {
        Console.WriteLine();
        Console.WriteLine("  Git Version Info:");
        
        if (!string.IsNullOrEmpty(metadata.Revision))
        {
            Console.WriteLine($"    Revision:      {metadata.Revision}");
        }
        
        if (!string.IsNullOrEmpty(metadata.CommitSha))
        {
            var shortSha = metadata.CommitSha.Length > 8 
                ? metadata.CommitSha[..8] 
                : metadata.CommitSha;
            Console.WriteLine($"    Commit SHA:    {shortSha} (full: {metadata.CommitSha})");
        }
    }
    
    Console.WriteLine();
    
    // Show raw JSON for reference
    Console.WriteLine("Raw metadata JSON (inference_model.json):");
    Console.WriteLine("──────────────────────────────────────────");
    Console.WriteLine(jsonContent);
    Console.WriteLine();
}

// ══════════════════════════════════════════════════════════════════
// EXAMPLE 4: Attempt to Download Incompatible Model (Error Handling)
// ══════════════════════════════════════════════════════════════════
static async Task AttemptIncompatibleModelDownload(string baseDir, ILogger logger)
{
    // Try to download a popular model that doesn't have genai_config.json
    var modelId = "Qwen/Qwen2.5-0.5B-Instruct";
    
    var outputDir = HuggingFaceExtensions.BuildModelPath(baseDir, modelId);
    
    Console.WriteLine($"Model ID: {modelId}");
    Console.WriteLine($"Output Directory: {outputDir}");
    Console.WriteLine();
    Console.WriteLine("This model does NOT have genai_config.json...");
    Console.WriteLine();
    
    var downloadInfo = new DownloadModelInfo(
        ModelInfo: new ModelInfo(
            Uri: modelId,
            Revision: "main",
            Path: null
        ),
        OutputDirectory: baseDir,
        Token: null,
        BufferSize: null
    );
    
    try
    {
        Console.WriteLine("Attempting download...");
        
        // This will fail the pre-flight check
        await downloadInfo.HuggingFaceDownloadWithMetadataAsync(
            blobPathFilterPredicate: null,
            progress: null,
            logger: logger,
            cancellationToken: CancellationToken.None
        );
        
        Console.WriteLine("✗ Unexpected: Download should have failed!");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine("✓ Error caught gracefully (as expected):");
        Console.WriteLine($"   {ex.Message}");
        Console.WriteLine();
        Console.WriteLine("This demonstrates that the system:");
        Console.WriteLine("  • Validates model compatibility BEFORE downloading");
        Console.WriteLine("  • Saves time by not downloading incompatible models");
        Console.WriteLine("  • Provides clear error messages");
        Console.WriteLine("  • Recommends compatible alternatives");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Unexpected error: {ex.GetType().Name}");
        Console.WriteLine($"   {ex.Message}");
        logger.LogError(ex, "Unexpected error");
    }
}

// ══════════════════════════════════════════════════════════════════
// EXAMPLE 5: Load and Run Inference
// ══════════════════════════════════════════════════════════════════
static async Task RunInferenceExample(string baseDir, ILogger logger)
{
    var modelId = "microsoft/Phi-3-mini-4k-instruct-onnx";
    var subdirectoryPath = "cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4";
    
    Console.WriteLine($"Model ID: {modelId}");
    Console.WriteLine($"Subdirectory: {subdirectoryPath}");
    
    // Initialize Foundry Local Core to access catalog
    if (!FoundryLocalCore.IsInitialized)
    {
        var initConfig = new Dictionary<string, string>
        {
            ["ModelCacheDir"] = baseDir
        };
        await FoundryLocalCore.InitializeAsync(initConfig);
    }
    
    // Find the model in the catalog (will have version suffix)
    var catalog = FoundryLocalCore.Instance.GetCatalog();
    var cachedModels = await catalog.GetCachedModelsAsync(CancellationToken.None);
    
    logger.LogInformation($"Total cached models: {cachedModels.Count}");
    foreach (var m in cachedModels)
    {
        logger.LogInformation($"  Cached: {m}");
    }
    
    var versionedModelId = cachedModels.FirstOrDefault(m => m.StartsWith(modelId + ":"));
    
    if (versionedModelId == null)
    {
        Console.WriteLine();
        Console.WriteLine("⚠ Model not found in cache.");
        Console.WriteLine($"Looking for: {modelId}:*");
        Console.WriteLine();
        Console.WriteLine("Trying manual filesystem search...");
        
        // Manual fallback
        var parts = modelId.Split('/');
        if (parts.Length == 2)
        {
            var modelBaseDir = Path.Combine(baseDir, "HuggingFace", parts[0], parts[1]);
            if (Directory.Exists(modelBaseDir))
            {
                var versionedPath = Directory.GetDirectories(modelBaseDir).FirstOrDefault();
                if (versionedPath != null)
                {
                    Console.WriteLine($"Found versioned folder: {versionedPath}");
                    var metadataPath = Directory.GetFiles(versionedPath, "inference_model.json", SearchOption.AllDirectories)
                        .FirstOrDefault();
                    
                    if (metadataPath != null)
                    {
                        var metadataDir = Path.GetDirectoryName(metadataPath);
                        Console.WriteLine($"Found model at: {metadataDir}");
                        
                        // Extract version from path for model ID
                        var versionFolder = Path.GetFileName(versionedPath);
                        versionedModelId = $"{modelId}:{versionFolder}";
                        Console.WriteLine($"Using model ID: {versionedModelId}");
                        Console.WriteLine();
                    }
                }
            }
        }
        
        if (versionedModelId == null)
        {
            Console.WriteLine("❌ Could not find model. Please run Example 1 first to download.");
            return;
        }
    }
    
    // Get the actual model path from catalog
    var modelPath = await catalog.GetModelPathAsync(versionedModelId, CancellationToken.None);
    
    if (modelPath == null || !Directory.Exists(modelPath))
    {
        Console.WriteLine();
        Console.WriteLine("⚠ Model path not found via catalog, trying manual path construction...");
        
        // Manual fallback for path
        var parts = modelId.Split('/');
        if (parts.Length == 2)
        {
            var versionSuffix = versionedModelId.Split(':').Last();
            var modelBaseDir = Path.Combine(baseDir, "HuggingFace", parts[0], parts[1], versionSuffix);
            var subdir = "cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4";
            modelPath = Path.Combine(modelBaseDir, subdir);
            
            if (Directory.Exists(modelPath))
            {
                Console.WriteLine($"Found model at: {modelPath}");
            }
            else
            {
                Console.WriteLine("❌ Model path not found. Please run Example 1 first to download the model.");
                return;
            }
        }
        else
        {
            Console.WriteLine("❌ Could not construct model path.");
            return;
        }
    }
    
    Console.WriteLine($"Found model: {versionedModelId}");
    Console.WriteLine($"Model Path: {modelPath}");
    Console.WriteLine();
    
    try
    {
        // Load the model with the full path since it's not in catalog yet
        Console.WriteLine("Loading model...");
        var modelManager = FoundryLocalCore.Instance.GetModelManager();
        await modelManager.LoadModelAsync(
            modelIdNameOrAlias: versionedModelId,  // Use versioned ID
            customModelPath: modelPath,  // Provide the full path
            ct: CancellationToken.None
        );
        
        // Verify model loaded
        var loadedModel = await modelManager.GetLoadedModelAsync(versionedModelId, CancellationToken.None);
        if (loadedModel == null)
        {
            Console.WriteLine($"✗ Failed to load model");
            return;
        }
        
        Console.WriteLine("✓ Model loaded");
        Console.WriteLine();
        
        // Get chat client
        Console.WriteLine("Creating chat client...");
        var chatClient = await FoundryLocalCore.Instance.GetChatClientAsync(versionedModelId);
        Console.WriteLine("✓ Chat client ready");
        Console.WriteLine();
        
        // Run inference - Non-streaming
        Console.WriteLine("Running inference (non-streaming)...");
        Console.WriteLine("Prompt: What is the capital of France?");
        Console.WriteLine();
        
        var request = new ChatCompletionCreateRequest
        {
            Model = versionedModelId,
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem("You are a helpful assistant."),
                ChatMessage.FromUser("What is the capital of France? Answer in one sentence.")
            },
            MaxTokens = 100,
            Temperature = 0.7f
        };
        
        var response = await chatClient.HandleRequestAsync(
            new Microsoft.Neutron.OpenAI.ChatCompletionCreateRequestExtended
            {
                Model = request.Model,
                Messages = request.Messages,
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature
            },
            CancellationToken.None
        );
        
        Console.WriteLine("Response:");
        Console.WriteLine("─────────");
        if (response.Choices?.Count > 0)
        {
            var message = response.Choices[0].Message?.Content;
            Console.WriteLine(message);
        }
        Console.WriteLine();
        
        // Run inference - Streaming
        Console.WriteLine("Running inference (streaming)...");
        Console.WriteLine("Prompt: Tell me a short joke");
        Console.WriteLine();
        
        var streamRequest = new Microsoft.Neutron.OpenAI.ChatCompletionCreateRequestExtended
        {
            Model = modelId,
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem("You are a funny assistant."),
                ChatMessage.FromUser("Tell me a short joke about programming.")
            },
            MaxTokens = 150,
            Temperature = 0.8f
        };
        
        Console.WriteLine("Response (streaming):");
        Console.WriteLine("─────────────────────");
        await foreach (var chunk in chatClient.HandleStreamRequestAsync(
            streamRequest,
            CancellationToken.None))
        {
            if (chunk.Choices?.Count > 0)
            {
                var content = chunk.Choices[0].Delta?.Content;
                if (!string.IsNullOrEmpty(content))
                {
                    Console.Write(content);
                }
            }
        }
        Console.WriteLine();
        Console.WriteLine();
        
        Console.WriteLine("✓ Inference completed successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Error during inference: {ex.Message}");
        logger.LogError(ex, "Inference failed");
    }
}

// ══════════════════════════════════════════════════════════════════
// EXAMPLE 6: List Cached Models
// ══════════════════════════════════════════════════════════════════
static async Task ListCachedModelsExample(string baseDir, ILogger logger)
{
    Console.WriteLine("Listing all cached models in the model cache...");
    Console.WriteLine();
    
    try
    {
        // Initialize Foundry Local Core if not already initialized
        if (!FoundryLocalCore.IsInitialized)
        {
            Console.WriteLine("Initializing Foundry Local Core...");
            var config = new Dictionary<string, string>
            {
                ["ModelCacheDir"] = baseDir
            };
            await FoundryLocalCore.InitializeAsync(config);
            Console.WriteLine("✓ Initialized");
            Console.WriteLine();
        }
        
        // Get the model catalog
        var catalog = FoundryLocalCore.Instance.GetCatalog();
        
        // Get list of cached models
        var cachedModels = await catalog.GetCachedModelsAsync(CancellationToken.None);
        
        if (cachedModels.Count == 0)
        {
            Console.WriteLine("No cached models found.");
            Console.WriteLine($"Cache directory: {baseDir}");
            Console.WriteLine();
            Console.WriteLine("Tip: Run Examples 1-2 to download models first.");
            return;
        }
        
        Console.WriteLine($"Found {cachedModels.Count} cached model(s):");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        
        foreach (var modelId in cachedModels)
        {
            // Get full model info
            var modelInfo = await catalog.GetModelInfoAsync(modelId, CancellationToken.None);
            
            if (modelInfo == null)
            {
                Console.WriteLine($"⚠ Model ID: {modelId} (info not available)");
                continue;
            }
            
            // Display all model information
            Console.WriteLine($"Model: {modelInfo.Name}");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            Console.WriteLine($"  ID:              {modelInfo.Id}");
            Console.WriteLine($"  Alias:           {modelInfo.Alias}");
            Console.WriteLine($"  Display Name:    {modelInfo.DisplayName ?? "N/A"}");
            Console.WriteLine($"  Version:         {modelInfo.Version}");
            Console.WriteLine($"  Provider:        {modelInfo.ProviderType}");
            Console.WriteLine($"  Model Type:      {modelInfo.ModelType}");
            Console.WriteLine($"  Task:            {modelInfo.Task ?? "N/A"}");
            Console.WriteLine($"  Publisher:       {modelInfo.Publisher ?? "N/A"}");
            Console.WriteLine($"  License:         {modelInfo.License ?? "N/A"}");
            
            if (!string.IsNullOrEmpty(modelInfo.LicenseDescription))
            {
                var desc = modelInfo.LicenseDescription.Length > 60 
                    ? modelInfo.LicenseDescription[..60] + "..." 
                    : modelInfo.LicenseDescription;
                Console.WriteLine($"  License Info:    {desc}");
            }
            
            Console.WriteLine($"  URI:             {modelInfo.Uri}");
            Console.WriteLine($"  Parent URI:      {modelInfo.ParentModelUri ?? "N/A"}");
            Console.WriteLine($"  Cached:          {(modelInfo.Cached ? "✓ Yes" : "✗ No")}");
            
            if (modelInfo.Runtime != null)
            {
                Console.WriteLine($"  Runtime:");
                Console.WriteLine($"    Device:        {modelInfo.Runtime.DeviceType}");
                Console.WriteLine($"    Provider:      {modelInfo.Runtime.ExecutionProvider}");
            }
            
            Console.WriteLine($"  File Size:       {modelInfo.FileSizeMb?.ToString() ?? "N/A"} MB");
            Console.WriteLine($"  Max Output:      {modelInfo.MaxOutputTokens?.ToString() ?? "N/A"} tokens");
            
            if (modelInfo.SupportsToolCalling == true)
            {
                Console.WriteLine($"  Tool Calling:    ✓ Supported");
                Console.WriteLine($"    Start Token:   {modelInfo.ToolCallStart ?? "N/A"}");
                Console.WriteLine($"    End Token:     {modelInfo.ToolCallEnd ?? "N/A"}");
            }
            else
            {
                Console.WriteLine($"  Tool Calling:    Not supported");
            }
            
            if (modelInfo.CreatedAtUnix > 0)
            {
                var createdAt = DateTimeOffset.FromUnixTimeSeconds(modelInfo.CreatedAtUnix);
                Console.WriteLine($"  Created:         {createdAt:yyyy-MM-dd HH:mm:ss UTC}");
            }
            
            // Get and display model path
            var modelPath = await catalog.GetModelPathAsync(modelId, CancellationToken.None);
            if (modelPath != null)
            {
                Console.WriteLine($"  Path:            {modelPath}");
                
                // For HuggingFace models, also display revision and commit SHA if available
                if (modelInfo.ProviderType == "HuggingFace")
                {
                    var inferenceMetadataPath = Path.Combine(modelPath, "inference_model.json");
                    if (File.Exists(inferenceMetadataPath))
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(inferenceMetadataPath);
                            var metadata = JsonSerializer.Deserialize<InferenceModelMetadata>(
                                jsonContent,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                            );
                            
                            if (metadata != null)
                            {
                                if (!string.IsNullOrEmpty(metadata.Revision))
                                {
                                    Console.WriteLine($"  Git Revision:    {metadata.Revision}");
                                }
                                
                                if (!string.IsNullOrEmpty(metadata.CommitSha))
                                {
                                    var shortSha = metadata.CommitSha.Length > 8 
                                        ? metadata.CommitSha[..8] 
                                        : metadata.CommitSha;
                                    Console.WriteLine($"  Git Commit:      {shortSha} (full: {metadata.CommitSha})");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning($"Could not read version info from metadata: {ex.Message}");
                        }
                    }
                }
            }
            
            // Display prompt templates if available
            if (modelInfo.PromptTemplate != null && modelInfo.PromptTemplate.Count > 0)
            {
                Console.WriteLine($"  Prompt Templates:");
                foreach (var template in modelInfo.PromptTemplate)
                {
                    var templatePreview = template.Value.Length > 50 
                        ? template.Value[..50] + "..." 
                        : template.Value;
                    Console.WriteLine($"    {template.Key}: {templatePreview}");
                }
            }
            
            // Display model settings if available
            if (modelInfo.ModelSettings?.Parameters != null && modelInfo.ModelSettings.Parameters.Length > 0)
            {
                Console.WriteLine($"  Model Settings:");
                foreach (var param in modelInfo.ModelSettings.Parameters)
                {
                    Console.WriteLine($"    {param.Name}: {param.Value ?? "(no value)"}");
                }
            }
            
            Console.WriteLine();
        }
        
        Console.WriteLine("✓ Model cache listing completed!");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error listing cached models");
        Console.WriteLine($"❌ Error: {ex.Message}");
    }
}

// ══════════════════════════════════════════════════════════════════
// TEST download_model COMMAND
// ══════════════════════════════════════════════════════════════════
static async Task TestDownloadModelCommand(string baseDir, ILogger logger)
{
    Console.WriteLine("Testing Core's download_model with HuggingFace path...");
    Console.WriteLine();
    
    var config = new Dictionary<string, string>
    {
        ["ModelCacheDir"] = baseDir
    };
    
    await FoundryLocalCore.InitializeAsync(config);
    var catalog = FoundryLocalCore.Instance.GetCatalog();
    
    // Test with the same model path the SDK is using
    var modelPath = "microsoft/Phi-3-mini-4k-instruct-onnx/cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4";
    
    Console.WriteLine($"Calling catalog.DownloadModelAsync with: {modelPath}");
    Console.WriteLine();
    
    try
    {
        var result = await catalog.DownloadModelAsync(modelPath, null, CancellationToken.None);
        
        if (result == DownloadModelResult.Success)
        {
            Console.WriteLine($"✓ Download succeeded!");
        }
        else if (result == DownloadModelResult.AlreadyCached)
        {
            Console.WriteLine($"✓ Model already cached!");
        }
        else
        {
            Console.WriteLine($"❌ Download failed: {result}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Exception: {ex.Message}");
        logger.LogError(ex, "download_model test failed");
        throw;
    }
    
    Console.WriteLine();
}
