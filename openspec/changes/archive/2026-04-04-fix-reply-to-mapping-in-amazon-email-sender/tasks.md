## 1. Fix AmazonEmailSender
- [x] 1.1 In `MapToAmazonRequest`, after building `destination`, check `message.ReplyTo`
         and set `emailRequest.ReplyToAddresses` to the list of addresses when non-empty.
- [x] 1.2 Add XML doc comment to the new mapping branch (CS1591 build enforcement).

## 2. Update EmailMessageFactory
- [x] 2.1 Add optional `replyTo` parameter (`IEnumerable<string>? replyTo = null`) to
         `EmailMessageFactory.Create` after the existing optional parameters.
- [x] 2.2 When `replyTo` is non-null and non-empty, add entries to `message.ReplyTo`
         using `MailboxAddress`.
- [x] 2.3 Update the `<param>` XML doc comment on `Create` to document the new parameter.

## 3. Tests
- [x] 3.1 Add unit test: when `MimeMessage.ReplyTo` contains one address, `SendEmailRequest.ReplyToAddresses`
         contains that address.
- [x] 3.2 Add unit test: when `MimeMessage.ReplyTo` is empty, `SendEmailRequest.ReplyToAddresses`
         is null or empty (no regression).
- [x] 3.3 Add unit test: `EmailMessageFactory.Create` with `replyTo` param populates
         `MimeMessage.ReplyTo` correctly.
- [x] 3.4 Add unit test: `EmailMessageFactory.Create` without `replyTo` param leaves
         `MimeMessage.ReplyTo` empty (existing-call-site regression guard).

## 4. Validate
- [x] 4.1 Run `dotnet build BaseLib.sln` — zero warnings/errors including CS1591.
- [x] 4.2 Run `dotnet test` — all tests pass.
- [x] 4.3 Run `openspec validate fix-reply-to-mapping-in-amazon-email-sender --strict`.
