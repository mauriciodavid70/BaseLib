## 1. Fix AmazonEmailSender
- [ ] 1.1 In `MapToAmazonRequest`, after building `destination`, check `message.ReplyTo`
         and set `emailRequest.ReplyToAddresses` to the list of addresses when non-empty.
- [ ] 1.2 Add XML doc comment to the new mapping branch (CS1591 build enforcement).

## 2. Update EmailMessageFactory
- [ ] 2.1 Add optional `replyTo` parameter (`IEnumerable<string>? replyTo = null`) to
         `EmailMessageFactory.Create` after the existing optional parameters.
- [ ] 2.2 When `replyTo` is non-null and non-empty, add entries to `message.ReplyTo`
         using `MailboxAddress`.
- [ ] 2.3 Update the `<param>` XML doc comment on `Create` to document the new parameter.

## 3. Tests
- [ ] 3.1 Add unit test: when `MimeMessage.ReplyTo` contains one address, `SendEmailRequest.ReplyToAddresses`
         contains that address.
- [ ] 3.2 Add unit test: when `MimeMessage.ReplyTo` is empty, `SendEmailRequest.ReplyToAddresses`
         is null or empty (no regression).
- [ ] 3.3 Add unit test: `EmailMessageFactory.Create` with `replyTo` param populates
         `MimeMessage.ReplyTo` correctly.
- [ ] 3.4 Add unit test: `EmailMessageFactory.Create` without `replyTo` param leaves
         `MimeMessage.ReplyTo` empty (existing-call-site regression guard).

## 4. Validate
- [ ] 4.1 Run `dotnet build BaseLib.sln` — zero warnings/errors including CS1591.
- [ ] 4.2 Run `dotnet test` — all tests pass.
- [ ] 4.3 Run `openspec validate fix-reply-to-mapping-in-amazon-email-sender --strict`.
