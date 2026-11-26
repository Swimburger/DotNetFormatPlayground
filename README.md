# .NET Format String Playground

An executable quick reference for formatting exact .NET values across cultures. The app runs entirely in the browser with Blazor WebAssembly, preserves `decimal`, `Int128`, and `BigInteger` precision inside .NET, and generates the C# needed to reproduce each result.

## What it does

- Formats `int`, `long`, `Int128`, `BigInteger`, `decimal`, `double`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, and `Guid`.
- Compares standard format output in the selected culture and invariant culture.
- Previews custom format strings live with a type-specific insertable reference.
- Generates equivalent interpolation, `ToString`, and `string.Format` code.
- Includes curated examples and links to official .NET formatting documentation.

Input never leaves the browser. Results reflect the bundled .NET 11 runtime and ICU globalization data.

## Prerequisites

- .NET SDK 11.0.100 or later in the 11.0 feature band
- .NET `wasm-tools` workload
- Node.js 22.18 or later
- pnpm 12

## Develop and test

```sh
dotnet workload install wasm-tools
dotnet restore
dotnet test
pnpm install
pnpm run build:cloudflare
pnpm exec playwright install chromium
pnpm run test:e2e
```

`pnpm run test:e2e` starts the built Cloudflare project on `http://127.0.0.1:8794`.

## Deploy

The project uses Cloudflare Workers Static Assets through `cloudflare.config.ts` and the `cf` CLI:

```sh
pnpm run deploy
```

Production: [format.swimburger.net](https://format.swimburger.net/)

## Scope

This playground focuses on converting already-typed values into text. For time-zone interpretation, `DateTime.Kind`, daylight-saving transitions, and UTC conversion, use the [.NET DateTime Lab](https://datetime.swimburger.net/).

## License

[MIT](LICENSE)
