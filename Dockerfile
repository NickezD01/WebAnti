# Giai đoạn build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file .sln và các file project từ thư mục "AntiPhisher" vào thư mục làm việc hiện tại của Docker (.)
COPY ["AntiPhisher/AntiPhisher.sln", "."]
COPY ["AntiPhisher/AntiPhisher.API/AntiPhisher.API.csproj", "AntiPhisher.API/"]
COPY ["AntiPhisher/AntiPhisher.Application/AntiPhisher.Application.csproj", "AntiPhisher.Application/"]
COPY ["AntiPhisher/AntiPhisher.Domain/AntiPhisher.Domain.csproj", "AntiPhisher.Domain/"]
COPY ["AntiPhisher/AntiPhisher.Infrastructure/AntiPhisher.Infrastructure.csproj", "AntiPhisher.Infrastructure/"]

# Restore sau khi đã copy file .sln
RUN dotnet restore "AntiPhisher.sln"

# Copy toàn bộ source code vào container
COPY . .

# Build dự án API 
# Lưu ý: Vì lúc này ta đã COPY toàn bộ code vào rồi, đường dẫn sẽ là từ gốc container
RUN dotnet publish "AntiPhisher/AntiPhisher.API/AntiPhisher.API.csproj" -c Release -o /app/publish

# Giai đoạn chạy
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "AntiPhisher.API.dll"]