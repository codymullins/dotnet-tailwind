# dotnet-tailwind

Really basic tool to bootstrap Tailwind in .NET Blazor projects.

Run `dotnet tailwind init` to automatically create the necessary build targets and files for a basic Tailwind integration.

## Install
```
dotnet tool install tailwind
```

## How to use

1. In the directory with your `.csproj`:

```
dotnet tool run tailwind init
```

2. Build your project. Tailwind will automatically generate an `app.css` in `wwwroot/css`

3. Add the `app.css` to your `App.razor` 

4. Anytime you build the solution, the `app.css` will now be regenerated.