## Context

`FileAttachment` exposes three optional (`?`) properties — `Stream`, `FileName`, and `MediaType`
— so callers can partially populate the object. The `EmailMessageFactory.BuildMimeMessage` method
iterates over a list of attachments and constructs a `MimePart` for each. At line 64 it passes
`file.Stream` directly to `MimeContent(Stream stream)`, but `Stream` is `Stream?`, triggering
CS8604.

There are no other CS8xxx warnings in the codebase and no existing suppressions (`null!` or
`#pragma warning disable CS8xxx`).

## Goals / Non-Goals

- Goals: zero CS8xxx warnings; no new `null!` suppressions; public API unchanged.
- Non-Goals: enforcing `<TreatWarningsAsErrors>` for CS8xxx (a separate future decision);
  changing the `FileAttachment` model design.

## Decisions

- **Decision: skip attachment when `Stream` is null.**
  `FileAttachment.Stream` is intentionally `Stream?`, signalling that a stream is optional.
  Throwing at build time (via a null-forgiving `!`) would hide a real runtime risk. Throwing an
  exception when `file.Stream is null` is a valid alternative, but skipping is the safer default
  for a library — callers may populate `FileName` and `MediaType` for preview purposes without
  providing actual bytes. A `continue` guard preserves backward compatibility.
- **Alternatives considered:**
  - `file.Stream!` (null-forgiving operator) — silences the warning but masks a real null-deref
    risk at runtime. Rejected per project convention ("never suppress `!` without justification").
  - Change `FileAttachment.Stream` to `Stream` (non-nullable) — **breaking change** for existing
    consumers; rejected as out of scope for a warning-cleanup task.
  - Throw `ArgumentException` when `file.Stream is null` — stricter, but also a behaviour change
    for callers who rely on graceful skipping. Could be revisited in a separate hardening change.

## Risks / Trade-offs

- Silently skipping an attachment with a null stream may hide a caller bug. Mitigation: the
  fix can optionally emit a `Debug.WriteLine` or logger call (if a logger is available) to aid
  diagnostics without changing the external contract.

## Open Questions

- Should a future change add `<TreatWarningsAsErrors>` scoped to CS8xxx to prevent regressions?
  This would be a separate proposal once the baseline is clean.
