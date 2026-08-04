# Vault Sikker Dokumentopbevaring - Projektplan

## Projektbeskrivelse

En Blazor Server applikation til sikker opbevaring af passwords og personlige dokumenter. Implementerer hashing og encryption for at træne sikkerhedsemnerne.

## Fase 1: Projektstruktur \& Setup

* \[x] Oprette Domain layer (Vault.Domain)
* \[x] Oprette Service layer (Vault.Service)
* \[x] Oprette Web layer (Vault)
* \[x] Oprette test layer (Vault.Testing)
* \[x] Konfigurere dependencies

## Fase 2: Domain Layer

* \[x] Oprette Database Models (User, BankAccount, Transaction)
* \[x] Opsætning af Entity Framework Core
* \[x] DbContext konfiguration (inkl. BankAccount og Transaction)

## Fase 3: Service Layer

* \[x] Oprette DTOs (BankAccountDtos, TransactionDtos)
* \[x] Implementere IAuthenticationService ✓
* \[x] Implementere IBankAccountService
* \[x] Encryption/Hashing utilities (BCrypt, AES) ✓
* \[x] Security Services (IEncryptionService, IHashingService) ✓

## Fase 4: Web Layer

* \[x] Blazor pages og komponenter
* \[x] Login/Register
* \[x] Bank konto management
* \[x] Transaction management
* \[x] Navigation og layout

## Fase 5: Testing

* \[ ] Unit tests (XUnit, Moq)
* \[ ] Integration tests
* \[ ] End-to-end tests med Selenium

## Fase 6: Dokumentation

* \[ ] README.md med setup vejledning
* \[ ] Architecture dokumentation
* \[ ] API/Service dokumentation
* \[ ] definer virkemåde, krav, benyttede Nuget-pakker og deres versioner, installationsvejledning, github badges osv

## Teknologivalg

* **Framework**: Blazor Server .NET 9
* **Database**: SQL Server (LocalDB eller Azure)
* **Hashing**: BCrypt via BCryptNet NuGet
* **Encryption**: System.Security.Cryptography (AES)
* **Testing**: XUnit + Moq + Selenium

