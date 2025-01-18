# Stage 1: Build Loggle.Web
# FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
WORKDIR /app

COPY Loggle.sln ./
COPY src/Loggle/Loggle.csproj src/Loggle/
COPY src/Loggle.Web/Loggle.Web.csproj src/Loggle.Web/
COPY test/Loggle.Tests/Loggle.Tests.csproj test/Loggle.Tests/
COPY test/Loggle.Hosting.TestApp/Loggle.Hosting.TestApp.csproj test/Loggle.Hosting.TestApp/

RUN dotnet restore

COPY . ./
WORKDIR /app/src/Loggle.Web

# RUN dotnet build -c Release src/Loggle.Web/Loggle.Web.csproj -o output
RUN dotnet publish -c Release -o /output

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble AS final
WORKDIR /app

COPY --from=build /output ./

ENV \
    # Unset ASPNETCORE_HTTP_PORTS from base image
    ASPNETCORE_HTTP_PORTS= \
    ASPNETCORE_URLS=http://+:8080

EXPOSE 8080
ENTRYPOINT ["dotnet", "Loggle.Web.dll"]
