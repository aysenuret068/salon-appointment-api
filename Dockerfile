FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY ["SalonAppointmentApi/SalonAppointmentApi.csproj", "SalonAppointmentApi/"]

RUN dotnet restore "SalonAppointmentApi/SalonAppointmentApi.csproj"

COPY . .

WORKDIR "/src/SalonAppointmentApi"

RUN dotnet publish "SalonAppointmentApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 10000

ENTRYPOINT ["dotnet", "SalonAppointmentApi.dll"]