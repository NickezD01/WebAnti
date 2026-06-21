# Giai đoạn build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy toàn bộ thư mục AntiPhisher vào trong Docker
# Điều này giúp Docker hiểu toàn bộ cấu trúc dự án của bạn ngay lập tức
COPY ["AntiPhisher/", "AntiPhisher/"]

# Restore các gói NuGet bằng cách trỏ thẳng vào file .sln
RUN dotnet restore "AntiPhisher/AntiPhisher.sln"

# Copy tất cả mọi thứ còn lại (bao gồm các file config, code source)
COPY . .

# Build dự án API
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