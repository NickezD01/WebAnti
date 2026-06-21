# Giai đoạn build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file .sln và các file project để restore
COPY ["AntiPhisher.sln", "./"]
COPY ["AntiPhisher.API/*.csproj", "AntiPhisher.API/"]
COPY ["AntiPhisher.Application/*.csproj", "AntiPhisher.Application/"]
COPY ["AntiPhisher.Domain/*.csproj", "AntiPhisher.Domain/"]
COPY ["AntiPhisher.Infrastructure/*.csproj", "AntiPhisher.Infrastructure/"]

RUN dotnet restore "AntiPhisher.sln"

# Copy toàn bộ source code vào
COPY . .

# Build project API
RUN dotnet publish "AntiPhisher.API/AntiPhisher.API.csproj" -c Release -o /app/publish

# Giai đoạn chạy
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Thiết lập port cho Render/Railway
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "AntiPhisher.API.dll"]