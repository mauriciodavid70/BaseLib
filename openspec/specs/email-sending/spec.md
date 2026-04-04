# email-sending Specification

## Purpose
TBD - created by archiving change fix-reply-to-mapping-in-amazon-email-sender. Update Purpose after archive.
## Requirements
### Requirement: Reply-To Header Mapping
`AmazonEmailSender` MUST map the `ReplyTo` address list from the `MimeMessage` to the
`ReplyToAddresses` field of the SES v2 `SendEmailRequest` whenever the list is non-empty.

#### Scenario: Reply-To address forwarded to SES
- **WHEN** a `MimeMessage` has one or more Reply-To addresses
- **THEN** `SendEmailRequest.ReplyToAddresses` contains exactly those addresses

#### Scenario: Empty Reply-To list omitted
- **WHEN** a `MimeMessage` has no Reply-To addresses
- **THEN** `SendEmailRequest.ReplyToAddresses` is null or empty

### Requirement: EmailMessageFactory Reply-To Parameter
`EmailMessageFactory.Create` MUST accept an optional `replyTo` parameter and, when
provided and non-empty, add those addresses to `MimeMessage.ReplyTo`.

#### Scenario: Reply-To set via factory
- **WHEN** `EmailMessageFactory.Create` is called with a non-empty `replyTo` list
- **THEN** the returned `MimeMessage.ReplyTo` contains exactly those addresses

#### Scenario: Reply-To omitted via factory
- **WHEN** `EmailMessageFactory.Create` is called without a `replyTo` argument
- **THEN** the returned `MimeMessage.ReplyTo` is empty and existing behaviour is unchanged

