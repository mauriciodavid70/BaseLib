# Change: Resolve all nullable reference type warnings (CS8xxx)

## Why

GitHub issue #7 requests that `<Nullable>enable</Nullable>` be enabled across all projects. The
setting is already present in every `.csproj`, so the remaining work is to eliminate the one
active nullable warning (CS8604 in `EmailMessageFactory`) and to audit all four projects to
confirm zero CS8xxx warnings remain. Reaching a clean baseline makes it policy-safe to treat
nullable warnings as errors in a future hardening step.

## What Changes

- **Audit** all four projects (`BaseLib.Core`, `BaseLib.Core.AmazonCloud`, `BaseLib.Core.MySql`,
  `BaseLib.Core.Tests`) for CS8xxx warnings emitted during `dotnet build`.
- **Fix** CS8604 in `BaseLib.Core/Mail/EmailMessageFactory.cs` (line 64): `file.Stream` is
  typed `Stream?` on `FileAttachment` but passed to `MimeContent(Stream stream)`, which requires
  a non-null argument. The fix is to add a null guard that skips the attachment when
  `file.Stream` is null (see `design.md` for rationale).
- **Confirm** no `null!` suppressions, `#pragma warning disable CS8xxx` pragmas, or
  attribute-based workarounds exist that paper over genuine nullability issues.
- The existing public API surface is **not changed in a breaking way** — no public signatures
  are altered. `FileAttachment.Stream` remains `Stream?`.

## Impact

- Affected specs: `mail-attachments` (new capability spec introduced by this change)
- Affected code: `BaseLib.Core/Mail/EmailMessageFactory.cs`
- No NuGet version bump required (internal quality fix, no public API change)
- Consumers are unaffected; attachments with a null `Stream` are silently skipped, which is
  consistent with the optional nature of the `Stream?` property
