FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CoreAPI/CoreAPI.csproj", "CoreAPI/"]
RUN dotnet restore "CoreAPI/CoreAPI.csproj"
COPY . .
RUN dotnet build "CoreAPI/CoreAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoreAPI/CoreAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

# Configurar timezone para Brasília
ENV TZ=America/Sao_Paulo
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoreAPI.dll"]