# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY Stagiaire.Contracts/Stagiaire.Contracts.csproj Stagiaire.Contracts/
COPY Smartek.Common/Smartek.Common.csproj Smartek.Common/
COPY SubjectMatching.Service/SubjectMatching.Service.csproj SubjectMatching.Service/
RUN dotnet restore SubjectMatching.Service/SubjectMatching.Service.csproj
COPY . .
WORKDIR /app/SubjectMatching.Service
RUN dotnet publish -c Release -o /out

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SubjectMatching.Service.dll"]
