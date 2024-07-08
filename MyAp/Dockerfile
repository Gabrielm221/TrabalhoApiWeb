# Imagem base do SDK do .NET Core para compilação
FROM mcr.microsoft.com/dotnet/sdk:8.0.200 AS build
WORKDIR /app

# Copia e restaura os arquivos do projeto
COPY API/*.csproj ./
RUN dotnet restore

# Copia o código fonte e compila a aplicação
COPY API/. ./
RUN dotnet publish -c Release -o out

# Imagem base mínima do .NET Core para execução
FROM mcr.microsoft.com/dotnet/aspnet:8.0.200 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Comando para iniciar a aplicação
ENTRYPOINT ["dotnet", "MyAp.dll"]
