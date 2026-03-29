FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8081
EXPOSE 8082

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EventMaster.csproj", "./"]
RUN dotnet restore "EventMaster.csproj"
COPY . .
RUN dotnet build "EventMaster.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EventMaster.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY https /https
ENV ASPNETCORE_URLS="https://+:8082;http://+:8081"
ENV ASPNETCORE_Kestrel__Certificates__Default__Password="devcert"
ENV ASPNETCORE_Kestrel__Certificates__Default__Path="/https/aspnetapp.pfx"
ENTRYPOINT ["dotnet", "EventMaster.dll"]