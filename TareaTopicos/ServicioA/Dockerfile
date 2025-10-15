# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo el csproj y restaurar (esto se cachea en una capa separada)
COPY ServicioA.csproj .
RUN dotnet restore

# Ahora copia el resto del código
COPY . .
RUN dotnet publish -c Release -o /app

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ServicioA.dll"]
