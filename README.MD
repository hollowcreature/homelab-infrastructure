# Homelab Infrastructure

Sanitized configuration for a self-hosted homelab: reverse proxy, single sign-on, and a dozen self-hosted services, all running on a Raspberry Pi 5 behind Tailscale (no public exposure).

This repo shows *how* the infrastructure is put together, it will not work if you copy-paste it -> hostnames, secrets, and keys have been replaced with placeholders. See [Setup](#setup).

## Architecture

```
Internet
   │
   ✕  (no public exposure — Tailscale only)
   │
Tailscale network
   │
┌──▼──────────────────────────────────────────┐
│                    Caddy                     │  reverse proxy, TLS termination
└──┬────────────────────────────────────────┬──┘
   │                                        │
┌──▼──────┐                          ┌──────▼───┐
│ Authelia │◄── OIDC / forward_auth ─┤ Services │
└─────────┘                          └──────────┘
```

Every service sits behind Caddy. Most are also gated by Authelia's `forward_auth`, meaning a request has to pass through Authelia's session check before it ever reaches the backend service.

Where the target application supports it, Authelia additionally provides **single sign-on** via OpenID Connect.

## Services

| Service | Purpose | Auth |
|---|---|---|
| Gitea | Git hosting | Gated + OIDC SSO |
| Nextcloud | File sync/storage | OIDC SSO |
| Grafana | Metrics dashboards | Gated + OIDC SSO |
| Vaultwarden | Password manager | Gated + OIDC SSO (master password still required — by design, see below) |
| Jellyfin | Media server | Gated + OIDC SSO (via a community plugin) |
| Firefly III | Personal finance | Gated, trusted-header auth (no native OIDC support) |
| Pi-hole | DNS-level ad blocking | Gated only (no SSO support) |
| Suricata | Network intrusion detection | N/A — passive monitoring |
| Prometheus / Loki | Metrics & log aggregation | Internal only |
| Dashboard | Homelab status page — see [Dashboard](#dashboard) below | Gated |

## Notable issues encountered during development

- **Every reverse-proxied app needed telling it lives under a subpath.** Some (Vaultwarden, Gitea) expect the full prefixed path to reach them (`handle`); others (Nextcloud) expect the prefix stripped before they see it (`handle_path`). Test and see which one works.

- **Client-side reverse-proxy requests need to resolve the public hostname from *inside* the container**, which Docker's embedded DNS can't do for a Tailscale MagicDNS name. Fixed with an `extra_hosts` entry mapping the public hostname to the host's Docker bridge IP.

## Machine-to-machine authentication

The custom dashboard's background health-check service needs to verify that gated services are actually reachable, not just that Authelia's login page is up. Rather than giving it a real user's session or bypassing auth, it authenticates as its own registered OAuth2 client using the `client_credentials` grant, requesting a token scoped to the exact service it's about to check, and attaches that as a Bearer token. Authelia's `access_control` rules scope this client to only the specific resources it needs -> it cannot use its token to reach anything else.

## Dashboard
 
A homelab status page, built from scratch with ASP.NET Core, EF Core, and PostgreSQL. Tracks live status, response time, and 24-hour uptime for every service listed above, with a small homepage (weather, greeting, quick-launch tiles) in front of the full detail view.
 
**Stack**: ASP.NET Core 9 (minimal API), PostgreSQL via EF Core, a hosted `BackgroundService` for polling, vanilla HTML/CSS/JS frontend (Tailwind, no framework).

### Running it
 
1. Set up a PostgreSQL database and update the connection string in `dashboard-app/appsettings.json` (copy from `appsettings.json.example` if present, or set `ConnectionStrings__DefaultConnection` as an environment variable)
2. Set `Authelia__ClientSecret` as an environment variable or via `dotnet user-secrets`, matching the `healthchecker` OIDC client registered in `authelia/configuration.yml`
3. Apply migrations: `dotnet ef database update`
4. Run locally with `dotnet run`, or build the container image with the included `Dockerfile` and deploy via `dashboard-app/docker-compose.yml`
5. Mark any service that sits behind Authelia's gate with `RequiresAuth = true` in its database row, so the health checker requests a Bearer token before checking it


## Setup

1. Replace every occurrence of `your-domain.example.com` with your own domain
2. Generate your own secrets -> replace redacted values in `authelia/configuration.yml`
3. Generate your own RSA key pair for Authelia's OIDC provider (see the comment in `configuration.yml`)
4. Set real database passwords and API keys in each service's `docker-compose.yml` -> use `.env` files or a secrets manager
5. Set up your own network (or equivalent private networking) and TLS certificates