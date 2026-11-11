FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/GestorTareas.Api/GestorTareas.Api.csproj src/GestorTareas.Api/
COPY src/GestorTareas.Enviador/GestorTareas.Enviador.csproj src/GestorTareas.Enviador/
COPY src/GestorTareas.ControlAcceso/GestorTareas.ControlAcceso.csproj src/GestorTareas.ControlAcceso/
COPY src/GestorTareas.Documentos/GestorTareas.Documentos.csproj src/GestorTareas.Documentos/
COPY src/GestorTareas.GestionPermisos/GestorTareas.GestionPermisos.csproj src/GestorTareas.GestionPermisos/
COPY src/GestorTareas.Negocio/GestorTareas.Negocio.csproj src/GestorTareas.Negocio/
COPY src/GestorTareas.Notificaciones/GestorTareas.Notificaciones.csproj src/GestorTareas.Notificaciones/

RUN dotnet restore src/GestorTareas.Api/GestorTareas.Api.csproj \
    && dotnet restore src/GestorTareas.Enviador/GestorTareas.Enviador.csproj

COPY src/ src/

RUN dotnet publish src/GestorTareas.Api/GestorTareas.Api.csproj \
        --configuration Release \
        --no-restore \
        --output /app/api \
    && dotnet publish src/GestorTareas.Enviador/GestorTareas.Enviador.csproj \
        --configuration Release \
        --no-restore \
        --output /app/enviador

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOCUMENTOS_RUTA=/app/datos/documentos

RUN mkdir -p /app/datos/documentos

COPY --from=build /app/api /app/api
COPY --from=build /app/enviador /app/enviador

EXPOSE 8080
ENTRYPOINT ["dotnet", "/app/api/GestorTareas.Api.dll"]
