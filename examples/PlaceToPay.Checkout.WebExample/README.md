# PlaceToPay Checkout - Ejemplo .NET

Aplicacion ASP.NET Core Minimal API (net10.0) que recorre todos los endpoints del SDK .NET de PlaceToPay Checkout contra sandbox.

Harness local de validación del SDK. No se pública en NuGet.

## Ejecutar

```bash
cd examples/PlaceToPay.Checkout.WebExample

dotnet user-secrets set "PlaceToPay:Login"     "YOUR_LOGIN"
dotnet user-secrets set "PlaceToPay:SecretKey" "YOUR_SECRET"

dotnet run
# abrir http://localhost:5000
```

## Flujos

| Flujo | Endpoint | Metodo SDK |
| --- | --- | --- |
| Crear session (form) | `POST /create` | `CreateSessionAsync` |
| Retorno despues de pago | `GET /return` | `QuerySessionAsync` |
| Consultar session | `GET /session/{id}` | `QuerySessionAsync` |
| Cancelar session | `POST /session/{id}/cancel` | `CancelSessionAsync` |
| Collect | `POST /collect` | `CollectAsync` |
| Reverse | `POST /reverse` | `ReverseAsync` |
| Invalidate token | `POST /invalidate` | `InvalidateTokenAsync` |

## Transacciones cubiertas

Este portal cubre los 2 bloques funcionales de Checkout:

- Sesion: `CreateSessionAsync`, `QuerySessionAsync`, `CancelSessionAsync`.
- Token/post-pago: `CollectAsync`, `ReverseAsync`, `InvalidateTokenAsync`.

## Funcionalidades destacadas del ejemplo

- Firma SHA-256 y nonce criptografico manejados por el SDK.
- Verificacion de estados de transaccion via `QuerySessionAsync`.
- Flujo de retorno de pagador (`/return`) para conciliacion basica.
