# Giai đoạn build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# SỬA CÁC DÒNG NÀY:
COPY ["AntiPhisher.sln", "./"]
COPY ["AntiPhisher.API/AntiPhisher.API.csproj", "AntiPhisher.API/"]
COPY ["AntiPhisher.Application/AntiPhisher.Application.csproj", "AntiPhisher.Application/"]
COPY ["AntiPhisher.Domain/AntiPhisher.Domain.csproj", "AntiPhisher.Domain/"]
COPY ["AntiPhisher.Infrastructure/AntiPhisher.Infrastructure.csproj", "AntiPhisher.Infrastructure/"]

RUN dotnet restore "AntiPhisher.sln"

# Copy toàn bộ source code
COPY . .

# Build project API
RUN dotnet publish "AntiPhisher.API/AntiPhisher.API.csproj" -c Release -o /app/publish

# Giai đoạn chạy
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "AntiPhisher.API.dll"]