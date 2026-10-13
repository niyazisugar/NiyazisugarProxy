# Source provenance

Adapted at the user's explicit request from buraakoztoprak/socksproject:
https://github.com/buraakoztoprak/socksproject

NetworkRoutes.cs follows native/NeviSocks/NetworkRoutes.cs (physical IPv4 route and Windows alias selection).
RoutingConfig.cs adapts native/NeviSocks/Core.cs and NeviSOCKS.ps1 (TUN, DNS and outbound binding). NiyazisugarProxy keeps its own updater, UI and selected client paths. The source's fixed Sezar2 destination exception is optional instead of globally enabled.

The engine remains unmodified official sing-box 1.14.2; its source and license are linked in README.md and bundled in the installer ZIP.
