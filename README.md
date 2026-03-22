dotnet build
dotnet run

# HuggingFace Model Download - Samples

This folder contains **end-to-end samples** demonstrating the unified HuggingFace model download pattern across multiple SDKs:

- **C# (.NET 9.0)**
- **JavaScript/TypeScript (Node.js 18+)**
- **Rust**
- **HuggingFaceDownloadSample** (C#) — full-featured download and metadata demo

All samples show how to:
1. Add a HuggingFace model to the catalog (fast, metadata only)
2. Check if the model is cached
3. Download model files (ONNX, config, etc.)
4. Load the model into memory
5. Run inference
6. Verify model in catalog

---

## 📁 Sample Projects

- **E2E_CSharp_HuggingFace/** — C# end-to-end test, see [README](E2E_CSharp_HuggingFace/README.md)
- **E2E_JavaScript_HuggingFace/** — TypeScript/JavaScript end-to-end test, see [README](E2E_JavaScript_HuggingFace/README.md)
- **E2E_Rust_HuggingFace/** — Rust end-to-end test (see `src/main.rs`)
- **HuggingFaceDownloadSample/** — C# sample with advanced metadata, subdirectory, and integration features, see [README](HuggingFaceDownloadSample/README.md)

---

## 🏁 Quick Start

### C# E2E Sample
```bash
cd samples/E2E_CSharp_HuggingFace
dotnet build
dotnet run
```

### JavaScript/TypeScript E2E Sample
```bash
cd samples/E2E_JavaScript_HuggingFace
npm install
npm test
```

### Rust E2E Sample
```bash
cd samples/E2E_Rust_HuggingFace
cargo run
```

### HuggingFaceDownloadSample (C#)
```bash
cd samples/HuggingFaceDownloadSample
dotnet build
dotnet run
```

---

## Prerequisites

- **Foundry Local Core** must be built and available
- **Model cache directory**: defaults to `~/.foundry-local`
- **Network access** for HuggingFace API and model download
- **Disk space**: ~2GB for the test model

See each sample's README for language-specific requirements.

---

## 🔎 More Info

- See [E2E_HuggingFace_README.md](E2E_HuggingFace_README.md) for a cross-language overview and test model details.
- Each sample folder contains a README with usage and expected output.
- For advanced download and metadata features, see [HuggingFaceDownloadSample/README.md](HuggingFaceDownloadSample/README.md) and [INSTRUCTIONS.md](HuggingFaceDownloadSample/INSTRUCTIONS.md).
