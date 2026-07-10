# Pull Request

## Summary

<!-- One sentence. What does this PR change and why is it needed? -->

## Type of change

- [ ] Bug fix (non-breaking)
- [ ] New feature (non-breaking, additive)
- [ ] Breaking change (requires major version bump)
- [ ] Docs / tests only
- [ ] Dependency update

## Checklist

- [ ] `dotnet build` is clean (TreatWarningsAsErrors=true).
- [ ] `dotnet test --settings coverlet.runsettings` passes (≥ 85 % line coverage enforced).
- [ ] I updated `CHANGELOG.md` under `Unreleased` if this is a user-visible change.
- [ ] If the auth test vector was touched, the regression still matches the Python and Java SDKs (ARCHITECTURE.md §3).
- [ ] I did NOT commit credentials, API keys or sandbox payloads (`*.webhook.json` is gitignored).

## Cross-SDK parity

- [ ] This change is .NET-only.
- [ ] This change should also be mirrored in `checkout-python` and `checkout-java`.
       <!-- Link sibling PRs or open issues: -->

## Testing

<!--
How did you verify this works? Include:
  - the command you ran
  - screenshots / log snippets if UI / behavior changed
  - sandbox output if you ran the Integration category against a real gateway
-->
