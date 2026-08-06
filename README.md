# Vault - Secure Bank Account Management System

En Blazor Server applikation til sikker styring af bankkonti med kryptering af sensitiv data.

## Features

- ✅ **Brugerauthentifikation** - Sikker login/register med BCrypt
- ✅ **Bankkonti Management** - Opret og slet bankkonti
- ✅ **Transaktioner** - Spor alle transaktioner med fuldt krypteringsbeskyttelse
- ✅ **AES-256 Kryptering** - Alle følsomme data krypteres (kontonummer, IBAN, saldo, transaktioner)
- ✅ **Synlig Metadata** - Kontonavn og oprettelsesdato forbliver ukrypteret for hurtig navigation
- ✅ **Sikker Lagring** - Adgangskoder hashes med BCrypt workfactor 12

## Teknologi Stack

| Komponent | Teknologi                              |
|-----------|----------------------------------------|
| Framework | .NET 9 med Blazor Server               |
| Database | SQL Server (LocalDB eller SSMS)        |
| Kryptering | System.Security.Cryptography (AES-256) |
| Hashing | BCrypt.Net (workfactor 12)             |
| ORM | Entity Framework Core                  |
| Frontend | Razor Components, Bootstrap 5 & Radzen |

## Projekt Struktur

```
Vault/
├── Vault.Domain/           # Domain models og database context
│   ├── Entities/
│   │   ├── Emums/
│   │   │   ├── AccountType.cs
│   │   │   ├── TransactionStatus.cs
│   │   │   └── TransactionType.cs
│   │   ├── User.cs
│   │   ├── BankAccount.cs
│   │   └── Transaction.cs
│   └── Persistence/
│       └── VaultDbContext.cs
│       └── VaultDbContextFactory.cs
├── Vault.Service/          # Business logic og services
│   ├── Interfaces/
│   │   ├── IAuthenticationService.cs
│   │   ├── IBankAccountService.cs
│   │   └── ITransactionService.cs
│   ├── Services/
│   │   ├── AuthenticationService.cs
│   │   ├── BankAccountService.cs
│   │   └── TransactionService.cs
│   ├── Security/
│   │   ├── IEncryptionService.cs
│   │   ├── EncryptionService.cs
│   │   ├── IHashingService.cs
│   │   └── HashingService.cs
│   └── DTOs/
│       ├── AuthDtos.cs
│       ├── TransactionDtos.cs
│       └── BankAccountDtos.cs
├── Vault.Testing/          # Unit
│   ├── ServiceTests/
│   │   ├── AuthenticationServiceTests.cs
│   │   ├── EncryptionServiceTests.cs
│   │   ├── IHashingServiceTests.cs
│   │   ├── IHashingServiceTests.cs
│   │   └── HashingServiceTests.cs
│   └── BrowserTests/
│       ├── BrowserManipulationTests.cs
│       ├── DemoHelper.cs
│       └── SelectingHTMLElementsTests.cs
└── Vault/                  # Blazor Web Application
    ├── Components/
    │   ├── Pages/
    │   │   ├── Home.razor
    │   │   ├── Login.razor
    │   │   ├── Register.razor
    │   │   ├── Logout.razor
    │   │   ├── BankAccount.razor
    │   │   └── BankAccounts.razor
    │   └── Layout/
    │       ├── MainLayout.razor
    │       └── NavMenu.razor
    └── Program.cs
```

## Sikkerhed

### Datakryptering

Alle følgende data krypteres med **AES-256 (CBC mode)** med tilfældig IV for hver encryption:
- Kontonummer
- Registeringsnummer
- IBAN
- Kontosaldo
- Valuta
- Transaktionsbeskrivelse
- Transaktionsbeløb
- Modtager

### Metadata (Ikke Krypteret)
- Kontonavn
- Oprettelsesdato
- KontoType
- TransaktionsType
- Transaktionsdato

### Adgangskodesikkerhed

Adgangskoder behandles med:
- **BCrypt** med workfactor 12
- Salt genereres automatisk
- Krypteringsnøgler afledes fra adgangskoden via **PBKDF2** (10.000 iterationer, SHA-256)

## Setup Vejledning

### Forudsætninger
- .NET 9 SDK
- SQL Server (LocalDB) eller SSMS forbindelse
- Visual Studio 2026 eller anden IDE

### Installation

1. **Clone repository**
```bash
git clone <repository-url>
cd Vault
```

2. **Restore NuGet packages**
```bash
dotnet restore
```

3. **Konfigurer database forbindelse** (appsettings.json)
```json
{
  "ConnectionStrings": {
    "VaultDb": "Server=(localdb)\\mssqllocaldb;Database=VaultDb;Trusted_Connection=true;"
  }
}
```

4. **Kør database migrations**
```bash
dotnet ef database update --project Vault.Domain --startup-project Vault
```

5. **Start applikationen**
```bash
cd Vault
dotnet run
```

Applikationen vil være tilgængelig på `https://localhost:7xxx`

## Databasemodeller

### User (Identity)
```csharp
- EncryptionKeyHash: string (Salt for key derivation)
- CreatedAt: DateTime
- UpdatedAt: DateTime
- BankAccounts: ICollection<BankAccount>
```

### BankAccount
```csharp
- Id: int (Primary Key)
- UserId: int (Foreign Key)
- AccountName: string (Visible, Required)
- AccountType: AccountType (Enum, Visible, Required)
- EncryptedAccountNumber: string (Encrypted)
- EncryptedRegistrationNumber: string (Encrypted)
- EncryptedIBAN: string (Encrypted)
- EncryptedBalance: string (Encrypted)
- EncryptedCurrency: string (Encrypted)
- EncryptedBankName: string (Encrypted)
- CreatedAt: DateTime (Visible)
- Transactions: ICollection<Transaction>
```

### Transaction
```csharp
- Id: int (Primary Key)
- BankAccountId: int (Foreign Key)
- TransactionDate: DateTime (Visible)
- TransactionType: TransactionType (Enum, Visible, Required)
- EncryptedDescription: string (Encrypted)
- EncryptedAmount: string (Encrypted)
- EncryptedRecipientName: string (Encrypted, Optional)
- EncryptedRecipientRegistrationNumber: string (Encrypted, Optional)
- EncryptedRecipientAccountNumber: string (Encrypted, Optional)
```

## API/Service Documentation

### IAuthenticationService

```csharp
Task<AuthResponse> RegisterAsync(RegisterRequest request)
Task<AuthResponse> LoginAsync(LoginRequest request)
Task<User?> GetUserAsync(int userId)
Task<User?> GetUserByUsernameAsync(string username)
```

### IBankAccountService

```csharp
Task<ServiceResponse<BankAccountDetailDto>> CreateAccountAsync(int userId, CreateBankAccountRequest request)
Task<ServiceResponse<BankAccountDetailDto>> GetAccountAsync(int userId, int accountId)
Task<ServiceResponse<List<BankAccountDto>>> GetAllAccountsAsync(int userId)
Task<ServiceResponse<BankAccountDetailDto>> UpdateAccountAsync(int userId, UpdateBankAccountRequest request)
Task<ServiceResponse<bool>> DeleteAccountAsync(int userId, int accountId)
```


### ITransactionService

```csharp
Task<ServiceResponse<TransactionDto>> CreateTransactionAsync(int userId, CreateTransactionRequest request)
Task<ServiceResponse<List<TransactionDto>>> GetAccountTransactionsAsync(int userId, int accountId)
Task<ServiceResponse<bool>> DeleteTransactionAsync(int userId, int transactionId)
```

## Kryptering og Decryptering Flow

### Oprettelse af Bankkonto
1. Bruger logger ind → Adgangskode verificeres
2. Bruger opretter konto med: AccountName, AccountNumber, IBAN, Balance, etc.
3. Alle følsomme felter krypteres individuelt med **AES-256**
4. En tilfældig **IV** genereres for hver kryptering
5. Data gemmes som `{EncryptedValue}:{Base64EncodedIV}`
6. Kun AccountName og CreatedAt gemmes i klartekst

### Hentning af Bankkonto
1. Bruger forespørger konto
2. System verificerer tilhørighed (userId check)
3. Krypterede felter dekrypteres ved hjælp af brugerens encryption key
4. Dekrypteret data returneres til frontend
5. Kun tilgængelig for autoriseret bruger

## Workflow Eksempler

### Registrering
```
1. Bruger fylder: username, email, password, confirm password
2. Backend validerer input
3. Adgangskode hashes med BCrypt
4. Krypteringsnøgle afledes fra adgangskode (PBKDF2)
5. Salt gemmes i EncryptionKeyHash
6. Bruger oprettes i database
```

### Login
```
1. Bruger fylder: username, password
2. Backend finder bruger efter username
3. Adgangskode verificeres mod BCrypt hash
4. Ved success: UserID og Username gemmes i session
5. Bruger redirects til /accounts
```

### Oprettelse af Bankkonto
```
1. Bruger (må være logget ind) udfylder kontoformular
2. Alle felter valideres (required, format, osv.)
3. Krypteringsnøgle afledes fra salt (gemt i user.EncryptionKeyHash)
4. Hver følsom field krypteres individuelt
5. Konto gemmes i database
6. Liste opdateres
```

### Sporing af Transaktioner
```
1. Bruger åbner bankkonto
2. System henter alle transaktioner for kontoen
3. Hver transaction dekrypteres
4. Vises i tabel format
5. Bruger kan tilføje nye eller slette gamle
```

## Performance Overvejelser

- **Lazy Loading**: Relaterede entiteter loader ikke automatisk
- **Efficient Queries**: Databaseforespørgsler bruger `.FirstOrDefaultAsync()` for at minimalisere data transfer
- **Validation**: Input valideres både frontend og backend

## Future Enhancements

- [ ] Two-Factor Authentication (2FA)
- [ ] Audit logging for alle transaktioner
- [ ] Export til CSV/PDF
- [ ] Multiple currencies support
- [ ] Recurring transactions
- [ ] Budget tracking

## Licens

MIT License - Se LICENSE filen for detaljer

## Kontakt

For spørgsmål eller issues, kontakt synx_eu på discord.
