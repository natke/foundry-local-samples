//! End-to-end sample demonstrating the HuggingFace three-step flow in the Rust SDK.
//!
//! This sample demonstrates:
//! 1. `add_catalog("https://huggingface.co")` — create a HuggingFace catalog
//! 2. `register_model("org/repo")` — download config files only (~50KB)
//! 3. `model.download()` — download ONNX files for inference
//!
//! This is the recommended pattern for HuggingFace models, matching the C# SDK.

use std::process::ExitCode;

use foundry_local_sdk::{
    ChatCompletionRequestMessage, ChatCompletionRequestSystemMessage,
    ChatCompletionRequestUserMessage, DeviceType, FoundryLocalConfig, FoundryLocalManager,
};

fn print_step(step: &str, title: &str) {
    println!("Step {step}: {title}");
    println!("{}", "\u{2500}".repeat(51));
}

#[tokio::main]
async fn main() -> ExitCode {
    println!("{}", "\u{2554}\u{2550}".to_owned() + &"\u{2550}".repeat(65) + "\u{2557}");
    println!("\u{2551}  Rust SDK - HuggingFace Three-Step Flow E2E Test                \u{2551}");
    println!("{}", "\u{255a}\u{2550}".to_owned() + &"\u{2550}".repeat(65) + "\u{255d}");
    println!();

    match run().await {
        Ok(()) => {
            println!();
            println!("{}", "\u{2554}\u{2550}".to_owned() + &"\u{2550}".repeat(65) + "\u{2557}");
            println!("\u{2551}  E2E Test PASSED - All steps completed successfully!          \u{2551}");
            println!("{}", "\u{255a}\u{2550}".to_owned() + &"\u{2550}".repeat(65) + "\u{255d}");
            ExitCode::SUCCESS
        }
        Err(e) => {
            println!();
            println!("{}", "\u{2554}\u{2550}".to_owned() + &"\u{2550}".repeat(65) + "\u{2557}");
            println!("\u{2551}  E2E Test FAILED                                               \u{2551}");
            println!("{}", "\u{255a}\u{2550}".to_owned() + &"\u{2550}".repeat(65) + "\u{255d}");
            println!();
            println!("Error: {e}");
            ExitCode::FAILURE
        }
    }
}

async fn run() -> Result<(), Box<dyn std::error::Error>> {
    // ══════════════════════════════════════════════════════════════════
    // STEP 1: Initialize Foundry Local Manager
    // ══════════════════════════════════════════════════════════════════
    print_step("1", "Initializing Foundry Local Manager");

    let mut config = FoundryLocalConfig::new("E2E_Rust_HuggingFace");

    // Use locally-built Core library if FOUNDRY_CORE_LIB is set
    if let Ok(core_lib) = std::env::var("FOUNDRY_CORE_LIB") {
        println!("  Using local Core library: {core_lib}");
        config = config.library_path(core_lib);
    }

    let manager = FoundryLocalManager::create(config)?;
    println!("  Azure Catalog: {}", manager.catalog().name());
    println!();

    // ══════════════════════════════════════════════════════════════════
    // STEP 2: Create HuggingFace Catalog
    // ══════════════════════════════════════════════════════════════════
    print_step("2", "Creating HuggingFace Catalog");

    let hf_catalog = manager
        .add_catalog("https://huggingface.co", None)
        .await?;
    println!("  HuggingFace Catalog: {}", hf_catalog.name());
    println!();

    // ══════════════════════════════════════════════════════════════════
    // STEP 3: Register Phi-3 Model (config files only, ~50KB)
    // ══════════════════════════════════════════════════════════════════
    let phi_identifier = "microsoft/Phi-3-mini-4k-instruct-onnx/cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4";

    print_step("3", "Registering HuggingFace Model: Phi-3");
    println!("  Identifier: {phi_identifier}");
    println!("  (Downloads config files only — fast!)");
    println!();

    let phi_model = hf_catalog
        .register_model(phi_identifier)
        .await?;
    println!("  Registered: {}", phi_model.id());
    println!("  Alias: {}", phi_model.alias());
    println!();

    // ══════════════════════════════════════════════════════════════════
    // STEP 4: Download Phi-3 ONNX Files
    // ══════════════════════════════════════════════════════════════════
    print_step("4", "Downloading Phi-3 ONNX Files");
    println!("  (Large files — this will take time if not cached)");
    println!();

    phi_model.download::<fn(&str)>(None).await?;
    println!("  Download complete");
    println!();

    // ══════════════════════════════════════════════════════════════════
    // STEP 5: Verify Model Lookup
    // ══════════════════════════════════════════════════════════════════
    print_step("5", "Looking Up Registered Model");

    // Lookup by alias
    let found_by_alias = hf_catalog
        .get_model(phi_model.alias())
        .await?;
    println!("  Found by alias '{}': {}", phi_model.alias(), found_by_alias.id());

    // Lookup by identifier
    let found_by_id = hf_catalog
        .get_model(phi_identifier)
        .await?;
    println!("  Found by identifier: {}", found_by_id.id());

    // List all registered models
    let all_models = hf_catalog.get_models().await?;
    println!("  Total registered models: {}", all_models.len());
    println!();

    // ══════════════════════════════════════════════════════════════════
    // STEP 6: Register and Download Gemma 3 (subpath model)
    // ══════════════════════════════════════════════════════════════════
    let gemma_url = "https://huggingface.co/onnxruntime/Gemma-3-ONNX/tree/main/gemma-3-4b-it/cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4";

    print_step("6", "Register + Download Gemma 3 (Subpath Model)");
    println!("  URL: {gemma_url}");
    println!();

    let gemma_model = hf_catalog
        .register_model(gemma_url)
        .await?;
    println!("  Registered: {}", gemma_model.id());
    println!("  Alias: {}", gemma_model.alias());

    gemma_model.download::<fn(&str)>(None).await?;
    println!("  Download complete");
    println!();

    // ══════════════════════════════════════════════════════════════════
    // STEP 7: Load and Run Inference with Phi-3
    // ══════════════════════════════════════════════════════════════════
    print_step("7", "Running Inference with Phi-3");

    phi_model.load().await?;
    println!("  Model loaded: {}", phi_model.alias());
    println!();

    run_inference(
        &phi_model,
        "You are a helpful math assistant.",
        "What is 2+2? Answer in one short sentence.",
    )
    .await?;

    phi_model.unload().await?;
    println!("  Model unloaded");
    println!();

    // ══════════════════════════════════════════════════════════════════
    // STEP 8: Load and Run Inference with Gemma 3
    // ══════════════════════════════════════════════════════════════════
    print_step("8", "Running Inference with Gemma 3");

    gemma_model.load().await?;
    println!("  Model loaded: {}", gemma_model.alias());
    println!();

    run_inference(
        &gemma_model,
        "You are a helpful science assistant.",
        "What is the largest planet in our solar system? Answer in one short sentence.",
    )
    .await?;

    gemma_model.unload().await?;
    println!("  Model unloaded");
    println!();

    // ══════════════════════════════════════════════════════════════════
    // STEP 9: Azure Catalog — List Available Models
    // ══════════════════════════════════════════════════════════════════
    print_step("9", "Azure Catalog — List Available Models");

    let azure_catalog = manager.catalog();
    let azure_models = azure_catalog.get_models().await?;
    println!("  Available Azure models: {}", azure_models.len());
    for m in azure_models.iter().take(5) {
        println!("    {} ({})", m.alias(), m.id());
    }
    if azure_models.len() > 5 {
        println!("    ... and {} more", azure_models.len() - 5);
    }
    println!();

    // ══════════════════════════════════════════════════════════════════
    // STEP 10: Azure Catalog — Get, Download, Load, Inference, Unload
    // ══════════════════════════════════════════════════════════════════
    let azure_alias = "qwen2.5-1.5b";

    print_step("10", "Azure Catalog — Model Lifecycle");
    println!("  Azure Model Alias: {azure_alias}");
    println!();

    let azure_model = azure_catalog
        .get_model(azure_alias)
        .await?
        .ok_or("Azure catalog model not found")?;
    println!("  Azure model retrieved: {}", azure_model.id());
    println!("  Alias: {}", azure_model.alias());
    println!("  Variants:");
    for v in azure_model.variants() {
        let device = v.info().runtime.as_ref().map_or("unknown", |r| match r.device_type {
            DeviceType::CPU => "CPU",
            DeviceType::GPU => "GPU",
            DeviceType::NPU => "NPU",
            _ => "unknown",
        });
        println!("    {} — {} (cached: {})", v.id(), device, v.info().cached);
    }
    println!();

    // Select a CPU variant (matching C# ModelManagementExample pattern)
    if let Some(cpu_variant) = azure_model.variants().iter().find(|v| {
        v.info()
            .runtime
            .as_ref()
            .map_or(false, |r| r.device_type == DeviceType::CPU)
    }) {
        azure_model.select_variant(cpu_variant.id())?;
        println!("  Selected CPU variant: {}", cpu_variant.id());
    } else {
        println!("  No CPU variant found, using default: {}", azure_model.id());
    }
    println!();

    println!("  Downloading Azure model (if not cached)...");
    azure_model.download::<fn(&str)>(None).await?;
    println!("  Azure model downloaded");
    println!();

    println!("  Loading Azure model...");
    azure_model.load().await?;
    let is_loaded = azure_model.is_loaded().await?;
    println!("  Azure model loaded: {is_loaded}");
    println!();

    run_inference(
        &azure_model,
        "You are a helpful geography assistant.",
        "What is the capital of France? Answer in one short sentence.",
    )
    .await?;

    // List loaded models while the Azure model is still loaded
    let loaded_models = azure_catalog.get_loaded_models().await?;
    println!("  Currently loaded Azure models: {}", loaded_models.len());
    for v in &loaded_models {
        println!("    {} ({})", v.alias(), v.id());
    }
    println!();

    azure_model.unload().await?;
    println!("  Azure model unloaded");
    println!();

    // ══════════════════════════════════════════════════════════════════
    // STEP 11: List All Cached Models (Both Catalogs)
    // ══════════════════════════════════════════════════════════════════
    print_step("11", "Listing All Cached Models");

    let azure_cached = azure_catalog.get_cached_models().await?;
    println!("  Azure cached models: {}", azure_cached.len());
    for v in &azure_cached {
        println!("    {} ({})", v.alias(), v.id());
    }
    println!();

    let hf_cached = hf_catalog.get_cached_models().await?;
    println!("  HuggingFace cached models: {}", hf_cached.len());
    for v in &hf_cached {
        println!("    {} ({})", v.alias(), v.id());
    }
    println!();

    // ══════════════════════════════════════════════════════════════════
    // STEP 12: HuggingFace Gated Model without genai_config (Llama 3.2-1B)
    // ══════════════════════════════════════════════════════════════════
    print_step("12", "Gated Model without genai_config (Llama 3.2-1B)");
    println!("  This model is a gated PyTorch model without ONNX files.");
    println!("  Registration should fail because it has no genai_config.json.");
    println!();

    let hf_token = std::env::var("HF_TOKEN").ok();
    if hf_token.is_none() {
        println!("  HF_TOKEN not set, skipping gated model test");
    } else {
        println!("  HF_TOKEN found");
        println!();

        // Add catalog with token
        let hf_catalog_with_token = manager
            .add_catalog("https://huggingface.co", hf_token.clone())
            .await?;
        println!("  HuggingFace Catalog (with token): {}", hf_catalog_with_token.name());
        println!();

        let llama_identifier = "meta-llama/Llama-3.2-1B/tree/main";
        println!("  Attempting to register gated model: {llama_identifier}...");
        println!("  (Expected: failure due to missing genai_config.json)");
        match hf_catalog_with_token.register_model(llama_identifier).await {
            Ok(model) => {
                println!("  Registration unexpectedly succeeded: {}", model.alias());
                println!("  This model has no ONNX files - download/inference would fail.");
            }
            Err(e) => {
                println!("  Registration correctly failed: {e}");
            }
        }
    }
    println!();

    // ══════════════════════════════════════════════════════════════════
    // STEP 13: Private HuggingFace Model with Token (Qwen3 0.6B ONNX)
    // ══════════════════════════════════════════════════════════════════
    print_step("13", "Private Model with Token (Qwen3 0.6B ONNX)");
    println!();

    if hf_token.is_none() {
        println!("  HF_TOKEN not set, skipping private model test");
    } else {
        let hf_catalog_with_token = manager
            .add_catalog("https://huggingface.co", hf_token)
            .await?;
        println!("  Using HuggingFace Catalog with token: {}", hf_catalog_with_token.name());
        println!();

        // Register private ONNX model
        let qwen_identifier = "natke/qwen3_0.6b_dq4fp16_4_4";
        println!("  Registering private model: {qwen_identifier}...");
        let qwen_model = hf_catalog_with_token
            .register_model(qwen_identifier)
            .await?;
        println!("  Qwen3 0.6B: {} registered", qwen_model.alias());
        println!();

        // Download ONNX files
        println!("  Downloading ONNX files for Qwen3 0.6B...");
        qwen_model.download::<fn(&str)>(None).await?;
        println!("  Qwen3 0.6B downloaded: {}", qwen_model.id());
        println!();

        // Run inference
        println!("  Running inference with Qwen3 0.6B...");
        qwen_model.load().await?;
        println!("  Model loaded: {}", qwen_model.alias());
        println!();

        run_inference(
            &qwen_model,
            "You are a helpful physics assistant.",
            "What is the speed of light? Answer in one short sentence.",
        )
        .await?;

        qwen_model.unload().await?;
        println!("  Qwen3 0.6B model unloaded");
    }
    println!();

    // ══════════════════════════════════════════════════════════════════
    // Summary
    // ══════════════════════════════════════════════════════════════════
    println!("  What was tested:");
    println!("     - add_catalog(\"https://huggingface.co\") creates HuggingFace catalog");
    println!("     - register_model() downloads config files only (fast)");
    println!("     - model.download() downloads ONNX files");
    println!("     - Model lookup by alias and identifier");
    println!("     - Subpath model (Gemma 3) register + download");
    println!("     - Inference with Phi-3 and Gemma 3");
    println!("     - Azure catalog: list available models");
    println!("     - Azure catalog: get model, download, load, inference, unload");
    println!("     - Azure catalog: list loaded and cached models");
    println!("     - HuggingFace catalog: list cached models");
    println!("     - Gated model rejection (Llama 3.2-1B, no genai_config)");
    println!("     - Private model with token (Qwen3 0.6B, if HF_TOKEN set)");

    Ok(())
}

async fn run_inference(
    model: &foundry_local_sdk::Model,
    system_prompt: &str,
    user_prompt: &str,
) -> Result<(), Box<dyn std::error::Error>> {
    let client = model.create_chat_client().temperature(0.7).max_tokens(256);

    println!("  Prompt: {user_prompt}");
    println!();

    let messages: Vec<ChatCompletionRequestMessage> = vec![
        ChatCompletionRequestSystemMessage::from(system_prompt).into(),
        ChatCompletionRequestUserMessage::from(user_prompt).into(),
    ];

    let response = client.complete_chat(&messages, None).await?;
    println!("  Response:");
    println!("  {}", "\u{2500}".repeat(9));
    if let Some(choice) = response.choices.first() {
        if let Some(ref content) = choice.message.content {
            println!("  {content}");
        }
    }
    println!();

    Ok(())
}
