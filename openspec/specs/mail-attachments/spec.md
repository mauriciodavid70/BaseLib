# mail-attachments Specification

## Purpose
TBD - created by archiving change update-nullable-warnings-cleanup. Update Purpose after archive.
## Requirements
### Requirement: Null Stream Guard for File Attachments

When building a MIME email message, `EmailMessageFactory` SHALL skip any `FileAttachment`
whose `Stream` property is null rather than passing the null value to `MimeContent`, so that
the factory never emits CS8604 at compile time and never throws a `NullReferenceException`
at runtime.

#### Scenario: Attachment with null stream is skipped

- **WHEN** `BuildMimeMessage` is called with a list that includes a `FileAttachment` where
  `Stream` is `null`
- **THEN** no `MimePart` is added for that attachment and the returned `MimeMessage` contains
  only the attachments whose `Stream` is non-null

#### Scenario: Attachment with valid stream is included

- **WHEN** `BuildMimeMessage` is called with a `FileAttachment` whose `Stream` is a non-null
  readable `Stream`
- **THEN** a `MimePart` is created for that attachment and included in the returned
  `MimeMessage`

#### Scenario: Build produces zero CS8xxx warnings

- **WHEN** `dotnet build BaseLib.sln` is executed after the fix is applied
- **THEN** the build output contains zero warnings matching `warning CS8`

