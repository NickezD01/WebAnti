# Giai đoạn build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy tất cả mọi thứ vào thư mục /src trong container
COPY . .

# Build dự án trực tiếp bằng đường dẫn từ thư mục gốc /src
# Lệnh này sẽ tự động restore các gói cần thiết nên không cần chạy dotnet restore riêng
RUN dotnet publish "AntiPhisher/AntiPhisher.API/AntiPhisher.API.csproj" -c Release -o /app/publish

# Giai đoạn chạy
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Thiết lập cổng kết nối
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Chạy file dll của project API
ENTRYPOINT ["dotnet", "AntiPhisher.API.dll"]