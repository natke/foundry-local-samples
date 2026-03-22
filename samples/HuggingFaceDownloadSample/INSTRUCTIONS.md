# How to Run the HuggingFace Model Download Sample

## Quick Start

```bash
# Navigate to the sample directory
cd samples/HuggingFaceDownloadSample

# Build the sample
dotnet build

# Run the sample
dotnet run
```

## What the Sample Does

The sample demonstrates six examples:

### Example 1: Simple Model Download
- Downloads `microsoft/Phi-3-mini-4k-instruct-onnx/cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4`
- CPU-optimized INT4 quantized variant (~2GB)
- Skips download if model already exists
- Shows automatic metadata generation
- Displays model information

### Example 2: Subdirectory Model Download
- Downloads `onnxruntime/gpt-oss-20b-onnx/webgpu/webgpu-int4-rtn-block-32`
- Skips download if model already exists
- Demonstrates handling models with subdirectory structure
- Shows metadata placement in correct subdirectory

### Example 3: Azure Catalog Model Download
- Downloads `qwen2.5-1.5b-instruct-generic-cpu:4` from Azure Foundry Local catalog
- Checks if model is already cached before downloading
- Skips download if cached and displays metadata instead
- Demonstrates integration with Azure model catalog
- Uses catalog API to download Azure-hosted models
- Shows Azure catalog metadata integration

### Example 4: Error Handling for Incompatible Models
- Attempts to download `Qwen/Qwen2.5-0.5B-Instruct` (no genai_config.json)
- Demonstrates pre-flight validation check
- Shows graceful error handling
- Explains why validation prevents wasted downloads

### Example 5: Load and Run Inference
- Initializes Foundry Local Core
- Loads the downloaded model
- Runs non-streaming chat completion
- Runs streaming chat completion
- Demonstrates complete end-to-end workflow

### Example 6: List Cached Models
- Lists all models in the cache (both HuggingFace and Azure)
- Displays comprehensive model information (20+ fields)
- Shows model paths, capabilities, and metadata
- Verifies cache discovery works for all model types
- Demonstrates integration with model catalog

## Expected Output

Example 1 downloads and displays metadata, Example 2 handles subdirectories, and Example 3 runs inference:

```
╔════════════════════════════════════════════════════════════════╗
║  HuggingFace Model Download Sample - Foundry Local Core       ║
╚════════════════════════════════════════════════════════════════╝

Example 1: Downloading a simple HuggingFace model
──────────────────────────────────────────────────
Model ID: Qwen/Qwen2.5-0.5B-Instruct
Output Directory: /Users/[username]/.foundry-local/HuggingFace/Qwen/Qwen2.5-0.5B-Instruct

✓ Model already downloaded, skipping download...

Generated Metadata:
───────────────────
  Name:            Qwen/Qwen2.5-0.5B-Instruct
  Model Family:    qwen
  Task:            chat-completion
  License:         Apache-2.0
  Context Length:  32768 tokens
  Max Output:      4096 tokens

Press Enter to continue to Example 2...

Example 2: Downloading a model with subdirectory path
──────────────────────────────────────────────────────
...

Press Enter to continue to Example 3...

Example 3: Attempting to download incompatible model
──────────────────────────────────────────────────────
Model ID: Qwen/Qwen2.5-0.5B-Instruct
Output Directory: /Users/[username]/.foundry-local/HuggingFace/Qwen/Qwen2.5-0.5B-Instruct

This model does NOT have genai_config.json...

Attempting download...
✓ Error caught gracefully (as expected):
   Model 'Qwen/Qwen2.5-0.5B-Instruct' does not contain genai_config.json and is not compatible with Foundry Local Core. Only ONNX Runtime GenAI models can be used for inference. Recommended models from https://huggingface.co/onnxruntime: onnxruntime/Phi-3-mini-4k-instruct-onnx, onnxruntime/Phi-3.5-mini-instruct-onnx, onnxruntime/gpt-oss-20b-onnx

This demonstrates that the system:
  • Validates model compatibility BEFORE downloading
  • Saves time by not downloading incompatible models
  • Provides clear error messages
  • Recommends compatible alternatives

Press Enter to continue to Example 4...

Example 4: Loading and running inference with the model
─────────────────────────────────────────────────────────
Model Path: /Users/[username]/.foundry-local/HuggingFace/Qwen/Qwen2.5-0.5B-Instruct

Initializing Foundry Local Core...
✓ Initialized

Loading model...
✓ Model loaded

Creating chat client...
✓ Chat client ready

Running inference (non-streaming)...
Prompt: What is the capital of France?

Response:
─────────
The capital of France is Paris.

Running inference (streaming)...
Prompt: Tell me a short joke

Response (streaming):
─────────────────────
Why do programmers prefer dark mode? Because light attracts bugs! 🐛

✓ Inference completed successfully!

Press Enter to continue to Example 2...
```

## Downloaded Files Location

Models are now organized by source and organization:
```
~/.foundry-local/HuggingFace/
├── microsoft/
│   └── Phi-3-mini-4k-instruct-onnx/
│       ├── LICENSE                    # ← Root metadata files
│       ├── README.md                  # ← Root metadata files
│       ├── config.json                # ← Root metadata files
│       └── cpu_and_mobile/
│           └── cpu-int4-rtn-block-32-acc-level-4/
│               ├── genai_config.json
│               ├── inference_model.json
│               ├── model.onnx
│               └── ... other files
└── onnxruntime/
    └── gpt-oss-20b-onnx/
        ├── LICENSE                    # ← Root metadata files
        ├── README.md                  # ← Root metadata files
        ├── config.json                # ← Root metadata files
        └── webgpu/
            └── webgpu-int4-rtn-block-32/
                ├── genai_config.json
                ├── inference_model.json
                └── ... other files
```

## Requirements

- **Internet connection**: To download models from HuggingFace Hub
- **Disk space**: ~1-2GB for both example models
- **Time**: 2-5 minutes for download + 1-2 minutes for inference
- **Platform**: macOS ARM64 (configured in .csproj, adapt for other platforms)

## Customizing the Sample

### For Different Platforms

The sample is configured for macOS ARM64. For other platforms, update the RuntimeIdentifier in the .csproj:

```xml
<PropertyGroup>
  <RuntimeIdentifier>osx-arm64</RuntimeIdentifier>  <!-- macOS ARM64 (Apple Silicon) -->
  <!-- OR -->
  <RuntimeIdentifier>osx-x64</RuntimeIdentifier>    <!-- macOS Intel -->
  <!-- OR -->
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>    <!-- Windows x64 -->
  <!-- OR -->
  <RuntimeIdentifier>linux-x64</RuntimeIdentifier>  <!-- Linux x64 -->
</PropertyGroup>
```

### Download a Different Model

Edit `Program.cs` and change the model IDs:

```csharp
// Line ~77 - Change the model ID
var modelId = "microsoft/Phi-3-mini-4k-instruct";  // Try Phi instead of Qwen
```

### Use a HuggingFace Token

For private models or to avoid rate limits:

```csharp
var downloadInfo = new DownloadModelInfo(
    ModelInfo: new ModelInfo(modelId, "main"),
    OutputDirectory: outputDir,
    Token: "hf_xxxxxxxxxxxxxxxxxxxxxxxxxx",  // Your HF token
    BufferSize: null
);
```

### Change Download Location

```csharp
// Line ~35 - Change the base directory
var outputBaseDir = Path.Combine(
    Path.GetTempPath(),
    "my-models"  // Custom location
);
```

## Troubleshooting

### Build Errors

If you get NuGet errors:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# Build again
dotnet build
```

### Network Errors

If download fails due to network issues:
- Check internet connection
- Verify HuggingFace Hub is accessible: https://huggingface.co
- Try using a HuggingFace token if rate limited

### Permission Errors

If you can't write to `~/.foundry-local`:
- Change `outputBaseDir` in Program.cs to a writable location
- Or run with appropriate permissions

## What Happens Behind the Scenes

1. **Download Phase**: 
   - Fetches model files from HuggingFace Hub
   - Shows progress for each file
   - Saves to local directory

2. **Metadata Generation Phase**:
   - Reads `config.json` for context length
   - Reads `tokenizer_config.json` and `added_tokens.json` for tool calling detection
   - Reads `README.md` for license and description
   - Detects model family from model ID
   - Generates comprehensive `inference_model.json`

3. **Display Phase**:
   - Parses the generated metadata
   - Shows formatted output
   - Displays raw JSON

## Next Steps

After running the sample:

1. **Examine the generated metadata** at `~/.foundry-local/HuggingFace/*/inference_model.json`
2. **Use in your application** - Copy the download pattern to your code
3. **Load the model** - Use ONNX Runtime GenAI to load and run inference
4. **Read the detailed README** - See `README.md` for more integration examples

## Code Reference

The key API call is:
```csharp
await downloadInfo.HuggingFaceDownloadWithMetadataAsync(
    blobPathFilterPredicate: null,  // Optional file filter
    progress: progressReporter,     // Optional progress tracking
    logger: logger,                 // Optional logging
    cancellationToken: cancellationToken
);
```

This single call:
- ✅ Downloads all model files
- ✅ Generates comprehensive metadata
- ✅ Saves `inference_model.json`
- ✅ Handles subdirectories automatically
- ✅ Reports progress
- ✅ Logs all operations

## Support

For questions or issues:
- See the full documentation in `README.md`
- Check the implementation plan: `../../docs/DownloadHuggingFaceModelPlan.md`
- Review the status: `../../docs/PhaseStatusSummary.md`
