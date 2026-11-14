# Security Policy

We take the security of SoundCharts for ArcGIS seriously. Thank you for
helping keep the project and its users safe.

## Supported versions

SoundCharts for ArcGIS is pre-1.0 and under active development. Security fixes
are applied to the latest version and shipped in a new build. Please make
sure you are running the most recent add-in before reporting an issue.

| Version | Supported |
|---|---|
| Latest build | :white_check_mark: |
| Older builds | :x: |

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues
or pull requests.**

Instead, report them privately through GitHub's built-in security advisory
workflow:

1. Go to the
   [Security tab](https://github.com/philliphoff/EncDotNet.ArcGIS/security/advisories)
   of the repository.
2. Click **"Report a vulnerability"** to open a private advisory.
3. Provide as much detail as you can, including:
   - A description of the vulnerability and its impact.
   - Steps to reproduce, or a proof-of-concept.
   - Affected version(s), and the ArcGIS Pro version.
   - Any suggested mitigation, if known.

This channel is private to the maintainers, so details are not disclosed
publicly while the issue is being investigated and fixed.

If the vulnerability is in EncDotNet.S100 itself rather than in the add-in,
report it to
[EncDotNet.S100](https://github.com/philliphoff/EncDotNet.S100/security/advisories)
instead.

## What to expect

- We will acknowledge your report as soon as we are able to review it.
- We will investigate, keep you informed of progress, and work on a fix.
- Once a fix is released, we will publish a security advisory and credit the
  reporter (unless you prefer to remain anonymous).

## Dependencies

This project uses Central Package Management (`Directory.Packages.props`) and
runs `gh-advisory-database` security checks before introducing new
dependencies. NuGet audit fails the build on a package with a known
vulnerability. If you find a vulnerability that stems from a third-party
package, please include the package name and version in your report.
