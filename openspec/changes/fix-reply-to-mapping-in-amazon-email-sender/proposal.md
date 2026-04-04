# Change: Fix Reply-To mapping in AmazonEmailSender

## Why

`AmazonEmailSender.MapToAmazonRequest` constructs an SES v2 `SendEmailRequest` but never
populates `ReplyToAddresses`, so Reply-To headers set on the `MimeMessage` are silently
dropped. Recipients who hit "Reply" in their mail client therefore reply to the `From`
address instead of the intended Reply-To address (GitHub issue #6).

## What Changes

- `AmazonEmailSender.MapToAmazonRequest` SHALL read `message.ReplyTo` from the
  `MimeMessage` and set `SendEmailRequest.ReplyToAddresses` when the list is non-empty.
- `EmailMessageFactory.Create` SHALL accept an optional `replyTo` parameter
  (`IEnumerable<string>?`) and populate `MimeMessage.ReplyTo` when provided, giving
  callers a convenient, consistent way to set Reply-To without manipulating the
  `MimeMessage` after construction.

## Impact

- Affected specs: `email-sending` (new capability spec — none existed before)
- Affected code:
  - `BaseLib.Core.AmazonCloud/Mail/AmazonEmailSender.cs` — core bug fix
  - `BaseLib.Core/Mail/EmailMessageFactory.cs` — factory convenience parameter
  - `BaseLib.Core.Tests/` — new unit-test coverage for both changes
- **Non-breaking**: the new `replyTo` parameter on `EmailMessageFactory.Create` is
  optional (`= null`), preserving all existing call sites. No public type or required
  signature changes.
