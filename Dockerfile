# Use the official .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Copy the .env file
COPY .env .env

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5296
ENV ASPNETCORE_ENVIRONMENT=Development

# Expose port
EXPOSE 5296

# Run the application
ENTRYPOINT ["dotnet", "EAD-BE.dll"]