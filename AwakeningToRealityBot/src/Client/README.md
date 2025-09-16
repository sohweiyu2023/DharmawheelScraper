# ATR Client (Blazor WebAssembly)

This project implements the Awakening to Reality user interface in Blazor WebAssembly.

## Features

- Multi-document selector allowing users to focus queries on specific Dharma texts.
- Conversation timeline with citation chips and responsive design.
- Source viewer preview that displays the selected citation snippet.
- Placeholder integration with the Azure Functions API via `ChatClient`.

## Running locally

1. Install the .NET 8 SDK.
2. From this directory run `dotnet restore` followed by `dotnet run`.
3. The development server listens on `https://localhost:7147` by default.

## Next steps

- Replace the placeholder document selector with data retrieved from the admin API.
- Integrate Azure AD B2C authentication using `Microsoft.Authentication.WebAssembly.Msal`.
- Connect the citation viewer to PDF.js via JavaScript interop for exact page highlighting.
