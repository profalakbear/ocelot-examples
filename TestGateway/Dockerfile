FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TestGateway.csproj", "."]
RUN dotnet restore "./TestGateway.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "TestGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TestGateway.dll"]
