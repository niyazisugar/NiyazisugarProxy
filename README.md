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
