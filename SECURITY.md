# Security Policy

## Supported Versions

The following versions of KeePassAutoReload are currently supported with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.2.x   | :white_check_mark: |
| < 1.2.0 | :x:                |

Only the latest patch release in the 1.2.x line receives fixes. Users are strongly encouraged to update to the latest release.

## Reporting a Vulnerability

If you discover a security vulnerability in KeePassAutoReload, please report it responsibly:

1. Open a GitHub issue prefixed with `[SECURITY]` describing the vulnerability in detail.
2. Include reproduction steps, affected versions, and the potential impact.
3. Allow reasonable time for the maintainers to investigate and release a fix before disclosing publicly.

## Disclosure Policy

- Reports are acknowledged within a reasonable timeframe.
- Fixes are released as a new patch version and documented in `CHANGELOG.md`.
- Credit is given to reporters unless they prefer to remain anonymous.

## Security Measures

- Release assets are accompanied by a `SHA256SUMS.txt` file so users can verify integrity after download.
- Updates are fetched over HTTPS using TLS 1.2/1.3.
- The plugin verifies downloaded update assets against the published SHA256 checksums before installation.
