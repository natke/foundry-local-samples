# HuggingFace Model Download - E2E Test Samples

This directory contains end-to-end test samples demonstrating the **unified download pattern** for HuggingFace models across all three SDK v2 implementations.

## Overview

Each sample demonstrates the complete workflow:

1. **Add model to catalog** (fast, metadata only)
2. **Check if cached** (conditional download)
3. **Download files** (using standard download method)
4. **Load model** (into memory)
5. **Run inference** (verify it works)
6. **Verify in catalog** (ensure model is registered)
7. **Cleanup** (unload model)

## Key Feature: Unified Pattern

All samples demonstrate that HuggingFace models use the **same download workflow** as Azure catalog models:

```
Azure Models:     catalog.GetModel() → model.DownloadAsync() → model.LoadAsync()
HuggingFace:      catalog.AddHuggingFaceModel() → model.DownloadAsync() → model.LoadAsync()
                                    ↑ Different        ↑ SAME!         ↑ SAME!
```

This consistency improves developer experience and reduces learning curve.

---

## Available Samples

### 📁 E2E_CSharp_HuggingFace

**Language:** C# (.NET 9.0)  
**Entry Point:** `Program.cs`  
**Test Model:** microsoft/Phi-3-mini-4k-instruct-onnx

**Run:**
```bash
cd E2E_CSharp_HuggingFace
dotnet build
dotnet run
```

**What it tests:**
- `ICatalog.AddHuggingFaceModelAsync()` API
- `ModelVariant.DownloadAsync()` with progress callbacks
- `ModelVariant.LoadAsync()` and inference
- Catalog verification methods

**Key Code:**
```csharp
var model = await catalog.AddHuggingFaceModelAsync(
    "microsoft/Phi-3-mini-4k-instruct-onnx",
    subdirectoryPath: "cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4"
);

if (!await model.IsCachedAsync())
    await model.DownloadAsync(p => Console.WriteLine($"{p}%"));

await model.LoadAsync();
```

---

### 📁 E2E_Python_HuggingFace

**Language:** Python 3.9+  
**Entry Point:** `test_huggingface_e2e.py`  
**Test Model:** microsoft/Phi-3-mini-4k-instruct-onnx

**Run:**
```bash
cd E2E_Python_HuggingFace
python3 test_huggingface_e2e.py
```

**What it tests:**
- `Catalog.add_huggingface_model()` API
- `model.download()` with progress callbacks
- `model.load()` and inference
- Catalog verification methods

**Key Code:**
```python
model = catalog.add_huggingface_model(
    "microsoft/Phi-3-mini-4k-instruct-onnx",
    subdirectory_path="cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4"
)

if not model.is_cached:
    model.download(progress_callback=lambda p: print(f"{p}%"))

model.load()
```

---

### 📁 E2E_JavaScript_HuggingFace

**Language:** TypeScript/JavaScript (Node.js 18+)  
**Entry Point:** `test_huggingface_e2e.ts`  
**Test Model:** microsoft/Phi-3-mini-4k-instruct-onnx

**Run:**
```bash
cd E2E_JavaScript_HuggingFace
npm install
npm test
```

**What it tests:**
- `Catalog.addHuggingFaceModel()` API
- `model.download()` with progress callbacks
- `model.load()` and inference
- Catalog verification methods

**Key Code:**
```typescript
const model = await catalog.addHuggingFaceModel(
    "microsoft/Phi-3-mini-4k-instruct-onnx",
    "main",
    "cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4"
);

if (!model.isCached)
    model.download(p => console.log(`${p}%`));

await model.load();
```

---

## Prerequisites

### All Samples

1. **Foundry Local Core**: Must be built and accessible
2. **Model Cache Directory**: Defaults to `~/.foundry-local`
3. **Network Access**: Required for HuggingFace API and model download
4. **Disk Space**: ~2GB for the test model

### Per Language

**C#:**
- .NET 9.0 SDK
- NuGet packages will be restored automatically

**Python:**
- Python 3.9+
- Install SDK: `pip install -e ../../foundry-local-sdk/sdk_v2/python`

**JavaScript/TypeScript:**
- Node.js 18+
- Run `npm install` in the sample directory

---

## Test Model

All samples use the same test model for consistency:

**Model:** `microsoft/Phi-3-mini-4k-instruct-onnx`  
**Variant:** `cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4`  
**Size:** ~2GB  
**Format:** ONNX Runtime GenAI  
**License:** MIT

This is a small, fast variant suitable for testing on most machines.

---

## Running All Tests

To run all three E2E tests sequentially:

```bash
# C# test
cd samples/E2E_CSharp_HuggingFace
dotnet run
cd ../..

# Python test
cd samples/E2E_Python_HuggingFace
python3 test_huggingface_e2e.py
cd ../..

# TypeScript test
cd samples/E2E_JavaScript_HuggingFace
npm install
npm test
cd ../..
```

---

## Expected Behavior

### First Run (Model Not Cached)

1. Add operation completes quickly (~1-2 seconds)
2. Cache check returns `false`
3. Download operation runs with progress updates (~5-10 minutes)
4. Model loads successfully
5. Inference produces a response
6. All verification checks pass

### Subsequent Runs (Model Cached)

1. Add operation completes quickly
2. Cache check returns `true`
3. Download step is **skipped**
4. Model loads from cache (fast)
5. Inference produces a response
6. All verification checks pass

---

## Success Criteria

Each test should complete with:

✅ Model added to catalog successfully  
✅ Download completed (or skipped if cached)  
✅ Model loaded into memory  
✅ Inference produces valid response  
✅ Model appears in cached models list  
✅ Model appears in loaded models list  
✅ Cleanup completes without errors

---

## Troubleshooting

### Common Issues

**1. Core Library Not Found**
- Ensure Foundry Local Core is built
- Check library path configuration
- Verify platform-specific extension (.dylib, .dll, .so)

**2. Authentication Errors**
- Tests use public models (no token needed)
- For private models, set `HF_TOKEN` environment variable

**3. Download Failures**
- Check network connectivity
- Verify HuggingFace API is accessible
- Ensure sufficient disk space

**4. Model Not Compatible**
- Test model contains `genai_config.json` (required)
- All onnxruntime organization models are compatible

### Debug Mode

Enable verbose logging by setting environment variables:

```bash
# C#
export FOUNDRY_LOCAL_LOG_LEVEL=Debug

# Python
export FOUNDRY_LOCAL_LOG_LEVEL=DEBUG

# TypeScript
export LOG_LEVEL=debug
```

---

## Validation Points

These tests validate:

### ✅ API Consistency
All three SDKs use the same pattern:
- Add → Download → Load → Use

### ✅ Separation of Concerns
- Adding to catalog is fast (metadata only)
- Downloading is separate (with progress)

### ✅ Cache Intelligence
- Check cached status before downloading
- Skip download if already cached

### ✅ Progress Reporting
- Download progress callbacks work correctly
- Progress values range from 0-100

### ✅ Error Handling
- Validation errors caught early
- Network errors handled gracefully
- Cleanup happens even on failure

---

## Performance Benchmarks

Typical execution times on standard hardware:

| Operation | Time | Network | Disk |
|-----------|------|---------|------|
| Add to catalog | 1-2 sec | ~10 KB | ~1 KB |
| Download (uncached) | 5-10 min | ~2 GB | ~2 GB |
| Download (cached) | 0 sec | 0 | 0 |
| Load model | 5-10 sec | 0 | 0 |
| Inference | 1-2 sec | 0 | 0 |

---

## Integration with CI/CD

These samples can be used in CI/CD pipelines:

```yaml
# GitHub Actions example
- name: Run C# E2E Test
  run: |
    cd samples/E2E_CSharp_HuggingFace
    dotnet run
    
- name: Run Python E2E Test
  run: |
    cd samples/E2E_Python_HuggingFace
    python3 test_huggingface_e2e.py
    
- name: Run TypeScript E2E Test
  run: |
    cd samples/E2E_JavaScript_HuggingFace
    npm install
    npm test
```

**Note:** First run will download the model. Subsequent runs will use cached model.

---

## Related Documentation

- [SDK v2 API Reference](../../docs/SDK_v2_HuggingFace_API_Reference.md)
- [Unified Download Design](../../docs/SDK_v2_Unified_Download_Design.md)
- [Implementation Summary](../../docs/SDK_v2_Unified_API_Implementation_Summary.md)
- [Quick Reference](../../docs/SDK_v2_HuggingFace_QuickReference.md)

---

## Support

For issues or questions:
- Review sample code and comments
- Check README files in each sample directory
- Review API reference documentation
- File GitHub issues
