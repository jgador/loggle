# Local Development Notes

Catch-all reminders for working on Loggle locally—CLI snippets, scripts, and things that are easy to forget.

## Environment prerequisites

- **.NET SDK 10** (`global.json` pins to `10.0.100`).
- **Docker Desktop** (used for Loggle.Web images and local stack).
- **PowerShell 7+** to run the helper scripts.
- Ensure `dotnet`, `docker`, and `pwsh` are on `PATH`.

## Useful folders/scripts

- `build-nuget.ps1` – wraps `dotnet pack` for `src/Loggle/Loggle.csproj`.
- `local/Dockerfile` – builds the Loggle.Web container used by local deployments.
- `examples/*.ps1` – spin up sample apps or docker-compose environments (`examples/loggle-compose.ps1`).
- `docs/azure` – cloud deployment guide if you need to sync local changes with ARM templates.

## NuGet package (Loggle)

1. Update `src/Loggle/Loggle.csproj` with the new `Version`, `AssemblyVersion`, `FileVersion`, `PackageReadmeFile`, and `PackageLicenseFile` as needed.
2. Build the package:

   ```powershell
   pwsh ./build-nuget.ps1 -Version 1.0.0-rc1
   ```

   - Drops `nupkgs/Loggle.<version>.nupkg`.
3. Manually publish when ready:

   ```powershell
   dotnet nuget push nupkgs/Loggle.1.0.0-rc1.nupkg --source nuget.org --api-key <your-api-key>
   ```

Need to run `dotnet pack` directly?

```powershell
dotnet pack src/Loggle/Loggle.csproj -c Release -o nupkgs /p:Version=1.0.0-rc1
```

## Docker image (Loggle.Web)

```powershell
docker build -f local/Dockerfile `
  -t jessegador/loggle-web:1.0.0-rc1 `
  -t jessegador/loggle-web:latest `
  --no-cache .
```

- Run from repo root so the Docker context contains the whole solution.
- Use `--no-cache` for a clean rebuild.
- Push both tags when satisfied:

  ```powershell
  docker push <dockerhub-user-or-org>/loggle-web:1.0.0-rc1
  docker push <dockerhub-user-or-org>/loggle-web:latest
  ```

## Debugging checklist

- Restore once at repo root: `dotnet restore Loggle.sln`.
- Run tests before publishing: `dotnet test Loggle.sln`.
- For Loggle.Web local testing, `dotnet watch run --project src/Loggle.Web/Loggle.Web.csproj` is handy.

Add to this file whenever you discover another “don’t forget” item.
