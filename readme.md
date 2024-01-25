# dotnet-tailwind

Really basic tool to bootstrap Tailwind in .NET Blazor projects.

Run `dotnet tailwind init` to automatically create the necessary build targets and files for a basic Tailwind integration.

## Installation

```sh
cd <repository root directory>
dotnet new tool-manifest
dotnet tool install tailwind
```

## Initializing Tailwind

```sh
cd .\path\to\project
dotnet tailwind init
```

**Add to your `App.razor` in the `<head>`:**

```html
<link rel="stylesheet" href="css/site.css" />
```

Anytime you build the solution, `wwwroot/css/site.css` will now be regenerated.

## Updating Tailwind

```sh
dotnet tailwind update
```

## Support

We support the below versions. The tool may work with versions outside this range, but we're not actively testing them. Your mileage may vary.

| .NET 6 | .NET 7 | .NET 8 | .NET 9 |
| -- | -- | -- | -- |
| ❌ | ✅ | ✅ |  |

## Sponsors

Sponsored by [PureBlazor](https://pureblazor.com)
