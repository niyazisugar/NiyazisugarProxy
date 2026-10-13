# NiyazisugarProxy

Windows Forms desktop app for routing a selected Metin2 launcher and its sibling `metin2client.bin` / `sezar2client.bin` processes through a SOCKS5 proxy. All paths are matched exactly; other applications use the direct outbound. Python and a browser are not required.

## Installation

Download **NiyazisugarProxy-Windows-x64.zip** from [Releases](../../releases/latest), extract it, and run `NiyazisugarProxy.exe`. The first installation on each PC is manual. Updates after that are installed from the app after the connection is stopped. Requires Windows with .NET Framework 4.x. The ZIP bundles the official sing-box 1.14.2 Windows AMD64 engine.

## Client selection

The selector accepts `.exe` and `.bin` files and supports selecting multiple files in one game directory. If only BIN files are selected, a second dialog asks for the launcher EXE. BIN files are routed but the game is started through its launcher. Files from different game directories are rejected.

## Updates

The app checks public GitHub Releases at startup. Only newer stable versions are offered. The asset must be named `NiyazisugarProxy.exe` and its SHA256 digest must match the value reported by GitHub. A copied installer waits for the running app to exit, replaces the app, keeps `NiyazisugarProxy.exe.previous`, and restarts. Replacement/start errors attempt to restore the previous EXE. A successful process start does not prove the new version works; use the backup for manual rollback if needed. The app updater replaces only the app EXE. On the next connection, an older engine is upgraded automatically to pinned sing-box 1.14.2 in LOCALAPPDATA after verifying the official archive SHA256. Existing engine files and local lists remain intact. No GitHub token is embedded or required on client PCs.

## Publishing a new version

Change both version attributes in `Version.cs` (for example `1.0.1.0`) and push to `main`. GitHub Actions compiles and tests on Windows, verifies and bundles the official engine, then publishes a release tagged `v1.0.1`. Existing release assets are not overwritten. For development, run `build.ps1` on Windows. No proxy lists, credentials, or game files belong in this repository.

## Limits

TUN affects local traffic handling; unselected programs use a direct exit, rather than bypassing the local engine. DNS uses direct Cloudflare DoH with IPv4-only answers, as in the working socksproject. Physical egress is chosen from gateway-bearing default IPv4 routes and both outbounds bind to its Windows alias. This shared DNS policy also affects other apps while the tunnel is active. No kill switch: if the engine stops, the game can reconnect using the normal IP. Anti-cheat compatibility, process matching, and end-to-end game connections require Windows testing. The selected executable and the two named `.bin` files in the same directory are included; differently named or nested game processes require another rule. This app does not change or disable game protection.

Third-party engine: [SagerNet/sing-box v1.14.2](https://github.com/SagerNet/sing-box/tree/v1.14.2), GNU GPL license distributed in the installation ZIP.

## Working-project adaptation (v1.1.0)

Routing and default-interface selection are adapted from [buraakoztoprak/socksproject](https://github.com/buraakoztoprak/socksproject) at the user’s request. Source provenance is recorded in UPSTREAM.md. The optional Server IP field enables exact TCP destination routing, including when process ownership is unavailable. It is blank by default. For the source project's Sezar2 endpoint, enter `185.128.114.39` only when you intend to route that server. All applications' TCP connections to that address match; process-only isolation cannot be promised for this exception. Other servers require their own verified address. CI validates the config against the real bundled sing-box engine, with and without this exception. These tests do not verify anti-cheat behavior or actual game login.

## Local diagnostic logs (v1.1.4)

Diagnostic network submission and its connection settings were removed at the user's request. The app creates `NiyazisugarProxy-Loglar` under the Windows-reported Desktop directory and writes a unique UTF-8 TXT per app session. It appends sanitized logs as events arrive, including after the on-screen log buffer is cleared. After 10 MiB it starts a new session file. The **Log klasörünü aç** button opens the actual folder, including redirected Desktop locations. Proxy credentials, known token formats and the user-profile path are redacted; network addresses remain for diagnosis. The user shares TXT files manually. GitHub application update checks remain enabled. File-write failures are shown instead of claiming a file was saved. This feature works offline.


## v1.2.0 — appearance, saved servers and two clients

The Clientler, Ayarlar and Loglar pages share a rounded desktop interface. Choose dark/light appearance and purple/blue/green accent in Ayarlar. A server name and validated IPv4 can be saved using IP'yi kaydet, and selected for either client. Preferences and selected client paths are saved locally in `%LOCALAPPDATA%\NiyazisugarProxy\preferences.json`; this file contains no proxy credentials.

Enable 2. client etkin to select a second proxy and launcher/EXE/BIN set. Use separate game folders. One sing-box TUN engine owns both SOCKS outbounds, so two independently competing TUN engines are not started. Process routes precede IP fallbacks. When an IP is configured, matched clients only proxy that destination; other destinations from that known process use direct routing.

For two clients targeting the same server IP, process paths are required to distinguish their proxy assignments. Unknown TCP connections to that shared IP are rejected instead of silently sending both clients through the first proxy. Protected processes that cannot be resolved may therefore fail to connect in this mode; assigning two proxies by server IP alone cannot distinguish them. Separate folders identify the paths, but cannot force anti-cheat-protected process lookup to succeed. Two-client live game operation still requires testing on the user's PCs.

## v1.2.1 — resizable window

Drag any window edge or corner to resize. Default size is 840×650, minimum is 640×520. The last normal window size is saved locally and clamped to the current monitor work area on startup. Client cards use a stacked layout at narrow widths, the optional second card is hidden until enabled, and smaller pages scroll to keep all controls accessible.

## v1.3.0 — independent client controls

Each card has Bağlan, Durdur and Proxy ekle. While client 1 is active, enable client 2, choose its files/server/proxy and connect it independently. Client 2 updates watched local sing-box rule files without restarting the TUN engine. Separate loopback TCP SOCKS bridges keep established upstream sessions on their original proxy; stopping one bridge closes only that slot's sessions. Only TCP is supported by these bridges; matching non-TCP game traffic is rejected.

For shared server IPs, unknown-process TCP connections must be rejected to prevent both clients silently using the first proxy. Existing TCP sessions are preserved, but protected processes may be unable to create/recreate connections when both slots are active. A shared-IP warning explains this before activating the second slot. The app cannot make protected process lookup succeed or guarantee Metin2 auto-hunt compatibility without a live game test. Runtime rule reload is asynchronous and may take a few seconds.

## v1.3.1 — remember proxy lists

Imported and manually added proxies, including credentials and selected proxy indices, are stored atomically in a DPAPI CurrentUser encrypted file at `%LOCALAPPDATA%\NiyazisugarProxy\proxies.dat`. Startup restores the list on this PC/Windows account; application updates do not replace it. A corrupt or unreadable file is left untouched and logged. UI preview tests do not read or write the real proxy vault.

## v1.3.2 — bound logging and async forwarding

Engine output enters a bounded 500-line queue (maximum 4096 characters per line) and is drained in batches every 250 ms, rather than posting a separate UI callback and file write per engine line. Overload counts are recorded when excess lines are dropped. Proxy sanitization no longer walks duplicate slot entries. TCP bridge forwarding uses async stream copies instead of two blocked worker threads per established session. This addresses identified sources of load; game reconnect failures and actual hour-long CPU usage still require user-PC logs and process measurements.
