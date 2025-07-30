# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /app

# Copy the solution file and project file
COPY *.sln ./
COPY GloHorizonApi/*.csproj ./GloHorizonApi/

# Restore dependencies
RUN dotnet restore

# Copy the entire source code
COPY . ./

# Build and publish the application
RUN dotnet publish GloHorizonApi/GloHorizonApi.csproj -c Release -o /app/publish

# Use the official .NET 8 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Expose the port the app runs on
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "GloHorizonApi.dll"]