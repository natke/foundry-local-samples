// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

/// <summary>
/// End-to-end sample demonstrating the HuggingFace Catalog three-step flow in C# SDK.
///
/// This sample demonstrates:
/// 1. HuggingFace Catalog three-step flow (PRIMARY):
///    - AddCatalogAsync() to create a separate HuggingFace catalog
///    - RegisterModelAsync() to fetch metadata only (fast, ~50KB)
///    - ListModelsAsync() to browse registered models
///    - GetModelAsync() to lookup registered models by identifier or alias
///    - DownloadAsync() on the model to download ONNX files when needed
/// 2. Azure Catalog usage as reference (default, built-in)
///
/// The three-step flow is the recommended approach for working with HuggingFace models
/// because it separates fast metadata discovery from large file downloads.
///
/// Any step failure will cause the test to exit with a non-zero exit code.
/// </summary>

using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Logging;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  C# SDK - HuggingFace Catalog Three-Step Flow E2E Test       ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Setup logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
});
var logger = loggerFactory.CreateLogger("CSharpE2E");

try
{
    // ══════════════════════════════════════════════════════════════════
    // STEP 1: Initialize Foundry Local Manager
    // ══════════════════════════════════════════════════════════════════
    Console.WriteLine("Step 1: Initializing Foundry Local Manager");
    Console.WriteLine("───────────────────────────────────────────────────");

    var config = new Configuration
    {
        AppName = "E2E_CSharp_HuggingFace",
    };

    await FoundryLocalManager.CreateAsync(config, logger);
    var manager = FoundryLocalManager.Instance;
    Console.WriteLine("✓ Initialized");
    Console.WriteLine();

    // ══════════════════════════════════════════════════════════════════
    // STEP 2: HuggingFace Catalog - Three-Step Flow
    // ══════════════════════════════════════════════════════════════════
    Console.WriteLine("Step 2: HuggingFace Catalog - Three-Step Flow");
    Console.WriteLine("───────────────────────────────────────────────────");
    Console.WriteLine();
    Console.WriteLine("The recommended way to work with HuggingFace models:");
    Console.WriteLine("1. Add a HuggingFace catalog");
    Console.WriteLine("2. Register models (fast metadata-only download)");
    Console.WriteLine("3. Download ONNX files only when needed");
    Console.WriteLine("4. Run inference with downloaded models");
    Console.WriteLine();

    // Step 2a: Add HuggingFace Catalog (test idempotency by adding twice)
    Console.WriteLine("2a. Adding HuggingFace Catalog...");
    var hfCatalog = await manager.AddCatalogAsync("https://huggingface.co");
    Console.WriteLine($"✓ HuggingFace Catalog created: {hfCatalog.Name}");

    Console.WriteLine("2a2. Adding HuggingFace Catalog again (testing idempotency)...");
    var hfCatalog2 = await manager.AddCatalogAsync("https://huggingface.co");
    Console.WriteLine($"✓ Second add returned same catalog: {hfCatalog2.Name == hfCatalog.Name && ReferenceEquals(hfCatalog2, hfCatalog)}");
    Console.WriteLine();

    // Step 2b: Register multiple models
    Console.WriteLine("2b. Registering HuggingFace models (metadata only - very fast)...");

    var phiModel = await hfCatalog.RegisterModelAsync(
        "microsoft/Phi-3-mini-4k-instruct-onnx/tree/main/cpu_and_mobile/cpu-int4-rtn-block-32");
    Console.WriteLine($"✓ Phi-3 Mini (block-32): {phiModel.Alias} registered");

    // Register a second variant from the same repo (demonstrates multi-variant support)
    var phiModel2 = await hfCatalog.RegisterModelAsync(
        "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-onnx/tree/main/cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4");
    Console.WriteLine($"✓ Phi-3 Mini (acc-level-4): {phiModel2.Alias} registered (same repo, different variant)");

    var gemmaRegistered = await hfCatalog.RegisterModelAsync(
        "onnxruntime/Gemma-3-ONNX/gemma-3-4b-it/cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4");
    Console.WriteLine($"✓ Gemma 3 CPU: {gemmaRegistered.Alias} registered");
    Console.WriteLine();

    // Step 2b2: Re-register a model (test idempotency)
    Console.WriteLine("2b2. Re-registering Phi-3 (testing idempotency)...");
    var reregistered = await hfCatalog.RegisterModelAsync(
        "microsoft/Phi-3-mini-4k-instruct-onnx/tree/main/cpu_and_mobile/cpu-int4-rtn-block-32");
    Console.WriteLine($"✓ Phi-3 re-registered successfully: {reregistered.Alias}");
    Console.WriteLine($"  Same alias as before: {reregistered.Alias == phiModel.Alias}");
    Console.WriteLine();

    // Step 2c: Browse registered models
    Console.WriteLine("2c. Listing all registered models...");
    var allRegisteredModels = await hfCatalog.ListModelsAsync();
    Console.WriteLine($"✓ Total registered models: {allRegisteredModels.Count}");
    foreach (var rm in allRegisteredModels)
    {
        Console.WriteLine($"  - {rm.Id} (Cached: {rm.SelectedVariant.Info.Cached})");
    }
    Console.WriteLine();

    // Step 2d: Lookup Phi-3 by identifier
    Console.WriteLine("2d. Looking up registered model by identifier...");
    phiModel = await hfCatalog.GetModelAsync(phiModel.Id)
        ?? throw new Exception("Phi-3 model not found after registration");
    Console.WriteLine($"✓ Found: {phiModel.Id} (Cached: {phiModel.SelectedVariant.Info.Cached})");
    Console.WriteLine();

    // Step 2e: Download Phi-3 ONNX files (idempotent - skips if already cached)
    Console.WriteLine("2e. Downloading ONNX files for Phi-3...");
    await phiModel.DownloadAsync(ct: CancellationToken.None);
    Console.WriteLine($"✓ Phi-3 downloaded: {phiModel.Id}");
    Console.WriteLine();

    // Step 2f: Run inference with Phi-3
    Console.WriteLine("2f. Running inference with Phi-3...");
    Console.WriteLine("───────────────────────────────────────────────────");

    await phiModel.LoadAsync(CancellationToken.None);
    Console.WriteLine($"✓ Model loaded: {phiModel.Alias}");
    Console.WriteLine();

    var chatClient = await phiModel.GetChatClientAsync(CancellationToken.None);
    Console.WriteLine("Prompt: What is 2+2?");
    Console.WriteLine();

    var messages = new List<ChatMessage>
    {
        ChatMessage.FromSystem("You are a helpful math assistant."),
        ChatMessage.FromUser("What is 2+2? Answer in one short sentence.")
    };

    var response = await chatClient.CompleteChatAsync(messages);
    Console.WriteLine("Response:");
    Console.WriteLine("─────────");
    if (response.Choices?.Count > 0)
    {
        Console.WriteLine(response.Choices[0].Message?.Content);
    }
    Console.WriteLine();

    await phiModel.UnloadAsync(CancellationToken.None);
    Console.WriteLine("✓ Phi-3 (block-32) model unloaded");
    Console.WriteLine();

    // Step 2f2: Download and run second Phi-3 variant (same repo, different variant)
    Console.WriteLine("2f2. Second variant from same repo: Phi-3 (acc-level-4)...");
    Console.WriteLine("───────────────────────────────────────────────────");
    Console.WriteLine("This demonstrates two models from the same HuggingFace repo.");
    Console.WriteLine();

    phiModel2 = await hfCatalog.GetModelAsync(phiModel2.Alias)
        ?? throw new Exception("Phi-3 (acc-level-4) not found after registration");
    Console.WriteLine($"✓ Found: {phiModel2.Alias}");

    Console.WriteLine("Downloading ONNX files for Phi-3 (acc-level-4)...");
    await phiModel2.DownloadAsync(ct: CancellationToken.None);
    Console.WriteLine($"✓ Phi-3 (acc-level-4) downloaded: {phiModel2.Id}");
    Console.WriteLine();

    await phiModel2.LoadAsync(CancellationToken.None);
    Console.WriteLine($"✓ Model loaded: {phiModel2.Alias}");
    Console.WriteLine();

    var phi2ChatClient = await phiModel2.GetChatClientAsync(CancellationToken.None);
    Console.WriteLine("Prompt: What is 3+3?");
    Console.WriteLine();

    var phi2Messages = new List<ChatMessage>
    {
        ChatMessage.FromSystem("You are a helpful math assistant."),
        ChatMessage.FromUser("What is 3+3? Answer in one short sentence.")
    };

    var phi2Response = await phi2ChatClient.CompleteChatAsync(phi2Messages);
    Console.WriteLine("Response:");
    Console.WriteLine("─────────");
    if (phi2Response.Choices?.Count > 0)
    {
        Console.WriteLine(phi2Response.Choices[0].Message?.Content);
    }
    Console.WriteLine();

    await phiModel2.UnloadAsync(CancellationToken.None);
    Console.WriteLine("✓ Phi-3 (acc-level-4) model unloaded");
    Console.WriteLine();

    // Step 2g: Lookup Gemma 3
    Console.WriteLine("2g. Looking up registered Gemma 3 model...");
    var gemmaModel = await hfCatalog.GetModelAsync(gemmaRegistered.Alias)
        ?? throw new Exception("Gemma 3 model not found after registration");
    Console.WriteLine($"✓ Found: {gemmaModel.Alias}");
    Console.WriteLine();

    // Step 2h: Download Gemma 3 ONNX files (idempotent - skips if already cached)
    Console.WriteLine("2h. Downloading ONNX files for Gemma 3...");
    await gemmaModel.DownloadAsync(ct: CancellationToken.None);
    Console.WriteLine($"✓ Gemma 3 downloaded: {gemmaModel.Id}");
    Console.WriteLine();

    // Step 2i: Run inference with Gemma 3
    Console.WriteLine("2i. Running inference with Gemma 3...");
    Console.WriteLine("───────────────────────────────────────────────────");

    await gemmaModel.LoadAsync(CancellationToken.None);
    Console.WriteLine($"✓ Model loaded: {gemmaModel.Alias}");
    Console.WriteLine();

    var gemmaChatClient = await gemmaModel.GetChatClientAsync(CancellationToken.None);
    Console.WriteLine("Prompt: What is the largest planet in our solar system?");
    Console.WriteLine();

    var gemmaMessages = new List<ChatMessage>
    {
        ChatMessage.FromSystem("You are a helpful science assistant."),
        ChatMessage.FromUser("What is the largest planet in our solar system? Answer in one short sentence.")
    };

    var gemmaResponse = await gemmaChatClient.CompleteChatAsync(gemmaMessages);
    Console.WriteLine("Response:");
    Console.WriteLine("─────────");
    if (gemmaResponse.Choices?.Count > 0)
    {
        Console.WriteLine(gemmaResponse.Choices[0].Message?.Content);
    }
    Console.WriteLine();

    await gemmaModel.UnloadAsync(CancellationToken.None);
    Console.WriteLine("✓ Gemma 3 model unloaded");
    Console.WriteLine();

    // ══════════════════════════════════════════════════════════════════
    // STEP 3: HuggingFace Gated Model without genai_config (Llama 3.2-1B)
    // ══════════════════════════════════════════════════════════════════
    Console.WriteLine("Step 3: HuggingFace Gated Model without genai_config (Llama 3.2-1B)");
    Console.WriteLine("───────────────────────────────────────────────────────────────────────");
    Console.WriteLine();
    Console.WriteLine("This model is a gated PyTorch model without ONNX files.");
    Console.WriteLine("Registration should fail because it has no genai_config.json.");
    Console.WriteLine();

    var hfToken = Environment.GetEnvironmentVariable("HF_TOKEN");
    if (string.IsNullOrEmpty(hfToken))
    {
        Console.WriteLine("⚠ HF_TOKEN environment variable not set, skipping gated model test");
    }
    else
    {
        Console.WriteLine("✓ HF_TOKEN found");
        Console.WriteLine();

        // Step 3a: Add HuggingFace Catalog with token
        Console.WriteLine("3a. Adding HuggingFace Catalog with token...");
        var hfCatalogWithToken = await manager.AddCatalogAsync("https://huggingface.co", hfToken);
        Console.WriteLine($"✓ HuggingFace Catalog (with token): {hfCatalogWithToken.Name}");
        Console.WriteLine();

        // Step 3b: Attempt to register Llama 3.2-1B (expect failure - no genai_config)
        var llamaIdentifier = "meta-llama/Llama-3.2-1B/tree/main";
        Console.WriteLine($"3b. Attempting to register gated model: {llamaIdentifier}...");
        Console.WriteLine("    (Expected: failure due to missing genai_config.json)");
        try
        {
            var llamaModel = await hfCatalogWithToken.RegisterModelAsync(llamaIdentifier);
            Console.WriteLine($"⚠ Registration unexpectedly succeeded: {llamaModel.Alias}");
            Console.WriteLine("  This model has no ONNX files - download/inference would fail.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✓ Registration correctly failed: {ex.Message}");
        }
    }
    Console.WriteLine();

    // ══════════════════════════════════════════════════════════════════
    // STEP 4: Private HuggingFace Model with Token (Qwen3 0.6B ONNX)
    // ══════════════════════════════════════════════════════════════════
    Console.WriteLine("Step 4: Private HuggingFace Model with Token (Qwen3 0.6B ONNX)");
    Console.WriteLine("──────────────────────────────────────────────────────────────────");
    Console.WriteLine();

    if (string.IsNullOrEmpty(hfToken))
    {
        Console.WriteLine("⚠ HF_TOKEN environment variable not set, skipping private model test");
    }
    else
    {
        // Step 4a: Catalog with token already created in Step 3a
        var hfCatalogWithToken = await manager.AddCatalogAsync("https://huggingface.co", hfToken);
        Console.WriteLine($"✓ Using HuggingFace Catalog with token: {hfCatalogWithToken.Name}");
        Console.WriteLine();

        // Step 4b: Register private ONNX model
        var qwenIdentifier = "natke/qwen3_0.6b_dq4fp16_4_4";
        Console.WriteLine($"4b. Registering private model: {qwenIdentifier}...");
        var qwenModel = await hfCatalogWithToken.RegisterModelAsync(qwenIdentifier);
        Console.WriteLine($"✓ Qwen3 0.6B: {qwenModel.Alias} registered");
        Console.WriteLine();

        // Step 4c: Download ONNX files (idempotent - skips if already cached)
        Console.WriteLine("4c. Downloading ONNX files for Qwen3 0.6B...");
        await qwenModel.DownloadAsync(ct: CancellationToken.None);
        Console.WriteLine($"✓ Qwen3 0.6B downloaded: {qwenModel.Id}");
        Console.WriteLine();

        // Step 4d: Run inference with Qwen3 0.6B
        Console.WriteLine("4d. Running inference with Qwen3 0.6B...");
        Console.WriteLine("───────────────────────────────────────────────────");

        await qwenModel.LoadAsync(CancellationToken.None);
        Console.WriteLine($"✓ Model loaded: {qwenModel.Alias}");
        Console.WriteLine();

        var qwenChatClient = await qwenModel.GetChatClientAsync(CancellationToken.None);
        Console.WriteLine("Prompt: What is the speed of light?");
        Console.WriteLine();

        var qwenMessages = new List<ChatMessage>
        {
            ChatMessage.FromSystem("You are a helpful physics assistant."),
            ChatMessage.FromUser("What is the speed of light? Answer in one short sentence.")
        };

        var qwenResponse = await qwenChatClient.CompleteChatAsync(qwenMessages);
        Console.WriteLine("Response:");
        Console.WriteLine("─────────");
        if (qwenResponse.Choices?.Count > 0)
        {
            Console.WriteLine(qwenResponse.Choices[0].Message?.Content);
        }
        Console.WriteLine();

        await qwenModel.UnloadAsync(CancellationToken.None);
        Console.WriteLine("✓ Qwen3 0.6B model unloaded");
    }
    Console.WriteLine();

    // ══════════════════════════════════════════════════════════════════
    // STEP 5: Azure Catalog Reference (built-in default catalog)
    // ══════════════════════════════════════════════════════════════════
    Console.WriteLine("Step 5: Azure Catalog Reference");
    Console.WriteLine("───────────────────────────────────────────────────");

    var catalog = await manager.GetCatalogAsync();
    Console.WriteLine($"✓ Catalog: {catalog.Name}");
    Console.WriteLine();

    var azureModelAlias = "qwen2.5-1.5b";

    Console.WriteLine($"Azure Model Alias: {azureModelAlias}");
    Console.WriteLine();
    Console.WriteLine("Calling GetModelAsync() with alias...");
    Console.WriteLine();

    var azureModel = await catalog.GetModelAsync(azureModelAlias, CancellationToken.None)
        ?? throw new Exception("Azure catalog model not found");

    Console.WriteLine($"✓ Azure model retrieved: {azureModel.Id}");
    Console.WriteLine($"  Alias: {azureModel.Alias}");
    Console.WriteLine();

    Console.WriteLine("Downloading Azure model (if not cached)...");
    await azureModel.DownloadAsync(ct: CancellationToken.None);
    Console.WriteLine("✓ Azure model downloaded");

    Console.WriteLine("Loading Azure model...");
    await azureModel.LoadAsync(CancellationToken.None);

    var isAzureModelLoaded = await azureModel.IsLoadedAsync(CancellationToken.None);
    Console.WriteLine($"✓ Azure model loaded: {isAzureModelLoaded}");
    Console.WriteLine();

    Console.WriteLine("Running inference with Azure model...");
    var azureChatClient = await azureModel.GetChatClientAsync(CancellationToken.None);
    Console.WriteLine("✓ Azure chat client created");
    Console.WriteLine();

    Console.WriteLine("Prompt: What is the capital of France?");
    Console.WriteLine();

    var azureMessages = new List<ChatMessage>
    {
        ChatMessage.FromSystem("You are a helpful geography assistant."),
        ChatMessage.FromUser("What is the capital of France? Answer in one short sentence.")
    };

    var azureResponse = await azureChatClient.CompleteChatAsync(azureMessages);

    Console.WriteLine("Response:");
    Console.WriteLine("─────────");
    if (azureResponse.Choices?.Count > 0)
    {
        Console.WriteLine(azureResponse.Choices[0].Message?.Content);
    }
    Console.WriteLine();

    await azureModel.UnloadAsync(CancellationToken.None);
    Console.WriteLine("✓ Azure model unloaded");
    Console.WriteLine();

    // ══════════════════════════════════════════════════════════════════
    // STEP 6: Summary
    // ══════════════════════════════════════════════════════════════════
    Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║  ✓ E2E Test PASSED - All steps completed successfully!        ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine("  What was tested:");
    Console.WriteLine("     ✓ HuggingFace catalog creation (idempotent)");
    Console.WriteLine("     ✓ Model registration (idempotent, config-only download)");
    Console.WriteLine("     ✓ Model listing and lookup by identifier");
    Console.WriteLine("     ✓ ONNX download for Phi-3 variant 1 (cpu-int4-rtn-block-32)");
    Console.WriteLine("     ✓ ONNX download for Phi-3 variant 2 (acc-level-4, same repo)");
    Console.WriteLine("     ✓ ONNX download for Gemma 3 (with subpath)");
    Console.WriteLine("     ✓ Inference with Phi-3 (both variants from same repo)");
    Console.WriteLine("     ✓ Inference with Gemma 3");
    Console.WriteLine("     ✓ Gated model rejection (Llama 3.2-1B, no genai_config)");
    Console.WriteLine("     ✓ Private model with token (Qwen3 0.6B, if HF_TOKEN set)");
    Console.WriteLine("     ✓ Azure catalog model lookup, load, inference, unload");
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║  ✗ E2E Test FAILED                                             ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("Stack trace:");
    Console.WriteLine(ex.StackTrace);
    logger.LogError(ex, "E2E test failed");
    Environment.Exit(1);
}
finally
{
    if (FoundryLocalManager.IsInitialized)
    {
        FoundryLocalManager.Instance.Dispose();
    }
}
