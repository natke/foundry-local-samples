# HuggingFace Model Download Sample

This sample demonstrates how to download HuggingFace models using **Foundry Local Core** with automatic metadata generation.

## What This Sample Shows

1. **Simple Model Download**: Download a complete model from HuggingFace Hub
2. **Subdirectory Model Download**: Download a specific variant from a model with subdirectories
3. **Azure Catalog Model Download**: Download a model from Azure Foundry Local catalog (qwen2.5-7b)
4. **Error Handling**: Demonstrates graceful handling of incompatible models (without genai_config.json)
5. **Load and Run Inference**: Load the downloaded model and run chat completions (streaming and non-streaming)
6. **List Cached Models**: Display all cached models with comprehensive metadata (20+ fields)
7. **Automatic Metadata Generation**: Get comprehensive model metadata including:
   - Model family (Qwen, Phi, Llama, Mistral, etc.)
   - Task type (chat-completion, text-generation, embedding)
   - License information
   - Context length and max output tokens
   - Tool calling support detection
   - Description from README
   - Source URL and download timestamp

## Prerequisites

- .NET 9.0 SDK or later
- Foundry Local Core library (included via project reference)
- Internet connection to download models from HuggingFace
- **Important**: Only `onnxruntime/*` models are compatible (they include genai_config.json)

## Building and Running

```bash
# From the sample directory
dotnet build
dotnet run

# Or directly
dotnet run --project HuggingFaceDownloadSample.csproj
```

## How It Works

The sample uses the `HuggingFaceDownloadWithMetadataAsync()` extension method provided by Foundry Local Core:

```csharp
var downloadInfo = new DownloadModelInfo(
    ModelInfo: new ModelInfo(
        Uri: "Qwen/Qwen2.5-0.5B-Instruct",
        Revision: "main",
        Path: null  // null = entire model, or specify subdirectory
    ),
    OutputDirectory: "/path/to/output",
    Token: null,  // Optional HuggingFace token for private models
    BufferSize: null
);

await downloadInfo.HuggingFaceDownloadWithMetadataAsync(
    progress: progressReporter,
    logger: logger,
    cancellationToken: cancellationToken
);
```

## What Gets Downloaded

After running, you'll have:

```
~/.foundry-local/HuggingFace/Qwen/Qwen2.5-0.5B-Instruct/
├── config.json                 # Model configuration
├── tokenizer_config.json       # Tokenizer settings
├── added_tokens.json           # Special tokens (tool calling)
├── README.md                   # Model documentation
├── model.onnx                  # ONNX model file
├── inference_model.json        # 🎯 Generated metadata (NEW!)
└── ... other model files
```

## Generated Metadata Example

The `inference_model.json` file contains:

```json
{
  "Name": "Qwen/Qwen2.5-0.5B-Instruct",
  "PromptTemplate": null,
  "ModelFamily": "qwen",
  "Task": "chat-completion",
  "License": "Apache-2.0",
  "LicenseDescription": null,
  "ContextLength": 32768,
  "MaxOutputTokens": 4096,
  "Description": "Qwen2.5 is the latest series of Qwen large language models...",
  "ToolCalling": {
    "Supports": true,
    "StartToken": "<tool_call>",
    "EndToken": "</tool_call>"
  },
  "SourceUrl": "https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct",
  "DownloadedAt": "2026-03-03T01:30:00.0000000Z"
}
```

## Key Features

### 1. Automatic Metadata Detection

The system automatically detects:
- **Model Family**: From model ID patterns (qwen, phi, llama, mistral, etc.)
- **Task Type**: From HuggingFace API or model ID keywords
- **License**: From README frontmatter or HuggingFace API
- **Context Length**: From `config.json` max_position_embeddings
- **Tool Calling**: From special tokens in `added_tokens.json`

### 2. Subdirectory Support

Many models have multiple variants in subdirectories:
```csharp
var downloadInfo = new DownloadModelInfo(
    ModelInfo: new ModelInfo(
        Uri: "onnxruntime/gpt-oss-20b-onnx",
        Revision: "main",
        Path: "webgpu/webgpu-int4-rtn-block-32"  // Specific variant
    ),
    OutputDirectory: outputDir,
    Token: null,
    BufferSize: null
);
```

Metadata is correctly placed in the subdirectory path.

### 3. Backward Compatibility

All new metadata fields are optional - existing Azure models continue to work:
```json
{
  "Name": "Phi-3-mini-4k-instruct",
  "PromptTemplate": { "user": "...", "assistant": "..." }
  // No HuggingFace-specific fields = works perfectly
}
```

### 4. Progress Reporting

Track download progress with the built-in progress reporter:
```csharp
var progress = new Progress<(string? fileName, double? percent)>(p =>
{
    Console.WriteLine($"Downloading {p.fileName}: {p.percent:F1}%");
});
```

### 5. Reading Metadata

Use the existing parser to read generated metadata:
```csharp
using Microsoft.AI.Foundry.Local;

var metadata = AzureModelCatalog.ParseInferenceModelJson(
    Path.Combine(modelPath, "inference_model.json")
);

// Access all fields
if (metadata.ToolCalling?.Supports == true)
{
    Console.WriteLine($"Model supports tool calling!");
}
```

## Integration Examples

### Use in Your Application

```csharp
public class ModelDownloader
{
    private readonly ILogger _logger;
    private readonly string _modelsDirectory;
    
    public async Task<string> DownloadModelAsync(
        string modelId,
        CancellationToken cancellationToken)
    {
        var outputPath = Path.Combine(
            _modelsDirectory, 
            modelId.Replace("/", "_")
        );
        
        var downloadInfo = new DownloadModelInfo(
            new ModelInfo(modelId, "main"),
            outputPath
        );
        
        await downloadInfo.HuggingFaceDownloadWithMetadataAsync(
            logger: _logger,
            cancellationToken: cancellationToken
        );
        
        return outputPath;
    }
    
    public ModelMetadata GetModelInfo(string modelPath)
    {
        var metadataPath = Path.Combine(modelPath, "inference_model.json");
        var metadata = AzureModelCatalog.ParseInferenceModelJson(metadataPath);
        
        return new ModelMetadata
        {
            Name = metadata.Name,
            Family = metadata.ModelFamily ?? "unknown",
            ContextLength = metadata.ContextLength ?? 0,
            SupportsTools = metadata.ToolCalling?.Supports ?? false
        };
    }
}
```

### Filter Downloaded Files

```csharp
// Download only ONNX files, skip safetensors
await downloadInfo.HuggingFaceDownloadWithMetadataAsync(
    blobPathFilterPredicate: path => 
        path.EndsWith(".onnx") || 
        path.EndsWith(".json") || 
        path.EndsWith(".txt"),
    logger: logger
);
```

### Private Model Download

```csharp
// For private models, provide a HuggingFace token
var downloadInfo = new DownloadModelInfo(
    ModelInfo: new ModelInfo("your-org/private-model", "main"),
    OutputDirectory: outputPath,
    Token: "hf_xxxxxxxxxxxxxxxxxxxxxxxxxx",  // Your HF token
    BufferSize: null
);
```

## Model Examples to Try

### ⚠️ Important: genai_config.json Requirement

**Foundry Local Core requires models to have `genai_config.json` for inference.**

Only use models from the `onnxruntime` organization on HuggingFace, which are ONNX Runtime GenAI compatible:

### Compatible Models (Have genai_config.json)

#### Small Models (< 5GB)
- `microsoft/Phi-3-mini-4k-instruct-onnx` - 3.8B parameters, 4K context ✅
  - Subdirectory: `cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4` (CPU optimized, ~2GB)
  - Subdirectory: `cuda-int4-rtn-block-32` (GPU optimized)
- `onnxruntime/Phi-3.5-mini-instruct-onnx` - 3.8B parameters, 128K context ✅

#### Medium Models (5-20GB)
- `onnxruntime/Phi-3-medium-4k-instruct-onnx` - 14B parameters, 4K context ✅

#### Specialized Variants
- `onnxruntime/gpt-oss-20b-onnx` - WebGPU optimized variants ✅
  - Path: `webgpu/webgpu-int4-rtn-block-32`
  - Path: `cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4`

### ❌ Incompatible Models (Missing genai_config.json)
- `Qwen/Qwen2.5-0.5B-Instruct` - No genai_config.json
- `microsoft/Phi-3-mini-4k-instruct` - No genai_config.json (use onnxruntime version)
- `meta-llama/Llama-3.2-1B-Instruct` - No genai_config.json

**Find more compatible models**: https://huggingface.co/onnxruntime

## Architecture

```
Your Application
    │
    ├─→ DownloadModelInfo.HuggingFaceDownloadWithMetadataAsync()
    │       │
    │       ├─→ Downloads model files from HuggingFace Hub
    │       │   (config.json, tokenizer_config.json, added_tokens.json, README.md, model files)
    │       │
    │       └─→ HuggingFaceMetadataGenerator.GenerateMetadataAsync()
    │               │
    │               ├─→ Reads downloaded files
    │               ├─→ Detects model family, task, license
    │               ├─→ Extracts context length, tool calling support
    │               └─→ Generates InferenceModelMetadata
    │
    └─→ Saves inference_model.json to model directory
```

## Error Handling

The download system is resilient:

```csharp
try
{
    await downloadInfo.HuggingFaceDownloadWithMetadataAsync(
        logger: logger,
        cancellationToken: cancellationToken
    );
}
catch (HttpRequestException ex)
{
    // Network error or model not found
    logger.LogError($"Failed to download: {ex.Message}");
}
catch (UnauthorizedAccessException ex)
{
    // Invalid token for private model
    logger.LogError($"Access denied: {ex.Message}");
}
catch (IOException ex)
{
    // Disk error
    logger.LogError($"IO error: {ex.Message}");
}
```

Metadata generation handles missing files gracefully:
- If `README.md` is missing → Description will be null
- If `added_tokens.json` is missing → Tool calling will be null
- If HuggingFace API fails → Falls back to local file detection only

## Next Steps

After downloading a model with metadata:

1. **Load for Inference**: Use ONNX Runtime GenAI to load the model
2. **Display in UI**: Show metadata in model selection UI
3. **Validate License**: Check license field before use
4. **Configure Context**: Use ContextLength to set max input tokens
5. **Enable Tool Calling**: If ToolCalling.Supports == true, enable function calling

## Troubleshooting

### Model Download Fails
- Check internet connection
- Verify model ID exists on HuggingFace Hub
- For private models, ensure valid token is provided

### Metadata Not Generated
- Check logs for warnings
- Verify model directory has required files (config.json)
- Ensure write permissions on output directory

### Incorrect Metadata
- Some fields may be null if source data unavailable
- File an issue with model ID for detection improvements

## Learn More

- **Implementation Plan**: `docs/DownloadHuggingFaceModelPlan.md`
- **Status**: `docs/PhaseStatusSummary.md`
- **HuggingFace Hub**: https://huggingface.co/models
- **ONNX Runtime GenAI**: https://github.com/microsoft/onnxruntime-genai

## License

This sample is provided under the same license as Foundry Local Core.
