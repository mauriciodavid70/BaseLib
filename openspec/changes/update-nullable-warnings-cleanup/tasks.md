## 1. Audit
- [ ] 1.1 Run `dotnet build BaseLib.sln 2>&1 | grep "warning CS8"` and record every CS8xxx warning across all four projects
- [ ] 1.2 Search the codebase for `null!`, `#pragma warning disable CS8`, and nullable-suppression attributes to confirm there are no hidden workarounds

## 2. Fix nullable warning in EmailMessageFactory
- [ ] 2.1 Add a null guard in `EmailMessageFactory.cs` before line 64: skip the attachment (continue the loop) when `file.Stream` is null
- [ ] 2.2 Verify the fix eliminates CS8604 by running `dotnet build BaseLib.sln 2>&1 | grep "warning CS8"` — expect zero matches

## 3. Test
- [ ] 3.1 Add a unit test in `BaseLib.Core.Tests` that passes a `FileAttachment` with `Stream = null` to `EmailMessageFactory` and asserts the attachment is silently omitted (no exception thrown)
- [ ] 3.2 Run `dotnet test` and confirm all tests pass

## 4. Final validation
- [ ] 4.1 Run a full `dotnet build BaseLib.sln` and confirm `0 Warning(s)` related to CS8xxx
- [ ] 4.2 Confirm the build still produces zero CS1591 errors (XML doc requirement unchanged)
