# Model & API Setup

This .NET 10 / DeepSeek edition uses DeepSeek V4 Flash for all chat examples via the OpenAI .NET SDK. Only Chapter 15 (tool routing with embeddings) requires a local embedding GGUF model.

## 1. DeepSeek API key

1. Create an account at https://platform.deepseek.com.
2. Generate an API key.
3. Copy the secrets example file in each chapter project to `appsettings.Secrets.json`:
   ```bash
   cp src/Chapter01/appsettings.Secrets.example.json src/Chapter01/appsettings.Secrets.json
   ```
4. Paste your key inside each `appsettings.Secrets.json`:
   ```json
   {
     "DeepSeek": {
       "ApiKey": "sk-..."
     }
   }
   ```

You can automate the copy step for all chapters with PowerShell:
```powershell
Get-ChildItem src/Chapter* | ForEach-Object {
    Copy-Item "$($_.FullName)/appsettings.Secrets.example.json" "$($_.FullName)/appsettings.Secrets.json" -Force
}
```

## 2. (Optional) Local embedding model for Chapter 15

Chapter 15 demonstrates tool routing with local embeddings. The example expects an embedding-only GGUF model at:

```text
models/bge-small-en-v1.5-q8_0.gguf
```

You can download it from Hugging Face using `git lfs`, a browser, or any GGUF source. A good starting point is:

- **CompendiumLabs/bge-small-en-v1.5-gguf** on Hugging Face

Place the downloaded `.gguf` file under `models/` before running `src/Chapter15`.

If the model is missing, Chapter 15 prints a helpful message and exits cleanly.

## 3. Notes

- DeepSeek V4 Flash is configured in every chapter's `appsettings.json` as `deepseek-v4-flash`.
- The chat base URL is `https://api.deepseek.com`.
- You can swap the chat model by changing `DeepSeek:ChatModel` in `appsettings.json`.
- You can swap the embedding model by changing `Embeddings:ModelPath` in `src/Chapter15/appsettings.json`.
