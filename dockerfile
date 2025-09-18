FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["FastReportPdfServer.csproj", "./"]
RUN dotnet restore "./FastReportPdfServer.csproj"
COPY . .
RUN dotnet publish "FastReportPdfServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FastReportPdfServer.dll"]
