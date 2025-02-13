FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CoreAPI.csproj", "."]
RUN dotnet restore "CoreAPI.csproj"
COPY . .
RUN dotnet build "CoreAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoreAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoreAPI.dll"]