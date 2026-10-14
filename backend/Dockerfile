FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore SchoolPlatform.sln

RUN dotnet publish \
    src/SchoolPlatform.Api/SchoolPlatform.Api.csproj \
    -c Release \
    -o /app/publish

RUN dotnet publish \
    src/SchoolPlatform.Operator/SchoolPlatform.Operator.csproj \
    -c Release \
    -o /app/operator-publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .
COPY --from=build /app/operator-publish ./operator

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "SchoolPlatform.Api.dll"]
