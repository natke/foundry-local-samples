# C# SDK - HuggingFace E2E Test Sample

End-to-end sample demonstrating the unified HuggingFace model download pattern.

## What This Tests

1. ✅ Adding a HuggingFace model to the catalog using `AddHuggingFaceModelAsync()`
2. ✅ Checking if the model is already cached
3. ✅ Downloading using the standard `DownloadAsync()` method (unified pattern)
4. ✅ Progress callback during download
5. ✅ Loading the model into memory
6. ✅ Running inference with the model
7. ✅ Verifying the model appears in catalog queries

## Unified Pattern Demonstrated

This sample shows that HuggingFace models use the **same download workflow** as Azure catalog models:

```csharp
// Azure catalog model
var azureModel = await catalog.GetModelAsync("phi-3-mini");
await azureModel.DownloadAsync(progress => Console.WriteLine($"{progress}%"));
await azureModel.LoadAsync();

// HuggingFace model (SAME pattern for download/load!)
var hfModel = await catalog.AddHuggingFaceModelAsync("microsoft/Phi-3...");
await hfModel.DownloadAsync(progress => Console.WriteLine($"{progress}%"));
await hfModel.LoadAsync();
```

## Prerequisites

1. **Foundry Local Core**: Must be built and available
2. **Model Cache Directory**: `~/.foundry-local` (created automatically)
3. **.NET 9.0 SDK**: Required to build and run

## How to Run

```bash
cd samples/E2E_CSharp_HuggingFace
dotnet build
dotnet run
```

## Expected Output

```
╔════════════════════════════════════════════════════════════════╗
║  C# SDK - HuggingFace Unified Download Pattern E2E Test       ║
╚════════════════════════════════════════════════════════════════╝

Step 1: Initializing Foundry Local Manager
───────────────────────────────────────────────────
✓ Initialized with cache: /Users/user/.foundry-local

Step 2: Getting Catalog
───────────────────────────────────────────────────
✓ Catalog: FoundryLocalCatalog

Step 3: Adding HuggingFace Model to Catalog
───────────────────────────────────────────────────
Model URI: microsoft/Phi-3-mini-4k-instruct-onnx
Subdirectory: cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4
Adding to catalog (this fetches metadata only, no download)...
✓ Model added: microsoft/Phi-3-mini-4k-instruct-onnx:5f5f794c
  Alias: phi-3-mini
  Display Name: Phi-3 Mini Instruct

Step 4: Checking Cache Status
───────────────────────────────────────────────────
Model cached: false
Model not cached, will download...

Step 5: Downloading Model Files
───────────────────────────────────────────────────
Using standard DownloadAsync method (same as Azure models)

  Progress: 5.0%
  Progress: 10.0%
  ...
  Progress: 100.0%
✓ Download complete!
  Model path: /Users/user/.foundry-local/HuggingFace/microsoft/Phi-3-mini-4k-instruct-onnx/5f5f794c

Step 6: Loading Model
───────────────────────────────────────────────────
✓ Model loaded: true

Step 7: Running Inference
───────────────────────────────────────────────────
✓ Chat client created

Prompt: What is 2+2?

Response:
─────────
2 + 2 equals 4.

Step 8: Verifying Model in Catalog
───────────────────────────────────────────────────
✓ Model found in cached models: true
✓ Model found in loaded models: true

Step 9: Cleanup
───────────────────────────────────────────────────
✓ Model unloaded

╔════════════════════════════════════════════════════════════════╗
║  ✓ E2E Test PASSED - All steps completed successfully!        ║
╚════════════════════════════════════════════════════════════════╝

Summary:
  • Model URI: microsoft/Phi-3-mini-4k-instruct-onnx
  • Model ID: microsoft/Phi-3-mini-4k-instruct-onnx:5f5f794c
  • Was cached: false
  • Now loaded: false

Key Achievement:
  The UNIFIED PATTERN allows HuggingFace models to use the same
  DownloadAsync → LoadAsync workflow as Azure catalog models!
```

## Key Features Tested

### ✅ Unified Pattern
The sample demonstrates that HuggingFace models follow the same pattern as Azure catalog models for download/load operations.

### ✅ Separation of Concerns
- **Step 3**: Add to catalog (fast, metadata only)
- **Step 5**: Download files (slow, with progress)

### ✅ Smart Download
- **Step 4**: Check if cached before downloading
- Only download if necessary

### ✅ Standard Infrastructure
- Uses existing `DownloadAsync()` method
- Progress callbacks work as expected
- Same error handling as Azure models

## Notes

- **Model Size**: ~2GB for this variant, download may take several minutes
- **Cache Location**: Models are cached to avoid re-downloading
- **Metadata**: `inference_model.json` is automatically generated
- **Compatibility**: Only ONNX Runtime GenAI models supported

## Troubleshooting

**Build Errors:**
- Ensure .NET 9.0 SDK is installed
- Check that Foundry Local SDK reference is correct

**Runtime Errors:**
- Ensure Foundry Local Core library is built and accessible
- Check network connection for HuggingFace API access
- Verify sufficient disk space for model download

**Model Not Compatible:**
- Ensure model contains `genai_config.json`
- Use models from onnxruntime organization for guaranteed compatibility
