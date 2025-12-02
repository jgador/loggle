# Stage 1: Build Loggle.Web
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /app

RUN apt-get update && apt-get upgrade -y

ENV \
    # Unset ASPNETCORE_HTTP_PORTS from base image
    ASPNETCORE_HTTP_PORTS= \
    ASPNETCORE_URLS=http://+:8080

COPY Directory.Build.props Directory.Build.props
COPY Directory.Packages.props Directory.Packages.props
COPY src/Loggle/Loggle.csproj src/Loggle/
COPY src/Loggle.Web/Loggle.Web.csproj src/Loggle.Web/

RUN dotnet restore src/Loggle.Web/Loggle.Web.csproj

COPY . ./
WORKDIR /app/src/Loggle.Web

RUN dotnet publish -c Release -o /output

# Stage 2: Final Loggle.Web runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
WORKDIR /app

COPY --from=build /output ./

EXPOSE 8080

ENTRYPOINT ["dotnet", "Loggle.Web.dll"]
