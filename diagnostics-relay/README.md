# Private diagnostic relay — setup required

The diagnostic submission feature is included starting in application v1.1.1. The deployed service address is prefilled; a working relay and connection code are still required. Production submission is verified separately from mocked tests. The application button sends only the cleaned log and application version; it does not upload the proxy list, credential configuration, Windows machine name, or game files. Network destination addresses remain useful for diagnosis. Cleaning is best effort; keep the receiving repository private.

## One-time service setup

1. Create **niyazisugar/NiyazisugarProxy-Diagnostics** as a **private** GitHub repository with Issues enabled. Give the ChatGPT GitHub connection access to that repository so reports can be read later.
2. In a Cloudflare account, create the Worker from `worker.mjs`, or deploy this directory with Wrangler. Set `DIAGNOSTICS_REPO` to the private repository above. No paid plan or billing change is performed by this code.
3. Create a fine-grained GitHub token limited to that repository with **Issues: Read and write** and repository metadata access. Store it only as the Worker's `GITHUB_TOKEN` secret. Never paste it into chat, put it in the EXE, or commit it.
4. Generate a random connection code (at least 32 random bytes) and store it as the Worker's `INGEST_KEY` secret. This key permits diagnostic submission only, not access to GitHub. Keep it outside this public source repository.
5. The first click on the desktop app's diagnostic button asks for the HTTPS Worker URL ending in `/reports` and the connection code. These settings are stored under LOCALAPPDATA/NiyazisugarProxy/diagnostics.dpapi with current-user Windows DPAPI; set them once per Windows user on each PC.
6. Submit a test log. A successful request shows an issue number. Tell the assistant that number in chat. The assistant does not automatically receive notifications from this service.

The service verifies that the target repository is private before creating an issue. Failed requests do not report success. It accepts only authenticated POSTs, caps request size, discards unexpected metadata, redacts token and known credential formats, and returns only the issue number. Logs are not written to server console output. No automatic retries occur to reduce duplicate issues. Do not use this as a public unauthenticated upload endpoint.

## Commands for deployment operator

```sh
npx wrangler login
npx wrangler secret put GITHUB_TOKEN
npx wrangler secret put INGEST_KEY
npx wrangler deploy
```

The service must be deployed and its private repository access verified before the feature is merged/released to the PC fleet. Local/mocked tests do not prove production integration. `node --test worker.test.mjs` runs without secrets or a live GitHub account.
