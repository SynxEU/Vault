# Architecture

## Layers
1. **Vault.Domain**: Entities and EF Core `VaultDbContext`.
2. **Vault.Service**: Authentication, bank-account and crypto services.
3. **Vault**: Blazor Server UI and session-based state.
4. **Vault.Testing**: Unit and E2E tests.

## Flow
- UI captures input through forms.
- Services validate input and persist through EF Core.
- BCrypt hashes passwords.
- AES-256 encrypts banking data.
- PBKDF2 salt is stored per user and reused for key derivation.

## Security model
- Passwords are never stored in plain text.
- Sensitive bank fields are encrypted before saving.
- Only safe metadata such as names and timestamps remain readable.

## Key services
- `IAuthenticationService`
- `IBankAccountService`
- `IEncryptionService`
- `IHashingService`
