FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Server.csproj", "./"]
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

# --- HTTPS Setup ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install openssl for certificate generation
RUN apt-get update && apt-get install -y openssl && \
    rm -rf /var/lib/apt/lists/*

# Generate self-signed certificate
RUN mkdir -p /https && \
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout /https/aspnetapp.key -out /https/aspnetapp.crt \
    -subj "/CN=localhost"

# Copy published app
COPY --from=build /app/publish .

# Set environment variables for HTTPS
ENV ASPNETCORE_HTTPS_PORT=8443
ENV ASPNETCORE_URLS=https://+:8443;http://+:8080
ENV ASPNETCORE_Kestrel__EndPoints__Https__Url=https://+:8443
ENV ASPNETCORE_Kestrel__EndPoints__Https__Certificate__Path=/https/aspnetapp.crt
ENV ASPNETCORE_Kestrel__EndPoints__Https__Certificate__KeyPath=/https/aspnetapp.key

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "Server.dll"]
