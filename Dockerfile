# Giai đoạn build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Chỉ định rõ đường dẫn từ ngoài vào trong
COPY ["AntiPhisher/AntiPhisher.sln", "./"]
COPY ["AntiPhisher/AntiPhisher.API/AntiPhisher.API.csproj", "AntiPhisher.API/"]
COPY ["AntiPhisher/AntiPhisher.Application/AntiPhisher.Application.csproj", "AntiPhisher.Application/"]
COPY ["AntiPhisher/AntiPhisher.Domain/AntiPhisher.Domain.csproj", "AntiPhisher.Domain/"]
COPY ["AntiPhisher/AntiPhisher.Infrastructure/AntiPhisher.Infrastructure.csproj", "AntiPhisher.Infrastructure/"]

RUN dotnet restore "AntiPhisher.sln"

# Copy toàn bộ source code vào
COPY . .

# Build project API
RUN dotnet publish "AntiPhisher/AntiPhisher.API/AntiPhisher.API.csproj" -c Release -o /app/publish

# Giai đoạn chạy
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "AntiPhisher.API.dll"]