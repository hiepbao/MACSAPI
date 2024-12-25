# Sử dụng image chính thức của .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Đặt thư mục làm việc
WORKDIR /app

# Sao chép file .csproj và khôi phục dependency
COPY *.csproj ./
RUN dotnet restore

# Sao chép toàn bộ source code
COPY . ./
RUN dotnet publish -c Release -o out

# Sử dụng image runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Lệnh chạy ứng dụng
ENTRYPOINT ["dotnet", "MACSAPI.dll"]
